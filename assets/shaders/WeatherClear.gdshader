shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 1.0;
uniform vec4 shadow_color : source_color = vec4(0.06, 0.075, 0.095, 0.34);
uniform vec4 background_overlay_color : source_color = vec4(0.03, 0.035, 0.045, 0.045);

float hash(vec2 value) {
    return fract(sin(dot(value, vec2(127.1, 311.7))) * 43758.5453123);
}

float cloud_blob(vec2 uv, vec2 center, vec2 radius) {
    vec2 delta = (uv - center) / radius;
    float distance = dot(delta, delta);
    return 1.0 - smoothstep(0.36, 1.0, distance);
}

float cloud_cluster(vec2 uv, vec2 center, vec2 radius) {
    float shape = 0.0;
    shape += cloud_blob(uv, center, radius) * 0.62;
    shape += cloud_blob(uv, center + vec2(-radius.x * 0.32, radius.y * 0.08), radius * vec2(0.72, 0.9)) * 0.45;
    shape += cloud_blob(uv, center + vec2(radius.x * 0.34, -radius.y * 0.02), radius * vec2(0.76, 0.82)) * 0.42;
    shape += cloud_blob(uv, center + vec2(radius.x * 0.02, -radius.y * 0.28), radius * vec2(0.62, 0.7)) * 0.34;
    return clamp(shape, 0.0, 1.0);
}

float tiled_cloud_cluster(vec2 uv, vec2 center, vec2 radius) {
    vec2 tile_uv = fract(uv);
    float shape = cloud_cluster(tile_uv, center, radius);
    shape += cloud_cluster(tile_uv, center + vec2(1.0, 0.0), radius);
    shape += cloud_cluster(tile_uv, center - vec2(1.0, 0.0), radius);
    shape += cloud_cluster(tile_uv, center + vec2(0.0, 1.0), radius);
    shape += cloud_cluster(tile_uv, center - vec2(0.0, 1.0), radius);
    shape += cloud_cluster(tile_uv, center + vec2(1.0, 1.0), radius);
    shape += cloud_cluster(tile_uv, center - vec2(1.0, 1.0), radius);
    return clamp(shape, 0.0, 1.0);
}

void fragment() {
    vec2 drift = vec2(TIME * 0.065, TIME * 0.018);
    vec2 uv = UV + drift;

    float cloud_shadow = 0.0;
    cloud_shadow += tiled_cloud_cluster(uv + vec2(0.08, 0.18), vec2(0.34, 0.42), vec2(0.42, 0.22)) * 0.72;
    cloud_shadow += tiled_cloud_cluster(uv * vec2(0.78, 0.88) + vec2(0.52, 0.68), vec2(0.58, 0.38), vec2(0.5, 0.26)) * 0.62;
    cloud_shadow += tiled_cloud_cluster(uv * vec2(1.08, 0.8) + vec2(0.2, 0.84), vec2(0.36, 0.55), vec2(0.36, 0.19)) * 0.38;

    cloud_shadow = clamp(cloud_shadow, 0.0, 1.0);

    float visible_cloud = smoothstep(0.08, 0.82, cloud_shadow);
    COLOR = background_overlay_color * intensity + shadow_color * visible_cloud * intensity;
}
