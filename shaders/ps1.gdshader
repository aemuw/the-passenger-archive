// PS1-style spatial shader: vertex snapping (wobble), world-space grime texture,
// nearest filtering. Colour quantisation + dithering live in post_ps1.gdshader.
shader_type spatial;
render_mode diffuse_lambert, specular_disabled, cull_back;

uniform vec4 albedo : source_color = vec4(1.0);
uniform sampler2D grime_tex : source_color, filter_nearest, repeat_enable;
uniform float grime_strength = 0.35;
uniform float tex_scale = 0.6;
uniform sampler2D surf_tex : source_color, filter_nearest, repeat_enable;
uniform float surf_strength = 0.0;
uniform float surf_scale = 1.2;
uniform float emission_strength = 0.0;
// Number of snap steps across the screen (NDC -1..1). ~ half of render width = 1 px grid.
uniform float snap_resolution = 320.0;

varying vec3 w_pos;
varying vec3 w_nrm;

void vertex() {
	w_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	w_nrm = abs(normalize(MODEL_NORMAL_MATRIX * NORMAL));

	vec4 clip = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX, 1.0));
	vec2 ndc = clip.xy / clip.w;
	ndc = floor(ndc * snap_resolution + 0.5) / snap_resolution;
	POSITION = vec4(ndc * clip.w, clip.z, clip.w);
}

void fragment() {
	vec2 uv;
	if (w_nrm.y > w_nrm.x && w_nrm.y > w_nrm.z) {
		uv = w_pos.xz;
	} else if (w_nrm.x > w_nrm.z) {
		uv = w_pos.zy;
	} else {
		uv = w_pos.xy;
	}
	vec3 g = texture(grime_tex, uv * tex_scale).rgb;
	vec3 sv = texture(surf_tex, uv * surf_scale).rgb;
	vec3 col = albedo.rgb * mix(vec3(1.0), g, grime_strength) * mix(vec3(1.0), sv, surf_strength);
	ALBEDO = col;
	ROUGHNESS = 1.0;
	EMISSION = albedo.rgb * emission_strength;
}
