using System;
using OpenTK.Mathematics;
using Terr3D.Client.Resources;
using Terr3D.Server.Shared;

namespace Terr3D.Server.Components;

public class SmokeParticleSystem : ParticleSystem
{
    private Vector3[] _velocities;

    private bool _persistent = false;
    
    public float StartScale = 0.5f;
    public float EndScale = 3.0f;
    public float Lifetime = 2.0f;
    public float Speed = 2.0f;

    public bool doEmit = true;

    int _deadParticles = 0;

    public SmokeParticleSystem(Vector4 colour, int numParticles = 100, bool persistent = false) : base(numParticles, ResourceManager.Textures[ResourceIndex.Textures.Mist])
    {
        _velocities = new Vector3[numParticles];
        base.colour = colour;
        _persistent = persistent;
    }

    private float RandomValNegToPos()
    {
        return Random.Shared.NextSingle() * 2f - 1f;
    }

    private float RandomValNormalised()
    {
        return Random.Shared.NextSingle();
    }

    private void ResetParticle(int i)
    {
        ref var instance = ref _instances[i];
        
        instance.Position = Entity.Transform.Position;
        
        Vector3 dir = new Vector3(
            RandomValNegToPos(),
            RandomValNormalised(),
            RandomValNegToPos()
        );
        
        if (dir.LengthSquared > 0.001f)
            dir.Normalize();
        else
            dir = Vector3.UnitY;

        float currentSpeed = Speed * (0.5f + 0.5f * RandomValNormalised());
        _velocities[i] = dir * currentSpeed;

        instance.Scale = new Vector3(StartScale);

        instance.Alpha = RandomValNormalised(); 
    }

    public override void OnInitialise()
    {
        for (int i = 0; i < _count; i++)
        {
            ResetParticle(i);
        }
    }

    bool _wasDisabled = false;

    public override void OnUpdate(float dt)
    {
        if(!doEmit) _wasDisabled = true;
        if(doEmit && _wasDisabled)
        {
            _wasDisabled = false;
            OnInitialise();
        }
        float fadeRate = 1.0f / Lifetime;
        float scaleRate = (EndScale - StartScale) / Lifetime;

        for (int i = 0; i < _count; i++)
        {
            if(_velocities[i] == Vector3.NegativeInfinity) continue;
            ref var instance = ref _instances[i];

            instance.Position += _velocities[i] * dt;
            instance.Alpha -= fadeRate * dt;
            instance.Scale += new Vector3(scaleRate * dt);

            if (instance.Alpha <= 0f)
            {
                if (doEmit)
                {
                    ResetParticle(i);
                    instance.Alpha = 1.0f;
                }
                else if(!_persistent)
                {
                    _velocities[i] = Vector3.NegativeInfinity; //sentinel value
                    _deadParticles ++;
                    if(_deadParticles >= _count)
                        Destroy();
                }
            }
        }
    }
}
