using System;
using OpenTK.Mathematics;

namespace Terr3D.Server.Shared;

public static class VectorHelpers
{

    public static Vector3 MoveTowards(this Vector3 pos, Vector3 target, float speed, float dt)
    {
        Vector3 toTarget = target - pos;
        float distance = toTarget.Length;

        float moveStep = speed * dt;

        if (distance <= moveStep || distance == 0f)
        {
            return target;
        }
        else
        {
            Vector3 direction = toTarget / distance;
            return pos + (direction * moveStep);
        }
    }

    public static Quaternion RotateTowardsY(this Quaternion currentRotation, Vector3 eyePos, Vector3 targetPos, float speed, float dt)
    {
        Vector3 toTarget = targetPos - eyePos;
        toTarget.Y = 0f;
        
        if (toTarget.Length == 0f)
        {
            return currentRotation;
        }

        toTarget = toTarget.Normalized();
        
        Quaternion targetRotation = Quaternion.FromAxisAngle(Vector3.UnitY, MathF.Atan2(toTarget.X, toTarget.Z));
        
        float rotationStep = speed * dt;
        return Quaternion.Slerp(currentRotation, targetRotation, MathF.Min(rotationStep, 1f));
    }

    
}