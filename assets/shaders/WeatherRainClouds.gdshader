shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.6;
uniform vec4 cloud_color : source_color = vec4(0.025, 0.035, 0.06, 0.48);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 value) {
    vec2 cell = floor(value);
    vec2 local = fract(value);
    vec2 smooth_local = local * local * (3.0 - 2.0 * local);

    float a = hash(cell);
    float b = hash(cell + vec2(1.0, 0.0));
    float c = hash(cell + vec2(0.0, 1.0));
    float d = hash(cell + vec2(1.0, 1.0));

    return mix(mix(a, b, smooth_local.x), mix(c, d, smooth_local.x), smooth_local.y);
}

void fragment() {
    float cloud = noise(UV * vec2(4.0, 2.4) + vec2(TIME * 0.035, TIME * 0.015));
    float density = 0.72 + cloud * 0.28;
    COLOR = cloud_color * density * intensity;
}
