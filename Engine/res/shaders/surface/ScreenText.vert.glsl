#version 330 core
layout(location = 0) in vec3 Position;
layout(location = 2) in vec2 TextUV;

out vec2 uv;

uniform vec2 pos;

void main() {
    gl_Position = vec4((Position.xy * 2.0 - 1.0) * 1 + pos, 0.0, 1.0);
    uv = TextUV;
    uv.y = -uv.y;
}