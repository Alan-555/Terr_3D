
struct VSIn  { float3 pos : POSITION; float2 uv : TEXCOORD0; };
struct V2F   { float4 pos : SV_Position; float2 uv : TEXCOORD0; };

uniform float4x4 uMVP;
Sampler2D uTex;

[shader("vertex")]
V2F vert(VSIn i)
{
    V2F o;
    o.pos = mul(uMVP, float4(i.pos, 1));
    o.uv  = i.uv;
    return o;
}

[shader("fragment")]
float4 frag(V2F i) : SV_Target
{
    return uTex.Sample(i.uv);
}