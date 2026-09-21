namespace Terr3D.Server.Shared;

/// <summary>
/// This interfaces defines methods to be used by both components and entities
/// </summary>
public interface IThinker //TODO: finish
{
    //Called after the thinker is created and engine initialised. Now custom init logic may commence
    void OnInitialise();


    //Change state
    void OnEnable();
    void OnDisable();

    //Events
    void OnTransformUpdate();

    //End
    void OnDestroyed();
}

public interface IUpdates
{
    /// <summary>
    /// Called each frame
    /// </summary>
    /// <param name="dt">The delta time</param>
    void OnUpdate(float dt);
}


public interface IDebugAction
{
    void DebugAction0();
}