#version 330 core
in vec2 uv;
in float alpha;

out vec4 FragColor;

uniform sampler2D albedo;

uniform vec4 colour;

void main() {
    vec4 texColor = texture(albedo, uv);
    FragColor = vec4(texColor.rgb, texColor.a * alpha) * colour;
}
