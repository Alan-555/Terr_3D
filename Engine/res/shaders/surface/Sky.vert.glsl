#version 330 core

layout(location = 0) in vec3 aPosition;

out vec3 viewDir;
uniform mat4 invProjView;

void main() {    
    //calculate the view direction vector
    vec4 worldPos4 = invProjView * vec4(aPosition.xy, 1.0, 1.0);
    viewDir = worldPos4.xyz / worldPos4.w;

    //output the model coordinates directly
    gl_Position = vec4(aPosition.xy, 1.0, 1.0);
}