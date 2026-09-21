#version 330 core

in vec2 uv;

uniform sampler2D albedo;
uniform vec3 colour;

out vec4 FragColor;

void main() {
    //just sample the font atlas
    FragColor = texture(albedo, uv) * vec4(colour,1);
}