#version 330 core
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TextUV;

layout(location = 3) in vec3 iOffset;
layout(location = 4) in vec3 iScale;
layout(location = 5) in float iAlpha;

out vec2 uv;
out float alpha;

uniform mat4 view;
uniform mat4 projection;
uniform vec3 camPos;

void main() {
    vec3 localPos = Position * iScale;

    vec3 forward = camPos - iOffset;
    forward.y = 0.0;

    if(length(forward) < 0.0001) {
        forward = vec3(0.0, 0.0, 1.0);
    } else {
        forward = normalize(forward);
    }

    vec3 up = vec3(0.0, 1.0, 0.0);
    vec3 right = cross(up, forward);

    vec3 worldPos = iOffset +
        (right * localPos.x) +
        (up * localPos.y) +
        (forward * localPos.z);
    
    float alphaDecay = exp(-0.02 * distance(worldPos, camPos)); 

    gl_Position = projection * view * vec4(worldPos, 1.0);

    uv = TextUV;
    uv.y = -uv.y;
    alpha = iAlpha * alphaDecay;
}
