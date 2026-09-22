using System.Runtime.CompilerServices;
using OpenTK.Mathematics;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Entities;


/// <summary>
/// Class used to represent all transformations of an object. Provides model matrix with caching logic
/// </summary>
public class Transform
{

    public Transform(Entity entity)
    {
        Entity = entity;
        entity.OnParentChanged += UpdateParent;
    }

    /// <summary>
    /// The entity this transform belongs to
    /// </summary>
    public readonly Entity Entity;

    /// <summary>
    /// The cached parent
    /// </summary>
    private Transform? _parent;

    /// <summary>
    /// The local position of this object
    /// </summary>
    public Vector3 LocalPosition
    {
        get => _localPos;
        set
        {
            float dx = _localPos.X - value.X;
            float dy = _localPos.Y - value.Y;
            float dz = _localPos.Z - value.Z;

            //do not update anything if the object did not move at all = moved a tiny tiny bit
            if (dx * dx + dy * dy + dz * dz < 1e-10f)
                return;
            _localPos = value;
            SetDirty();
        }
    }
    private Vector3 _localPos = new();

    /// <summary>
    /// The local rotation of this object
    /// </summary>
    public Quaternion LocalRotation
    {
        get => _localRot;
        set
        {
            if (Math.Abs(_localRot.X - value.X) < 1e-10f &&
                Math.Abs(_localRot.Y - value.Y) < 1e-10f &&
                Math.Abs(_localRot.Z - value.Z) < 1e-10f &&
                Math.Abs(_localRot.W - value.W) < 1e-10f)
                return;

            _localRot = value;
            SetDirty();
        }
    }
    private Quaternion _localRot = Quaternion.Identity;

    /// <summary>
    /// The local scale of this object
    /// </summary>
    public Vector3 LocalScale
    {
        get => _localScale;
        set
        {
            if (_localScale == value)
                return;
            _localScale = value;
            SetDirty();
        }
    }
    private Vector3 _localScale = new(1, 1, 1);

    /// <summary>
    /// The global position of this object
    /// </summary>
    public Vector3 Position
    {
        get
        {
            if (_parent == null) return LocalPosition;
            return GlobalMatrix.ExtractTranslation();
        }
        set
        {
            if (_parent == null)
            {
                LocalPosition = value;
                return;
            }

            Matrix4 parentGlobalInverse = Matrix4.Invert(_parent.GlobalMatrix);
            LocalPosition = Vector3.TransformPosition(value, parentGlobalInverse);
        }
    }
    /// <summary>
    /// The global rotation of this object
    /// </summary>
    public Quaternion Rotation
    {
        get
        {
            if (_parent == null) return LocalRotation;
            return _parent.Rotation * LocalRotation;
        }
        set
        {
            if (_parent == null)
            {
                LocalRotation = value;
                return;
            }
            Quaternion parentInverse = Quaternion.Invert(_parent.Rotation);
            LocalRotation = parentInverse * value;
        }
    }

    /// <summary>
    /// The global scale of this object
    /// </summary>
    public Vector3 Scale
    {
        get
        {
            if (_parent == null) return LocalScale;

            Vector3 parentScale = _parent.Scale;
            return new Vector3(LocalScale.X * parentScale.X, LocalScale.Y * parentScale.Y, LocalScale.Z * parentScale.Z);
        }
        set
        {
            if (_parent == null)
            {
                LocalScale = value;
                return;
            }

            Vector3 parentScale = _parent.Scale;
            LocalScale = new Vector3(value.X / parentScale.X, value.Y / parentScale.Y, value.Z / parentScale.Z);
        }
    }

    /// <summary>
    /// Used to set the quartation rotation using euler angles
    /// </summary>
    public Vector3 LocalEuler
    {
        get => LocalRotation.ToEulerAngles();
        set => LocalRotation = Quaternion.FromEulerAngles(value);
    }

    /// <summary>
    /// Returns the local forward vector in the global coordinates
    /// </summary>
    public Vector3 Forward =>
    (new Vector4(0, 0, -1, 0) * GlobalMatrix).Xyz.Normalized();


    /// <summary>
    /// Returns the local up vector in the global cooridnates
    /// </summary>
    public Vector3 Up =>
    (new Vector4(0, 1, 0, 0) * GlobalMatrix).Xyz.Normalized();

    /// <summary>
    /// Returns the local right vector in the global cooridnates
    /// </summary>
    public Vector3 Right =>
    (new Vector4(1, 0, 0, 0) * GlobalMatrix).Xyz.Normalized();


    /// <summary>
    /// The local 4x4 matrix describing all transformations
    /// </summary>
    public Matrix4 LocalMatrix => _cachedLocalMatrix ?? ComputeLocalMatrix();

    /// <summary>
    /// The global 4x4 matrix describing all transformations
    /// </summary>
    public Matrix4 GlobalMatrix => LocalMatrix * (_parent?.GlobalMatrix ?? Matrix4.Identity);//TODO: global matrix caching. Propagate invalidation to children!!!!
    Matrix4? _cachedLocalMatrix = null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetDirty()
    {
        _cachedLocalMatrix = null;
        //TODO: transform update
    }

    Matrix4 ComputeLocalMatrix()
    {
        Matrix4 scale = Matrix4.CreateScale(LocalScale);
        Matrix4 rotation = Matrix4.CreateFromQuaternion(LocalRotation);
        Matrix4 translation = Matrix4.CreateTranslation(LocalPosition);

        //cache matrix
        _cachedLocalMatrix = scale * rotation * translation;
        return _cachedLocalMatrix.Value;
    }

    private void UpdateParent()
    {
        var worldPos = Position;
        var worldRot = Rotation;
        var worldScale = Scale;
        _parent = Entity.Parent.Transform;
        Position = worldPos;
        Rotation = worldRot;
        Scale = worldScale;
    }


    public override string ToString() => $"{Position.X:n3}|{Position.Y:n3}|{Position.Z:n3}";
}