shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.6;
uniform vec2 uv_scale = vec2(1.0, 1.0);
uniform vec4 splash_color : source_color = vec4(0.63, 0.78, 0.9, 0.45);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(269.5, 183.3))) * 43758.5453123);
}

float splash_at(vec2 uv, vec2 scale, float time_offset) {
    vec2 splash_uv = uv * scale;
    vec2 cell = floor(splash_uv);
    vec2 local = fract(splash_uv);

    float seed = hash(cell + time_offset);
    float visible = step(0.42, hash(cell + vec2(11.7, 3.4) + floor(TIME * 1.3)));
    float speed = mix(4.8, 9.4, hash(cell + vec2(4.2, 17.9)));
    float phase = fract(TIME * speed * mix(0.78, 1.22, seed) + hash(cell + vec2(23.1, 91.7)) + time_offset);

    vec2 impact_point = vec2(
        mix(0.16, 0.84, seed),
        mix(0.58, 0.94, hash(cell + vec2(37.2, 8.9)))
    );

    float size = mix(0.72, 1.45, hash(cell + vec2(5.1, 49.4)));
    vec2 delta = local - impact_point;

    float impact = smoothstep(0.085 * size, 0.0, length(vec2(delta.x * 1.8, delta.y * 4.2)));
    float ripple_width = phase * mix(0.18, 0.46, hash(cell + vec2(71.0, 13.0)));
    float ripple = smoothstep(0.04, 0.0, abs(abs(delta.x) - ripple_width))
        * smoothstep(0.055, 0.0, abs(delta.y + phase * 0.075));
    float fade = 1.0 - smoothstep(0.12, 0.82, phase);

    return (impact * (1.0 - phase) + ripple) * fade * visible;
}

void fragment() {
    vec2 uv = UV * uv_scale;
    float splash = splash_at(uv, vec2(31.0, 13.0), 0.0)
        + splash_at(uv + vec2(0.37, 0.19), vec2(19.0, 8.0), 9.3) * 0.55;

    COLOR = splash_color * splash * intensity;
}
