#version 330 core
//future use?
//in vec3 localPos;
//in vec3 viewSpace;
in vec3 normal;
in vec3 worldPos;
in vec2 uv;

out vec4 FragColor;

uniform vec3 sunDir;
uniform vec3 camPos;

struct Material {//TODO: replace with PBR?
    vec3 diffuse;
    vec3 specular;
    float shininess;
    sampler2D albedo;
};
uniform Material material;

struct Light {
    vec4 pos;
    vec3 colour;
    float intensity;
};
uniform Light plrLight;

struct LightingInfo {
    vec3 dir;
    vec3 colour;
    float intensity;
};

struct EnvInfo {
    LightingInfo lighting;
    float stormIntensity;
};

uniform EnvInfo env;

uniform sampler2D shadowMap;
uniform mat4 lightMatrix;

uniform sampler2D skyTexture;

const vec3 sunColour = vec3(0.89, 0.81, 0.66);
const vec3 dayAmbientColour = vec3(0.15, 0.2, 0.22);
const vec3 nightAmbientColour = vec3(0.03, 0.05, 0.13);

uniform float fogDensity;

const float mapBorder = 50;
uniform float worldHalfExtents;

//computes a colour for a dir or point light
vec3 ComputeColour(vec3 camDir, vec3 _lightColour, vec4 _lightPos, float _lightIntensity) {

    //calculates the direction to the light (w hack - turn it into a vector if it is a dir light)
    vec3 lightVec = _lightPos.xyz - worldPos * _lightPos.w;

    //distance to the light
    float distance = length(lightVec);

    //normalised lightVec
    vec3 lightDir = lightVec / distance; 

    //assume dir light
    float attenuation = 1;

    //if point, calculate the distance according to the inverse square law (I think???)
    if(_lightPos.w != 0) {
        attenuation = 1.0 / (distance * distance + 0.0001);
    }    

    //cache the dot
    float lightDot = max(0.0, dot(lightDir, normal));

    //compute the diffuse
    vec3 diffuse = _lightColour * _lightIntensity * lightDot * material.diffuse;

    //assume no specular
    vec3 specular = vec3(0.0);

    //compute the specular if applicable
    if(lightDot > 0.0) {
        //Blinn-Phong
        vec3 halfwayDir = normalize(lightDir + camDir);
        float specW = pow(max(dot(normal, halfwayDir), 0.0), material.shininess);
        specular = _lightColour * _lightIntensity * specW * material.specular * lightDot;
    }
    //mix colours and attenuate
    return (diffuse + specular) * attenuation;
}
//helper that deconstructs the struct
vec3 ComputeColour(vec3 camDir, Light l) {
    return ComputeColour(camDir, l.colour, l.pos, l.intensity);
}

float InShadow() {
    vec3 offsetPos = worldPos + normal * 0.02;
    vec4 lightPos4 = lightMatrix * vec4(offsetPos, 1);
    vec3 lightPos = lightPos4.xyz / lightPos4.w;
    lightPos = lightPos * 0.5 + 0.5;

    if(lightPos.z > 1.0 || lightPos.x > 1.0 || lightPos.y > 1.0 || lightPos.x < 0.0 || lightPos.y < 0.0)
        return 0.0;

    float fragDepth = lightPos.z;

    float bias = 0.005;
    float shadow = 0.0;
    vec2 texelSize = 1.0 / textureSize(shadowMap, 0);
    for(int x = -1; x <= 1; ++x) {
        for(int y = -1; y <= 1; ++y) {
            float texDepth = texture(shadowMap, lightPos.xy + vec2(x, y) * texelSize).r;
            shadow += fragDepth - bias > texDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 9.0;

    return shadow;

}

vec3 Fog(vec3 finalColour) {
    if(fogDensity == 0)
        return finalColour;
    float camera_dx = max(0.0, abs(camPos.x) - (worldHalfExtents - mapBorder));
    float camera_dz = max(0.0, abs(camPos.z) - (worldHalfExtents - mapBorder));
    float camera_fromInner = sqrt(camera_dx * camera_dx + camera_dz * camera_dz);

    //how intense should the void effect be
    float voidIntensity = clamp(camera_fromInner / mapBorder, 0.0, 1.0);
    voidIntensity = 1 / pow(1.0 - voidIntensity, 0.5);

    //compute the fog distance to the current fragment
    float fogDistance = length(camPos - worldPos);

    //compute visibility with exponential falloff
    float visibility = exp(-fogDensity * fogDistance * voidIntensity);

    //distance to the void
    float dx = max(0.0, abs(worldPos.x) - (worldHalfExtents - mapBorder));
    float dz = max(0.0, abs(worldPos.z) - (worldHalfExtents - mapBorder));

    float distFromInner = sqrt(dx * dx + dz * dz);
    float edgeDistance = 1.0 - clamp(distFromInner / mapBorder, 0.0, 1.0);

    float edgeFactor = smoothstep(0.0, 0.15, edgeDistance);

    //change visibility depending on the edgeFactor
    visibility *= edgeFactor;

    //convert screen space uv to texture space uv
    vec2 screenUV = gl_FragCoord.xy / textureSize(skyTexture, 0);

    //sample the sky colour at that pixel
    vec3 skyFogColour = texture(skyTexture, screenUV).rgb;

    //mix it according to the visibility
    finalColour = mix(skyFogColour, finalColour, visibility);
    return finalColour;
}

void main() {
    float dayValue = (sunDir.y + 1) / 2.0;
    vec3 camDir = normalize(camPos - worldPos);

    //dir lighting
    vec3 sunColour = ComputeColour(camDir, sunColour, vec4(sunDir, 0), 0.75) * dayValue;
    vec3 flashColour = vec3(0.0);
    if(env.lighting.intensity > 0)
        flashColour = ComputeColour(camDir, env.lighting.colour, vec4(env.lighting.dir, 0), env.lighting.intensity);

    vec3 moonColour = ComputeColour(camDir, nightAmbientColour, vec4(-sunDir, 0), 0.75);

    //ambient lighting
    vec3 ambientColour = mix(nightAmbientColour, dayAmbientColour, dayValue) * (1 - dot(normal, vec3(1, 0, 0)) * 0.1);

    //dynamic lighting
    vec3 dynamicLights = ComputeColour(camDir, plrLight) * 0.0001 + env.stormIntensity * 0.0001;

    float shadow = 1;
    if(dayValue >= 0.5)
        shadow = max(0.25, 1 - InShadow());
    vec3 finalColour = (ambientColour + dynamicLights + sunColour * shadow + moonColour + flashColour) * texture2D(material.albedo, uv).xyz;

    finalColour = Fog(finalColour);

    FragColor = vec4(finalColour, 1.0);
}