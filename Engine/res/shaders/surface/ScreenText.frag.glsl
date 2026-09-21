#version 330 core
out vec4 FragColor;

uniform sampler2D text;
uniform vec4 colour;

in vec2 uv;

void main() {
    FragColor = texture(text, uv) * colour;
}