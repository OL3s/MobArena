shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.5;
uniform vec4 sun_color : source_color = vec4(1.0, 0.82, 0.35, 0.18);

void fragment() {
    vec2 center = UV - vec2(0.5);
    float glow = 1.0 - smoothstep(0.05, 0.78, length(center));
    float pulse = 0.82 + sin(TIME * 0.8) * 0.18;
    COLOR = sun_color * glow * pulse * intensity;
}
