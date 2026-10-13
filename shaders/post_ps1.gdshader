// Post-process applied to the low-res SubViewportContainer:
// ordered (Bayer) dithering, colour quantisation, VHS colour bleed + tracking glitches, grain, scanlines, vignette.
// `stress` (0..1) is raised when something dangerous is close: more bleed, more noise, a slow pulse.
shader_type canvas_item;

uniform float color_levels = 32.0;
uniform float grain = 0.012;
uniform float scanline = 0.035;
uniform float vignette = 0.55;
uniform float stress = 0.0;
uniform float effects = 0.45;
uniform vec3 tint = vec3(1.0, 0.98, 0.9);

const float BAYER[16] = float[16](
	0.0, 8.0, 2.0, 10.0,
	12.0, 4.0, 14.0, 6.0,
	3.0, 11.0, 1.0, 9.0,
	15.0, 7.0, 13.0, 5.0
);

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
	vec2 size = vec2(textureSize(TEXTURE, 0));
	vec2 uv = UV;

	// Rare tracking glitch: a horizontal band that slides sideways.
	float band = floor(TIME * 0.7);
	float bandY = hash(vec2(band, 3.0));
	float glitch = (step(0.93, hash(vec2(band, 9.0))) + stress * 0.5) * effects;
	float inBand = step(abs(uv.y - bandY), 0.02 + stress * 0.03);
	uv.x += inBand * glitch * (hash(vec2(floor(uv.y * size.y), band)) - 0.5) * 0.03;

	// Slight constant horizontal wobble, stronger under stress.
	uv.x += sin(uv.y * 40.0 + TIME * 2.0) * 0.0006 * (1.0 + stress * 5.0) * effects;

	// Colour bleed (chromatic offset).
	float ch = (0.0011 + stress * 0.0035) * effects;
	vec3 c;
	c.r = texture(TEXTURE, uv + vec2(ch, 0.0)).r;
	c.g = texture(TEXTURE, uv).g;
	c.b = texture(TEXTURE, uv - vec2(ch, 0.0)).b;
	c *= tint;

	ivec2 p = ivec2(clamp(uv, vec2(0.0), vec2(0.99999)) * size);
	float d = BAYER[(p.x % 4) + (p.y % 4) * 4] / 16.0 - 0.5;

	c += (hash(uv * size + vec2(TIME * 17.0, TIME * 31.0)) - 0.5) * (grain + stress * 0.05) * effects;
	c += d * 0.35 / color_levels;
	c = floor(c * color_levels + 0.5) / color_levels;

	c *= 1.0 - scanline * effects * float(p.y % 2);
	float v = distance(uv, vec2(0.5));
	float pulse = 1.0 + stress * effects * 0.25 * (0.5 + 0.5 * sin(TIME * 6.0));
	c *= 1.0 - smoothstep(0.25, 0.85, v) * vignette * pulse;

	COLOR = vec4(c, 1.0);
}
