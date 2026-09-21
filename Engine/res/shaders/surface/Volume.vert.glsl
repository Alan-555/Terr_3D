#version 330 core
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;

out vec3 worldPos;
out vec3 normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;


void main() {
    vec4 worldPos4 = model * vec4(Position, 1.0);
    gl_Position = projection * view * worldPos4;
    worldPos = worldPos4.xyz;
    normal = Normal;
}