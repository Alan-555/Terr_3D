using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;

public class RainParticleSystem : ParticleSystem
{
    readonly Vector3 _extents = new(15, 10, 15);
    float[] _obstacles;
    const float rainSpawnHeightExtension = 1;

    public float intensity = 0;

    private float _radius = 0;

    public RainParticleSystem(int numParticles = 5000, float radius = 0) : base(numParticles, ResourceManager.Textures[ResourceIndex.Textures.Particle])
    {
        _obstacles = new float[numParticles];
        colour = new Vector4(221, 232, 197, 255) / 255f;
        _radius = radius;
    }

    private void ResetParticle(int i)
    {
        ref var pos = ref _instances[i].Position;
        pos = PickPos() + Entity.Transform.Position;

        _instances[i].Scale = new Vector3(RandHelper.RandFloatNormalised() * 0.5f + 0.5f + 0.2f) * new Vector3(0.1f, 1f, 0.1f);
        _instances[i].Alpha = (RandHelper.RandFloatNormalised() * 0.3f + 0.1f) * intensity;

        if (Entity.Onstage.Partitioner.Raycast(new(pos, -Vector3.UnitY), out var hit))
            _obstacles[i] = hit.Position.Y;
        else
            _obstacles[i] = -100;

    }

    private Vector3 PickPos()
    {
        
        if (_radius != 0)
        {
            Vector2 pointInCircle = RandHelper.RandomPointInUnitCircle(_radius);

            float spawnY = RandHelper.RandFloat(-1f, 1f) * rainSpawnHeightExtension * 2 - rainSpawnHeightExtension;
            return new Vector3(pointInCircle.X, spawnY, pointInCircle.Y);
        }
        else
        {
            return new Vector3(
                RandHelper.RandFloat(-1f, 1f),
                1 + RandHelper.RandFloatNormalised() * rainSpawnHeightExtension,
                RandHelper.RandFloat(-1f, 1f)
            ) * _extents;
        }
    }

    public override void OnUpdate(float dt)
    {
        var max = Entity.Transform.Position + _extents;
        var min = Entity.Transform.Position - _extents;
        for (int i = 0; i < _count; i++)
        {
            ref var instance = ref _instances[i];
            instance.Position.Y -= 50 * dt;
            if (_radius == 0)
            {
                if (
                    instance.Position.Y < min.Y / 2 ||

                    instance.Position.X < min.X ||
                    instance.Position.Z < min.Z ||
                    instance.Position.X > max.X ||
                    instance.Position.Z > max.Z ||

                    instance.Position.Y < _obstacles[i]
                )
                {
                    ResetParticle(i);
                }
            }
            else
            {
                if (instance.Position.Y < 0 ||
                    instance.Position.Y < _obstacles[i]
                )
                {
                    ResetParticle(i);
                }
            }

        }
    }


    public override void OnInitialise()
    {
        for (int i = 0; i < _count; i++)
        {
            ResetParticle(i);
        }
    }
}