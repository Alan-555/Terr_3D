using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Server.World;
using Terr3D.Utils;

namespace Terr3D.Client;

public class SceneDrawer
{

    const int DepthBufferRes = 1024;

    const float ShadowDistance = 32;

    const float SunDistance = 32;

    const float SunOrthoExtension = 25;

    public Scene scene;

    SceneGlobals Globals { get;}
    SceneRegistry SceneRegistry { get;}

    readonly DepthFrameBuffer depthBuffer = new(DepthBufferRes);

    Texture _depthBufferTexture;

    ColourFrameBuffer? _skyBuffer;
    Texture? _skyBufferTexture;

    ShaderProgram _depthShader;

    ShaderProgram _fontShader;

    public SceneDrawer(Scene scene)
    {
        this.scene = scene;
        Globals = scene.Globals;
        SceneRegistry = scene.SceneRegistry;    
        _depthBufferTexture = new Texture(depthBuffer.depthMap);
        _depthShader = ResourceManager.Shaders[ResourceIndex.Shaders.Depth];
        _fontShader = ResourceManager.Shaders[ResourceIndex.Shaders.Font];
    }

    /// <summary>
    /// Keeps track of the current frame we render. This is used not to render an object twice. Overflowing is fine, yet it may cause an object not to render for one frame under VERY specific conditions. Very unlikely
    /// </summary>
    uint _frame = 0;


    /// <summary>
    /// Renders the entire scene to the default buffer
    /// </summary>
    public void RenderScene()
    {
        var cam = Globals.CurrentCamera;

        //cache matrices
        var viewMatrix = cam.ViewMatrix;
        var projectionMatrix = cam.ProjectionMatrix;
        var orthoMatrix = cam.OrthoProjectionMatrix;
        var viewProjMatrixForCulling = viewMatrix * projectionMatrix;


        if (PlayerController.debugCull)
            viewProjMatrixForCulling = Globals.Player.PlayerCam.ViewMatrix * Globals.Player.PlayerCam.ProjectionMatrix;

        //render to the light depth buffer and obtain the light matrix 
        DepthPass(ref viewMatrix, projectionMatrix, out var lightMatrix);

        //package the matrices
        MatrixPackage mat = new(ref viewMatrix, ref projectionMatrix, ref viewProjMatrixForCulling, ref orthoMatrix, ref lightMatrix);

        //draw the sky to a buffer
        DrawSky(ref mat);

        //render the geometry (excluding persistent overlays)
        RenderGeometry(ref mat, includePersistent: false);

        //render particles
        foreach (var ps in SceneRegistry.ParticleSystems)
        {
            ps.Render(ref mat.viewMatrix, ref mat.projectionMatrix, cam.Transform.Position);
        }

        //render persistent overlays
        RenderGeometry(ref mat, onlyPersistent: true);

        //render UI pass
        _fontShader.Use();
        foreach (var fontRenderer in Globals.Canvas.FontRenderer.PrepareFrame())
        {
            RenderShader(fontRenderer, _fontShader, ref mat, true);
        }

        _frame++;
    }

    void DrawSky(ref MatrixPackage mat)
    {
        var size = EngineWindow.Instance.ClientSize;
        //update size
        if (_skyBuffer == null || _skyBuffer.Width != size.X || _skyBuffer.Height != size.Y)
        {
            _skyBuffer?.Dispose();
            _skyBuffer = new ColourFrameBuffer(size.X, size.Y);
            _skyBufferTexture = new Texture(_skyBuffer.colourTexture);
        }

        var skyRenderer = Globals.Environment.Sky;

        //bind the sky colour buffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, (int)_skyBuffer);
        GL.Viewport(0, 0, _skyBuffer.Width, _skyBuffer.Height);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        skyRenderer.ShaderProgram.Use();

        //dispatch the sky shader
        RenderShader(skyRenderer, skyRenderer.ShaderProgram, ref mat, true);

        //use blit to render it as the background
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (int)_skyBuffer);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0, 0, _skyBuffer.Width, _skyBuffer.Height, 0, 0, size.X, size.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

