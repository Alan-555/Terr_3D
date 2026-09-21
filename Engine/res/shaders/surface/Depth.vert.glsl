#version 330 core

layout(location = 0) in vec3 Position;

uniform mat4 viewProjMatrix;
uniform mat4 model;

void main() {
    gl_Position = viewProjMatrix * model * vec4(Position, 1.0);
}