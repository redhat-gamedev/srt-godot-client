shader_type canvas_item;

uniform float width = 0.3;
uniform float gradient_ease = 5.0;
uniform float fill_ratio = 0.8;

uniform float red = 1.0;
uniform float green = 0.0;
uniform float blue = 0.0;

void fragment() {
	float fill_angle = fill_ratio * 3.141592656 * 2.0;
	vec2 uv = UV * 2.0 - 1.0;
	if (atan(uv.x, uv.y) + 3.141592656 < fill_angle) {
		float inner_width = 1.0 - width;
		inner_width *= inner_width;
		float d = uv.x * uv.x + uv.y * uv.y;
		if (d >= inner_width && d <= 1.0) {
			float w = abs((1.0 + inner_width) / 2.0 - d) / (1.0 - inner_width);
			w = 1.0 - pow(w + 0.5, gradient_ease);
			COLOR = vec4(red, green, blue, min(1.0, w));
		} else {
			COLOR.a = 0.0;
		}
	} else {
		COLOR.a = 0.0;
	}
}