using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;


/// <summary>
/// Provides projection and view matrices
/// </summary>
public class Camera : Component
{

    public const float Near = 0.05f;
    public const float Far = 256;

    public float FOV = MathHelper.DegreesToRadians(90f);


    public Camera()
    {
        UpdateAspect(EngineWindow.Instance.ClientSize.X, EngineWindow.Instance.ClientSize.Y);
    }

    Matrix4 GetProjectionMatrix(float aspectRatio)
    {
        return Matrix4.CreatePerspectiveFieldOfView(FOV, aspectRatio, Near, Far);
    }
    Matrix4 GetOrthoProjectionMatrix(float x, float y)
    {
        return Matrix4.CreateOrthographicOffCenter(0, x, y, 0, 0.1f, 10f);
    }

    /// <summary>
    /// Updates the aspect ratio of the camera
    /// </summary>
    /// <param name="x">The new width</param>
    /// <param name="y">The new height</param>
    public void UpdateAspect(float x, float y)
    {
        _cachedProjectionMatrix = GetProjectionMatrix(x / y);
        _cachedOrthoProjectionMatrix = GetOrthoProjectionMatrix(x, y);
    }

    /// <summary>
    /// The view matrix of the camera
    /// </summary>
    public Matrix4 ViewMatrix => _cachedViewMatrix ?? ComputeViewMatrix();
    Matrix4? _cachedViewMatrix = null;

    /// <summary>
    /// The perspective projection matrix of the camera
    /// </summary>
    public Matrix4 ProjectionMatrix => _cachedProjectionMatrix;
    Matrix4 _cachedProjectionMatrix;

    /// <summary>
    /// The orthographic projection matrix
    /// </summary>
    public Matrix4 OrthoProjectionMatrix => _cachedOrthoProjectionMatrix;
    Matrix4 _cachedOrthoProjectionMatrix;

    Matrix4 ComputeViewMatrix()
    {
        _cachedViewMatrix = Transform.GlobalMatrix.Inverted();
        return _cachedViewMatrix.Value;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateAspect(EngineWindow.Instance.ClientSize.X, EngineWindow.Instance.ClientSize.Y);
    }

    protected override void OnTransformUpdate()
    {
        //invalidate projection matrix
        _cachedViewMatrix = null;
    }
}