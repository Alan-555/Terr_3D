#version 330 core
in vec3 normal;
in vec3 worldPos;

out vec4 FragColor;

uniform vec3 sunDir;
uniform sampler2D texture;
uniform vec3 camPos;
uniform mat4 model;



float getDensity(vec3 startPos, vec3 dir, bool sun) {
    int maxSteps = 1024;
    float step = 0.01;
    float pointDensity = 0.15 * step;

    float density = 0;

    vec3 currentPos = startPos;

    for(int i = 0; i < maxSteps; i++) {
        currentPos += dir * step;

        vec3 localPos = (inverse(model) * vec4(currentPos, 1)).xyz;
        if(abs(localPos.x) > 0.5 || abs(localPos.y) > 0.5 || abs(localPos.z) > 0.5) {
            break; //exit volume
        }
        float light = 1;
        if(!sun){
            //light = getDensity(currentPos, normalize(currentPos + sunDir), true);
        }

        density += pointDensity * (1 / light);
        if(density > 1)
            break;
    }

    return density;
}

void main() {
    vec3 rayDir = normalize(worldPos - camPos);

    float density = getDensity(worldPos, rayDir, false);
    FragColor = vec4(vec3(0.75, 0.75, 0.75) + (rayDir * 0.0001), density); //texture2D(texture, screenUV);
}
