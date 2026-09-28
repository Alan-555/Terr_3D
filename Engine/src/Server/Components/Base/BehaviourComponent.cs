namespace Terr3D.Server.Components;

public abstract class BehaviourComponent : Component
{
    internal void Update(float dt) => OnUpdate(dt);
    /// <summary>
    /// Called each frame
    /// </summary>
    /// <param name="dt">The time in millis that has passed since the last frame</param>
    protected abstract void OnUpdate(float dt);
}