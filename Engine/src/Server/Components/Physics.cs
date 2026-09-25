using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Terr3D.Client;
using Terr3D.Client.Resources;
using Terr3D.Server.Entities;
using Terr3D.Server.Shared;
using Terr3D.Utils;

namespace Terr3D.Server.Components;

/// <summary>
/// This component provides physics logic for entities. They will be effected by gravity and stay above the terrain
/// </summary>
public class Physics : BehaviourComponent
{

    public static readonly Vector3 Gravity = new(0, -9.81f, 0);

    /// <summary>
    /// The air drag
    /// </summary>
    public float Drag = 0.5f;

    /// <summary>
    /// The damping force when the body is on the ground
    /// </summary>
    public float Friction = 5.0f;


    public float SlopePushForce = 150f;

    /// <summary>
    /// The maximum slope this body is able to climb without sliding
    /// </summary>
    public float SlopeLimit = MathHelper.DegreesToRadians(45);

    /// <summary>
    /// Should the entity fall, or stick to the floor?
    /// </summary>
    public bool entityFalls = true;

    /// <summary>
    /// Is the entity grounded now?
    /// </summary>
    public bool IsGrounded => true;//MathF.Abs(Transform.Position.Y - Entity.Onstage.Globals.Worldspawn.SampleHeight(Transform.Position)) < 0.5f; TODO: finish

    public AABB_Collider? Collider { get; set; }

    public Vector3 Velocity => _velocity;
    Vector3 _velocity = new();

    /// <summary>
    /// Accelerates entity with the given vector
    /// </summary>
    /// <param name="pushVector">The acceleration vector</param>
    public void Push(Vector3 pushVector)
    {
        _velocity += pushVector;
    }

    public override void Update(float dt)
    {
        /*if(Program.DEBUG_FLAG)
            DebugControls(dt);

        //Apply gravity
        if (entityFalls)
            _velocity += Gravity * dt;
        else
            Transform.Position = new(Transform.Position.X, Entity.Onstage.Globals.Worldspawn.SampleHeight(Transform.Position), Transform.Position.Z);

        //Apply drag
        _velocity -= _velocity * Drag * dt;

        //cache the ground normal and terrain steepness
        var normal = 0f;//Entity.Onstage.Globals.Worldspawn.GetTerrainNormal(Transform.Position); //TODO: fix
        float steepness = Vector3.Dot(Vector3.UnitY, normal);

        //If we are grounded, apply friction
        if (IsGrounded)
        {
            ApplyFriction(dt, steepness);
        }

        //Is the ground too steep?
        if (Vector3.CalculateAngle(Vector3.UnitY, normal) > SlopeLimit && IsGrounded)
        {
            //push downhill
            float force = (1 - steepness) * 5;
            _velocity.X += (1f - normal.Y) * normal.X * SlopePushForce * dt * force;
            _velocity.Z += (1f - normal.Y) * normal.Z * SlopePushForce * dt * force;
            _velocity.Y += -2f * SlopePushForce * dt * force;
        }

        //Move the body
        MoveAndResolveCollisions(dt);

        //cache the ground height
        float groundHeight = Entity.Onstage.Globals.Worldspawn.SampleHeight(Transform.Position);


        if (Transform.Position.Y < groundHeight)
        {
            Transform.Position = new Vector3(Transform.Position.X, groundHeight, Transform.Position.Z);
            _velocity.Y = MathF.Max(0, _velocity.Y);
        }

        //disallow the body to leave the bounds
        var worldExtents = Entity.Onstage.Globals.Worldspawn.TerrainSize / 2f - 0.1f;
        if (Math.Abs(Transform.Position.X) >= worldExtents || Math.Abs(Transform.Position.Z) >= worldExtents)
        {
            Transform.Position = new Vector3(
                Math.Clamp(Transform.Position.X, -worldExtents, worldExtents),
                Transform.Position.Y,
                Math.Clamp(Transform.Position.Z, -worldExtents, worldExtents)
            );
        }*/
    }


    private void MoveAndResolveCollisions(float dt)
    {
       /* Transform.Position += _velocity * dt;
        if (Collider == null) return;

        var colliders = Entity.Onstage.Partitioner.QueryCollision(Collider.AABB);
        foreach (var col in colliders)
        {
            var currentHull = Collider.AABB;
            var hit = col.Collide(currentHull);

            if (hit.Depth > 0)
            {
                Entity.Onstage.Globals.Canvas.RenderLabel($"Colliding with {col}", 5);
                Entity.Onstage.Globals.Canvas.RenderLabel($"{hit.Normal} {hit.Depth}", 6);

                const float epsilon = 0.001f;
                Transform.Position += hit.Normal * (hit.Depth + epsilon);

                float velocityAlongNormal = Vector3.Dot(_velocity, hit.Normal);

                if (velocityAlongNormal < 0)
                {
                    _velocity -= hit.Normal * velocityAlongNormal;
                }
            }
        }
*/




    }

    private void ApplyFriction(float dt, float steepness)
    {
        Vector3 horizontalVel = new(_velocity.X, 0, _velocity.Z);
        float speed = horizontalVel.Length;
        //IF horizontal rate is greater than zero, we apply friction
        if (speed > 0)
        {
            steepness = MathF.Min(steepness / 20, 1);
            float drop = Friction * dt * (check ? 1 - steepness : 1);
            float newSpeed = Math.Max(0, speed - drop);

            _velocity.X *= newSpeed / speed;
            _velocity.Z *= newSpeed / speed;
        }
    }
    bool check = false;
    private void DebugControls(float dt)
    {
        Vector3 move = new();
        if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.LeftShift))
        {
            if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Left)) move.X = -1; ;
            if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Right)) move.X = 1;
            if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Up)) move.Z = -1;
            if (EngineWindow.Instance.KeyboardState.IsKeyDown(Keys.Down)) move.Z = 1;
        }

        _velocity += move * dt * 10;
    }


}