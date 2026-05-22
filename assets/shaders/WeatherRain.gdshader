shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.6;
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec4 rain_color : source_color = vec4(0.45, 0.62, 0.78, 0.55);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
    vec2 uv = UV * uv_scale;
    vec2 rain_uv = vec2(uv.x * 34.0 - uv.y * 10.0, uv.y * 8.0 - TIME * 8.5);
    vec2 cell = floor(rain_uv);
    vec2 local = fract(rain_uv);
    float random = hash(cell);
    float visible = step(0.34, random);
    float streak_width = smoothstep(0.15, 0.0, abs(local.x - random));
    float streak_length = smoothstep(1.0, 0.08, local.y);
    float streak = streak_width * streak_length;
    COLOR = rain_color * streak * visible * intensity;
}
