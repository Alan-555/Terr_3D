#version 330 core

layout(location = 0) in vec3 Position;
layout(location = 2) in vec2 TextUv;

uniform mat4 projection;
uniform mat4 model;
uniform vec4 uvRect; // x: minU, y: minV, z: maxU, w: maxV

out vec2 uv;

void main() {
    //calculate the position in screen space (there is no view matrix since text is always rendered in front of the camera)
    gl_Position = projection * model * vec4(Position.xy, 0, 1.0);

    //calculate the correct uvs to grab from the font atlas
    float widthU = uvRect.z - uvRect.x;
    float heightV = uvRect.w - uvRect.y;

    uv.x = uvRect.x + (TextUv.x * widthU);
    uv.y = uvRect.y + (TextUv.y * heightV);
}