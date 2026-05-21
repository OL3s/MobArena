shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.6;
uniform vec4 rain_color : source_color = vec4(0.45, 0.62, 0.78, 0.55);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
    vec2 rain_uv = vec2(UV.x * 42.0 + UV.y * 10.0, UV.y * 30.0 + TIME * 1.35);
    vec2 cell = floor(rain_uv);
    vec2 local = fract(rain_uv);
    float random = hash(cell);
    float streak = smoothstep(0.09, 0.0, abs(local.x - random)) * smoothstep(0.88, 0.18, local.y);
    COLOR = rain_color * streak * intensity;
}
