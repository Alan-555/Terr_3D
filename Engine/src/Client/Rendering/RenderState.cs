using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL4;
using Terr3D.Utils;

namespace Terr3D.Client.Rendering;


public sealed class RenderState
{
    public bool Blend;
    public BlendingFactor Src = BlendingFactor.One, Dst = BlendingFactor.Zero;
    public bool Cull = true;
    public CullFaceMode CullMode = CullFaceMode.Back;
    public bool DepthTest = true, DepthWrite = true;

    // Current GL state
    static bool? gBlend, gCull, gDepthTest, gDepthWrite;
    static BlendingFactor? gSrc, gDst;
    static CullFaceMode? gCullMode;

    public void Apply()
    {
        if (gBlend != Blend) { Toggle(EnableCap.Blend, Blend); gBlend = Blend; }
        if (Blend && (gSrc != Src || gDst != Dst)) { GL.BlendFunc(Src, Dst); gSrc = Src; gDst = Dst; }

        if (gCull != Cull) { Toggle(EnableCap.CullFace, Cull); gCull = Cull; }
        if (Cull && gCullMode != CullMode) { GL.CullFace(CullMode); gCullMode = CullMode; }

        if (gDepthTest != DepthTest) { Toggle(EnableCap.DepthTest, DepthTest); gDepthTest = DepthTest; }
        if (gDepthWrite != DepthWrite) { GL.DepthMask(DepthWrite); gDepthWrite = DepthWrite; }
    }

    public static void InvalidateCache()
    {
        gBlend = gCull = gDepthTest = gDepthWrite = null;
        gSrc = gDst = null;
        gCullMode = null;
    }

    static void Toggle(EnableCap cap, bool on)
    {
        if (on) GL.Enable(cap); else GL.Disable(cap);
    }
}

public static class ShaderDirectives
{
    static readonly Regex Directive = new(@"^\s*//@(\w+)\s*(.*?)\s*$", RegexOptions.Multiline);

    public static RenderState Parse(string source)
    {
        var state = new RenderState();
        foreach (Match m in Directive.Matches(source))
            Apply(state, m.Groups[1].Value, m.Groups[2].Value.Trim());
        return state;
    }

    static void Apply(RenderState s, string key, string arg)
    {
        switch (key)
        {
            case "blend":
                s.Blend = arg != "off";
                (s.Src, s.Dst) = arg switch
                {
                    "alpha"    => (BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha),
                    "additive" => (BlendingFactor.SrcAlpha, BlendingFactor.One),
                    "premul"   => (BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha),
                    _          => (BlendingFactor.One, BlendingFactor.Zero),
                };
                break;
            case "cull":
                s.Cull = arg != "off";
                s.CullMode = arg == "front" ? CullFaceMode.Front : CullFaceMode.Back;
                break;
            case "depth_test":  s.DepthTest  = arg != "off"; break;
            case "depth_write": s.DepthWrite = arg != "off"; break;
            default:
                Diagnostics.Warn($"Unknown shader directive '@{key}'");
                break;
        }
    }
}