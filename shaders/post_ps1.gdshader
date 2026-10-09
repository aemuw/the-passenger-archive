// Post-process applied to the low-res SubViewportContainer:
// ordered (Bayer) dithering, colour quantisation, grain, scanlines, vignette.
shader_type canvas_item;

uniform float color_levels = 24.0;
uniform float grain = 0.035;
uniform float scanline = 0.06;
uniform float vignette = 0.55;
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
	ivec2 p = ivec2(UV * size);
	float d = BAYER[(p.x % 4) + (p.y % 4) * 4] / 16.0 - 0.5;

	vec3 c = texture(TEXTURE, UV).rgb * tint;
	c += (hash(UV * size + vec2(TIME * 17.0, TIME * 31.0)) - 0.5) * grain;
	c += d / color_levels;
	c = floor(c * color_levels + 0.5) / color_levels;

	c *= 1.0 - scanline * float(p.y % 2);
	float v = distance(UV, vec2(0.5));
	c *= 1.0 - smoothstep(0.25, 0.85, v) * vignette;

	COLOR = vec4(c, 1.0);
}
