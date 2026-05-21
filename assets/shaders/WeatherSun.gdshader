shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.5;
uniform vec4 sun_color : source_color = vec4(1.0, 0.78, 0.24, 0.22);
uniform vec4 flare_color : source_color = vec4(1.0, 0.92, 0.55, 0.3);

void fragment() {
    vec2 center = UV - vec2(0.5);
    float glow = 1.0 - smoothstep(0.05, 0.78, length(center));
    float heat_pulse = 0.82 + sin(TIME * 0.45) * 0.18;

    float diagonal = UV.x * 1.25 + UV.y * 0.55 + TIME * 0.018;
    float broad_flare = smoothstep(0.38, 0.9, sin(diagonal * 6.28318) * 0.5 + 0.5);
    float fine_shimmer = smoothstep(0.38, 0.86, sin((UV.y + TIME * 0.08) * 26.0 + sin(UV.x * 12.0)) * 0.5 + 0.5);
    float horizon_heat = 1.0 - smoothstep(0.18, 0.86, UV.y);

    vec4 glow_color = sun_color * glow * heat_pulse;
    vec4 flare = flare_color * (broad_flare * 0.7 + fine_shimmer * horizon_heat * 0.24);
    COLOR = (glow_color + flare) * intensity;
}