        //cleanup
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, size.X, size.Y);
    }

    //Renders the scene into a depth texture used for shadow mapping
    void DepthPass(ref Matrix4 viewMatrix, Matrix4 projectionMatrix, out Matrix4 lightMatrix)
    {
        //bring the camera far clip plane closer
        projectionMatrix.M33 = -(ShadowDistance + Camera.Near) / (ShadowDistance - Camera.Near);
        projectionMatrix.M43 = -(2.0f * ShadowDistance * Camera.Near) / (ShadowDistance - Camera.Near);

        //compute the view-projection matrix
        var viewProjMatrix = viewMatrix * projectionMatrix;

        //compute the light matrix
        lightMatrix = CalculateLightMatrix(ref viewProjMatrix);

        //bind the depth buffer
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, (int)depthBuffer);
        GL.Viewport(0, 0, DepthBufferRes, DepthBufferRes);

        GL.Clear(ClearBufferMask.DepthBufferBit);

        //use the depth shader
        _depthShader.Use();

        _depthShader.SetUniform("viewProjMatrix", ref lightMatrix);

        //package the matrices

        MatrixPackage mat = new(ref lightMatrix);

        //and render to the buffer
        RenderGeometry(ref mat, _depthShader, false);

        //clean up
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.Viewport(0, 0, EngineWindow.Instance.ClientSize.X, EngineWindow.Instance.ClientSize.Y);

        //technically we rendered a frame, so we tell the renderers to render again
        _frame++;
    }

    /// <summary>
    /// Renders all visible scene geometry with the renderer's default shader and material or optionally with a specified shader override
    /// </summary>
    /// <param name="mat">The package of matrices to use</param>
    /// <param name="shaderOverride">Optionally the shader to be used</param>
    void RenderGeometry(ref MatrixPackage mat, ShaderProgram? shaderOverride = null, bool includePersistent = true, bool onlyPersistent = false)
    {

        //gather all potentially visible renderers
        List<Renderer> worldRenderers = [];

        //static renderers (never persistent)
        if (!onlyPersistent)
            worldRenderers = scene.Partitioner.QueryFrustum(mat.viewProjMatrix);


        //dynamic renderers
        foreach (var r in SceneRegistry.DynamicRenderers)
        {
            bool isPersistent = r.rendererClass == RendererClass.RENDERER_PERSISTENT;

            if (onlyPersistent && !isPersistent) continue;
            if (!includePersistent && isPersistent) continue;

            if (
                r.IsEnabled &&  //has to be enabled
                !r.IsDestroyed && //mustn't be destroyed
                (
                    isPersistent || //is either persistent or...
                    Volume_AABB.ClassifyVisibilityForFrustum(mat.viewProjMatrix, r.DynamicAABB) != Visibility.NONE //... is potentially visible
                )
            )
            {
                //then do add it
                worldRenderers.Add(r);
            }
        }

        //if the shader is overridden
        if (shaderOverride != null)
        {
            //use the shader and render all potentially visible renderers
            shaderOverride.Use();
            foreach (var renderer in worldRenderers)
            {
                RenderShader(renderer, shaderOverride, ref mat, false);
            }
        }
        else
        {
            //group renderers by their shader
            var groupedByShader = worldRenderers.GroupBy(r => r.ShaderProgram._resHandle);
            foreach (var group in groupedByShader)
            {
                var shader = group.First().ShaderProgram;
                shader.Use();
                foreach (var renderer in group)
                {
                    RenderShader(renderer, shader, ref mat, true);
                }
            }
        }
    }
    /// <summary>
    /// Renders a renderer to the bound buffer. The shader has to be activated prior!!
    /// </summary>
    /// <param name="renderer">The renderer to render</param>
    /// <param name="shader">The shader to render it with</param>
    /// <param name="mat">The matrices to use</param>
    /// <param name="useMaterials">Flag whether to use the renderer's materials</param>
    void RenderShader(Renderer renderer, ShaderProgram shader, ref MatrixPackage mat, bool useMaterials)
    {
        //if the renderer is inactive, we do not draw it. Also, if it saw renderer already, we skip it as well
        if (!renderer.IsEnabled || renderer.IsDestroyed || renderer.renderedOn == _frame) return;

        renderer.renderedOn = _frame;

        //upload common uniforms to the GPU and set flags
        PrepareUniformsAndFlags(shader, renderer, ref mat, useMaterials);

        //bind the VAO
        GL.BindVertexArray(renderer.mesh.VAO);

        //dispatch the draw call
        GL.DrawElements(PrimitiveType.Triangles, renderer.TriangleCount * 3, DrawElementsType.UnsignedInt, 0);

        //unbind the VAO
        GL.BindVertexArray(0);

        //clean up
        ClearFlagsAndUniforms(shader);
    }

    /// <summary>
    /// Uploads all the uniforms requested by the shader and potentially its materials
    /// </summary>
    /// <param name="shader">The shader to use</param>
    /// <param name="renderer">The renderer to upload it for</param>
    /// <param name="mat">The matrices</param>
    /// <param name="useMaterials">A flag whether to upload the materials as well</param>
    void PrepareUniformsAndFlags(ShaderProgram shader, Renderer renderer, ref MatrixPackage mat, bool useMaterials)
    {
        if (useMaterials)
        {
            renderer.ApplyMaterial();
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.TRANSFORM_TO_NDC))
        {
            var modelMatrix = renderer.Transform.GlobalMatrix;
            shader.SetUniform("model", ref modelMatrix);
            shader.SetUniform("view", ref mat.viewMatrix);
            shader.SetUniform("projection", ref mat.projectionMatrix);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_MODEL_MATRIX))
        {
            var modelMatrix = renderer.Transform.GlobalMatrix;
            shader.SetUniform("model", ref modelMatrix);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_INV_PROJ_VIEW))
        {
            var invProjView = (new Matrix4(new Matrix3(mat.viewMatrix)) * mat.projectionMatrix).Inverted();
            shader.SetUniform("invProjView", ref invProjView);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_SKY))
        {
            GL.Disable(EnableCap.CullFace);
            GL.DepthFunc(DepthFunction.Lequal);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_SUN_DIR))
        {
            Vector3 sunDir = Globals.Environment.SunDir;
            shader.SetUniform("sunDir", ref sunDir);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_CAMERA_POS))
        {
            Vector3 camPos = Globals.CurrentCamera.Transform.Position;
            shader.SetUniform("camPos", ref camPos);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_ALPHA_BLEND))
        {
            GL.Disable(EnableCap.CullFace);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_ORTHO_PROJ))
        {
            GL.Disable(EnableCap.CullFace);
            shader.SetUniform("projection", ref mat.orthoMatrix);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_DYNAMIC_LIGHTS))
        {
            var camPos = new Vector4(Globals.CurrentCamera.Transform.Position, 1f);
            shader.SetUniform("plrLight.pos", ref camPos);
            var clr = new Vector3(245, 186, 69) / 255f;
            shader.SetUniform("plrLight.colour", ref clr);
            float intensity = 2;
            shader.SetUniform("plrLight.intensity", ref intensity);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_WIREFRAME))
        {
            var target = ((WireframeBoxRenderer)renderer).Target;
            Matrix4 scale = Matrix4.CreateScale(target.GetScale());
            Matrix4 rotation = Matrix4.CreateFromQuaternion(target.GetRot());
            Matrix4 translation = Matrix4.CreateTranslation(target.GetPos());

            var modelMatrix = scale * rotation * translation;
            
            shader.SetUniform("model", ref modelMatrix);
            shader.SetUniform("view", ref mat.viewMatrix);
            shader.SetUniform("projection", ref mat.projectionMatrix);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_RENDER_PRIORITY))
        {
            GL.Disable(EnableCap.CullFace);
            GL.DepthFunc(DepthFunction.Always);
            GL.DepthMask(false);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_SHADOW_MAP))
        {
            shader.SetUniform("shadowMap", ref _depthBufferTexture, 31);
            shader.SetUniform("lightMatrix", ref mat.lightMatrix);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_SKY_FOG))
        {
            if (_skyBufferTexture != null)
                shader.SetUniform("skyTexture", ref _skyBufferTexture, 30);
            var half = Globals.Worldspawn.TerrainSize / 2f;
            shader.SetUniform("worldHalfExtents", ref half);
            shader.SetUniformNoRef("fogDensity", 0.001f); //TODO: fog
        }
    }

    /// <summary>
    /// Restores the OpenGL state, should it have been modified
    /// </summary>
    /// <param name="shader">The shader that caused havoc</param>
    public void ClearFlagsAndUniforms(ShaderProgram shader)
    {
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_SKY))
        {
            GL.Enable(EnableCap.CullFace);
            GL.DepthFunc(DepthFunction.Less);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_ALPHA_BLEND))
        {
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_ORTHO_PROJ))
        {
            GL.Enable(EnableCap.CullFace);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_WIREFRAME))
        {
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }
        if (shader.behaviourFlags.HasFlag(CommonBehaviourFlags.USE_RENDER_PRIORITY))
        {
            GL.Enable(EnableCap.CullFace);
            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);
        }
    }

    /// <summary>
    /// Calculate the light matrix. This is a matrix that transforms worldspace coordinates into light space coordinates
    /// </summary>
    /// <param name="viewProjMatrix">The current camera's view-projection matrix</param>
    /// <returns></returns>
    public Matrix4 CalculateLightMatrix(ref Matrix4 viewProjMatrix)
    {
        //invert the view-projection matrix
        var ivpMat = viewProjMatrix.Inverted();

        //the NDC corners
        Vector3[] corners = [
            new(-1, -1, -1),
            new(+1, -1, -1),
            new(-1, +1, -1),
            new(+1, +1, -1),

            new(-1, -1, +1),
            new(+1, -1, +1),
            new(-1, +1, +1),
            new(+1, +1, +1),
        ];

        Vector3[] corners_world = new Vector3[8];

        Vector3 center = default;

        //convert all NDC points into worldpsace coordinates and calculate the worldspace center of the camera frustum
        for (int i = 0; i < 8; i++)
        {
            var v4 = new Vector4(corners[i], 1) * ivpMat;
            corners_world[i] = v4.Xyz / v4.W;

            center += corners_world[i];
        }

        center /= 8;

        //move the light space camera further away towards the sun
        var lightPos = center + (Globals.Environment.SunDir * SunDistance);

        //compute the light view matrix
        Quaternion inverseSunRot = Globals.Environment.Sun.Transform.Rotation.Inverted();

        Matrix4 lightRotationMatrix = Matrix4.CreateFromQuaternion(inverseSunRot);

        Matrix4 lightTranslationMatrix = Matrix4.CreateTranslation(-lightPos);

        var lightViewMatrix = lightTranslationMatrix * lightRotationMatrix;

        //calculate the light space aligned bounding box of the rendering camera frustum

        Vector3 minPoint = new(float.PositiveInfinity);
        Vector3 maxPoint = new(float.NegativeInfinity);

        for (int i = 0; i < 8; i++)
        {
            var v4 = new Vector4(corners_world[i], 1) * lightViewMatrix;
            var lightSpacePos = v4.Xyz;

            minPoint = Vector3.ComponentMin(minPoint, lightSpacePos);
            maxPoint = Vector3.ComponentMax(maxPoint, lightSpacePos);
        }

        //expand the near and far clip planes to catch camera shadow casters
        float zNear = -maxPoint.Z - SunOrthoExtension;
        float zFar = -minPoint.Z + SunOrthoExtension;

        //finally calculate the projection matrix and return the view-projection matrix
        var lightProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
            minPoint.X, maxPoint.X,
            minPoint.Y, maxPoint.Y,
            zNear, zFar
        );

        return lightViewMatrix * lightProjectionMatrix;

    }

}


readonly ref struct MatrixPackage
{
    public readonly ref Matrix4 viewMatrix;
    public readonly ref Matrix4 projectionMatrix;
    public readonly ref Matrix4 viewProjMatrix;
    public readonly ref Matrix4 orthoMatrix;
    public readonly ref Matrix4 lightMatrix;

    public MatrixPackage(
        ref Matrix4 viewMatrix,
        ref Matrix4 projectionMatrix,
        ref Matrix4 viewProjMatrix,
        ref Matrix4 orthoMatrix,
        ref Matrix4 lightMatrix)
    {
        this.viewMatrix = ref viewMatrix;
        this.projectionMatrix = ref projectionMatrix;
        this.viewProjMatrix = ref viewProjMatrix;
        this.orthoMatrix = ref orthoMatrix;
        this.lightMatrix = ref lightMatrix;
    }

    public MatrixPackage(ref Matrix4 viewProjMatrix)
    {
        this.viewProjMatrix = ref viewProjMatrix;
    }
}