#version 330 core
out vec4 FragColor;
in vec3 viewDir;

uniform vec3 sunDir;

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

//sun settings
const vec3 sunColour = vec3(1.0, 0.98, 0.9);
const float sunSize = 500;
const vec3 sunAuraColour = vec3(1.0, 0.98, 0.9);
const float sunAuraSize = 100;

//sky settings

const float blendSharpness = 20.4;
const float blendSunInfluence = 1;

const vec3 skyDayZenithColour = vec3(0.22, 0.40, 0.85);
const vec3 skyDayHorizonColour = vec3(0.55, 0.72, 1.00);
const vec3 skyNightZenithColour = vec3(0.09, 0.01, 0.31);
const vec3 skyNightHorizonColour = vec3(0.00, 0.02, 0.12);

const vec3 sunRiseSetColour = vec3(0.93, 0.39, 0.14);

//star settings
const float starsSize = 0.0095;
const float starDensity = 0.03;

const float starSpawnThreshold = 0.6;

const float horizonThreshold = -0.1;

mat3 getRotationMatrix(vec3 inDir) {//TODO: port this horrible mess to the CPU
    vec3 up = vec3(0.0, 1.0, 0.0);
    vec3 v = cross(up, inDir);
    float s = length(v);
    float c = dot(up, inDir);

    //Rodrigues' rotation formula
    mat3 vx = mat3(0.0, v.z, -v.y, -v.z, 0.0, v.x, v.y, -v.x, 0.0);
    return mat3(1.0) + vx + vx * vx * ((1.0 - c) / (s * s));
}

float smooth_pulse(float x, float a, float b, float w, float k) {
    float d = max((b - a - w) * 0.5, 0.00001);

    float t_left = clamp((x - a) / d, 0.0, 1.0);
    float t_right = clamp((b - x) / d, 0.0, 1.0);
    float t = min(t_left, t_right);

    float t_safe = max(t, 0.00001);
    float inv_t_safe = max(1.0 - t, 0.00001);

    float tk = pow(t_safe, k);
    float inv_tk = pow(inv_t_safe, k);

    return tk / (tk + inv_tk);
}

float cloudNoise(vec3 p) {
    float n = sin(p.x * 4.5) * sin(p.y * 4.5) * sin(p.z * 4.5);
    n += 0.5 * sin(p.x * 6.0 + p.y * 6.0);
    return n * 0.5 + 0.5;
}

vec3 Lighting(vec3 viewDirNormalised) {
    float i = env.lighting.intensity;
    if(i <= 0.0)
        return vec3(0.0);

    vec3 distortedDir = viewDirNormalised + cloudNoise(viewDirNormalised * 2.0) * 0.25;
    distortedDir = normalize(distortedDir);

    float sim = dot(distortedDir, env.lighting.dir);

    float distFromCenter = 1.0 - sim;

    float expFalloff = exp(-7 * distFromCenter);
    
    const float ambientFlash = 0.15;
    float finalFlashIntensity = mix(ambientFlash, 1.0, expFalloff);

    return max(vec3(0), vec3(finalFlashIntensity) * env.lighting.colour * i);
}

void main() {
    vec3 viewDirNormalised = normalize(viewDir);
    vec3 rotatedDir = viewDirNormalised * getRotationMatrix(sunDir);
    //normalise to [0,1]
    rotatedDir = (rotatedDir + 1) / 2.0;

    //normalise to [0,1]
    float dayValue = (sunDir.y + 1) / 2.0;

    //how close we are to the sun
    float sunSim = dot(viewDirNormalised, sunDir);

    //how red the sky is (dawn and dusk)
    float redFactor = smooth_pulse(dayValue, 0.0, 1, 0.075, 1.8) / (1 / exp(sunSim) * (dayValue * 5));
    //0 means at the bottom, while 1 means at the top
    float altitude = viewDirNormalised.y * 0.5 + 0.5;

    vec3 skyDay = mix(skyDayHorizonColour, skyDayZenithColour, altitude);
    vec3 skyNight = mix(skyNightHorizonColour, skyNightZenithColour, altitude);

    skyDay = mix(skyDay, sunRiseSetColour, clamp(redFactor, 0.0, 0.5));

    float blendOffset = (2.0 * dayValue - 1.0) * blendSunInfluence;
    float colourBlend = 1.0 / (1.0 + exp(-(rotatedDir.y - 0.5 + blendOffset) * blendSharpness));

    vec3 resColour = mix(skyNight, skyDay, colourBlend) * max(0.5, 1 - env.stormIntensity);

    //debug day/night border
    /*if(abs(rotatedDir.y - 0.5) < 0.01) {
        resColour = vec3(1,0,0);
    }*/

    //sun
    if(sunSim > 0) {

        float sunIntensity = pow(sunSim, sunSize) * max(0.1, 1 - env.stormIntensity);
        float sunAuraIntensity = pow(sunSim, sunAuraSize) * max(0.1, 1 - env.stormIntensity);
        vec3 prevColour = resColour;
        resColour = mix(resColour, sunAuraColour, sunAuraIntensity);
        resColour = mix(resColour, sunColour, sunIntensity);

        if(viewDirNormalised.y <= 0) {
            resColour = mix(resColour, prevColour, 1.0 / horizonThreshold * max(viewDirNormalised.y, horizonThreshold));
        }
    }
    //stars
    if(dayValue < starSpawnThreshold && viewDirNormalised.y > 0) {
        //thanks to https://github.com/n-yoda/unity-skybox-shaders/blob/master/Assets/SkyboxShaders/Skybox-StarrySky.shader
        float starIntensity = 1 - (dayValue / starSpawnThreshold);
        starIntensity = smoothstep(0.0, 1.0, starIntensity) * (1 - sunSim);
        vec3 pos = rotatedDir / starsSize;
        vec3 center = round(pos);
        float w = 1117;
        float hash = mod(dot(vec3(641, -113, 271), center), w);
        float threshold = w * starDensity;
        if(abs(hash) < threshold) {
            float dist = length(pos - center);
            float star = clamp(pow(clamp(0.5 - dist * dist, 0.0, 1.0) * 2, 14), 0.0, 1.0);
            resColour += star * starIntensity;
        }
    }
    resColour = 1.0 - (1.0 - resColour) * (1.0 - Lighting(viewDirNormalised));

    FragColor = vec4(resColour, 1.0);
}
