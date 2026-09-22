using OpenTK.Mathematics;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Components;
using Terr3D.Server.Shared;
using Terr3D.Server.World;

namespace Terr3D.Server.Entities;

public class Environment : Entity, IUpdates, IEnvironmentProvider
{

    const float defaultFog = 0.0125f;

    private const float LightingMaxInt = 15f;

    private float _maxAge = 0;
    private float _lighting_age = 0;

    public bool FogOverride {get; set;}

    public Entity Sun => _sun;
    EmptyEntity _sun;

    public Renderer Sky => _sky;
    Renderer _sky;

    public Environment Weather => this;

    public EnvInfo CurrentEnv => _currentEnvironment;


    private EnvInfo _currentEnvironment = new()
    {
        fogDensity = defaultFog,
        lighting_Colour = new(1, 1, 1),
        lighting_Dir = new(0, 0, 0),
        lighting_Intensity = 0
    };


    public Environment(string name, Entity parent) : base(name, parent, true)
    {
        Register();
        //Add a sky quad
        _sky = AddComponent(new Renderer(ResourceManager.Shaders[ResourceIndex.Shaders.Sky], ResourceManager.Meshes[ResourceIndex.Meshes.SkyQuad], RendererClass.RENDER_IGNORE));

        //Add the sun
        _sun = new EmptyEntity("Sun", Onstage.Worldspawn, false);
    }

    public void Register()
    {
        Onstage.Globals.Register<IEnvironmentProvider>(this);
    }

    public void SetFogDensity(float d)
    {
        _currentEnvironment.fogDensity = d;
    }

    public void TriggerLighting(float maxAge, Vector3 dir = default, Vector3 colour = default)
    {
        _lighting_age = 0;
        _maxAge = maxAge;
        _currentEnvironment.lighting_Intensity = 0;
        if (colour == default)
            colour = new(1f);
        _currentEnvironment.lighting_Colour = colour;
        if (dir == default)
        {
            dir = new(Random.Shared.NextSingle() * 2 - 1, Random.Shared.NextSingle() * 0.5f + 0.5f, Random.Shared.NextSingle() * 2 - 1);
        }
        dir.Normalize();
        _currentEnvironment.lighting_Dir = dir;

        /*string strikeKey = Random.Shared.Next(2) == 0 ? ResourceIndex.Audio.Strike0 : ResourceIndex.Audio.Strike1;
        AudioManager.PlayOneShot(ResourceManager.Audio[strikeKey], 0.5f);*/
    }

    public override void OnUpdate(float dt)
    {
        base.OnUpdate(dt);
        UpdateStorm(dt);
        if (_lighting_age < _maxAge)
        {
            UpdateLighting(dt);
        }
        else
        {
            _currentEnvironment.lighting_Intensity = 0f;
        }

    }

    const float amplitudeDecay = 3.7f;
    const float frequencyDecay = 1.3f;

    const float initFrequency = 60f;
    const float flashSharpness = 14f;

    private void UpdateLighting(float dt)
    {
        _lighting_age += dt;
        float t = _lighting_age / _maxAge;

        float envelope = MathF.Exp(-amplitudeDecay * t) * (1 - t); //thank you FYA1
        float cos = MathF.Cos(initFrequency * (1 - MathF.Pow(1 - t, frequencyDecay + 1) / (frequencyDecay + 1)));
        float spikes = MathF.Pow((1 + cos) / 2f, flashSharpness);

        float intensityNorm = envelope * spikes;

        _currentEnvironment.lighting_Intensity = intensityNorm * LightingMaxInt;

    }

    public void SetUniforms(ShaderProgram shader)
    {
        shader.SetUniform("env.lighting.dir", ref _currentEnvironment.lighting_Dir);
        shader.SetUniform("env.lighting.colour", ref _currentEnvironment.lighting_Colour);
        shader.SetUniform("env.lighting.intensity", ref _currentEnvironment.lighting_Intensity);
        float stormIntensity = _globalRain == null ? 0f : _globalRain.intensity;
        shader.SetUniform("env.stormIntensity", ref stormIntensity);
    }

   


    #region Effects

    public bool IsStormDamaging { get; private set; }
    public bool IsEndgameStrom { get; private set; }

    bool endGameStormStarting = false;
    bool _isStrom = false;
    float _stormCoolDown = 0f;

    float _stormNextStrike = 0;

    private RainParticleSystem? _globalRain;

    float _stormFog = 0.1f;

    float _stormDamagesFor = 0f;

    public void StartEndgameStorm()
    {
        StartStorm(4f);
        endGameStormStarting = true;
    }

    public void StartStorm(float coolDown)
    {
        _isStrom = true;
        _stormCoolDown = coolDown;
        _stormNextStrike = EngineWindow.Time + 10f;
        _globalRain?.Destroy();
        _globalRain = Onstage.Globals.Player.PlayerCam.Entity.AddComponent(new RainParticleSystem(5000));
        IsStormDamaging = false;
        _stormDamagesFor = 5f;
    }

    public void StopStorm()
    {
        _isStrom = false;
    }

    public void UpdateStorm(float dt)
    {
        if (!_isStrom)
        {
            if (_globalRain != null)
            {
                _globalRain.intensity -= dt * 0.06f;
                if (_globalRain.intensity <= 0)
                {
                    _globalRain.Destroy();
                    _globalRain = null!;
                }
            }

            if (_currentEnvironment.fogDensity > defaultFog && !FogOverride)
            {
                _currentEnvironment.fogDensity = MathF.Max(_currentEnvironment.fogDensity - dt * 0.007f, defaultFog);
            }

            if (_stormDamagesFor > 0f)
            {
                _stormDamagesFor -= dt;
            }
            else
            {
                IsStormDamaging = false;
            }

            return;
        }

        if (_globalRain != null && _globalRain.intensity < 1)
        {
            _globalRain.intensity = MathF.Min(1, _globalRain.intensity + dt * 0.05f);
        }

        if (_currentEnvironment.fogDensity < _stormFog && !FogOverride)
        {
            _currentEnvironment.fogDensity = MathF.Min(_currentEnvironment.fogDensity + dt * 0.005f, _stormFog);
        }

        float time = EngineWindow.Time;

        if (time < _stormNextStrike) return;
        IsStormDamaging = true; //first flash marks the impending doom
        if (endGameStormStarting)
        {
            IsEndgameStrom = true; //gg
        }
        float age = Random.Shared.NextSingle() * 0.5f + 0.5f; //0.5 to 1
        TriggerLighting(age);
        _stormNextStrike = time + age + _stormCoolDown * (Random.Shared.NextSingle() * 0.4f + 0.8f);
    }
    #endregion

    public override void OnDestroyed()
    {
        base.OnDestroyed();
    }
}

public struct EnvInfo
{
    public float fogDensity;

    public Vector3 lighting_Dir;
    public Vector3 lighting_Colour;
    public float lighting_Intensity;
}