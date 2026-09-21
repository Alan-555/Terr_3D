using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Terr3D.Server.Entities;
using Terr3D.Server.Components;
using Terr3D.Utils;

namespace Terr3D.Server.World;

public class SceneGlobals
{
    private readonly Dictionary<Type, object> _providers = new();

    /// <summary>
    /// Registers a provider instance for a specific type
    /// </summary>
    public void Register<T>(T provider) where T : notnull
    {
        if (_providers.ContainsKey(typeof(T)))
        {
            Diagnostics.Error($"The provider of type {typeof(T).Name} has already been registered. Overriding...");
        }
        _providers[typeof(T)] = provider;
    }

    /// <summary>
    /// Gets a registered provider.
    /// </summary>
    public T Get<T>() where T : class
    {
        if (_providers.TryGetValue(typeof(T), out var provider))
        {
            return (T)provider;
        }
        throw new KeyNotFoundException($"Provider of type {typeof(T).Name} is not registered");
    }


    public Worldspawn Worldspawn => Get<IWorldspawnProvider>().Worldspawn;
    public IEnvironmentProvider Environment => Get<IEnvironmentProvider>();
    public Gameplay Gameplay => Get<IGameplayProvider>().Logic;
    public Camera CurrentCamera {get=> Get<IWorldspawnProvider>().CurrentCamera; set=> Get<IWorldspawnProvider>().CurrentCamera = value; }
    public IPlayerProvider Player => Get<IPlayerProvider>();

    public Canvas Canvas => Get<ICanvasProvider>().Canvas;
}

public interface IProvider
{
    abstract void Register();
}


public interface IWorldspawnProvider : IProvider
{
    Worldspawn Worldspawn { get; }

    BoidManager BoidManager { get; }
    
    Camera CurrentCamera {get; set;}
}

public interface IEnvironmentProvider : IProvider
{
    Entity Sun { get; }
    Vector3 SunDir => Vector3.Transform(Vector3.UnitZ, Sun.Transform.Rotation);
    Renderer Sky { get; }
    Entities.Environment Weather { get; }
    EnvInfo CurrentEnv { get; }
}

public interface IGameplayProvider : IProvider
{
    Gameplay Logic { get; }
}

public interface IPlayerProvider : IProvider
{
    /// <summary>
    /// Mostly for debuging features and accessing the player...
    /// </summary>
    public Camera PlayerCam { get;}

    public Player LocalPlayer { get;}
}

public interface ICanvasProvider : IProvider
{
    public void RenderLabel(string text, int order);
    Canvas Canvas { get; }
}