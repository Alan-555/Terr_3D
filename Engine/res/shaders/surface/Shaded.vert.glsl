#version 330 core
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 TextUV;

//future use?
//out vec3 localPos;
//out vec3 viewSpace;
out vec3 worldPos;
out vec3 normal;
out vec2 uv;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main() {
    //cache position in world space
    vec4 worldSpacePos = model * vec4(Position, 1.0);

    //output the NDC pos
    gl_Position = projection * view * worldSpacePos;
    
    //transform the normal
    normal = normalize(mat3(model) * Normal);

    //output the world pos
    worldPos = worldSpacePos.xyz;

    //output the UVs
    uv = TextUV;
    uv.y = -uv.y; //TODO: investigate the flip. I defined UVs in Blender, but it seems that it uses the same uv coordinate system as OpenGL ???


    //localPos = Position;
    //viewSpace = (view * worldSpacePos).xyz;
}