using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.Engine;

/// <summary>
/// Represents a camera in 3D space.
/// </summary>
public class Camera
{
    /// <summary>
    /// Position of the camera in 3D space.
    /// </summary>
    public Vector3 Position { get; set; } = new Vector3(0, 0, 5);
    /// <summary>
    /// Target point the camera is looking at.
    /// </summary>
    public Vector3 Target { get; set; } = Vector3.Zero;
    /// <summary>
    /// Up direction of the camera.
    /// </summary>
    public Vector3 Up { get; set; } = Vector3.UnitY;
    /// <summary>
    /// Field of view of the camera.
    /// </summary>
    public float Fov { get; set; } = MathF.PI / 3f;
    /// <summary>
    /// Aspect ratio of the camera.
    /// </summary>
    public float Aspect { get; set; } = 16f / 9f;
    /// <summary>
    /// Near clipping plane distance.
    /// </summary>
    public float Near { get; set; } = 0.1f;
    /// <summary>
    /// Far clipping plane distance.
    /// </summary>
    public float Far { get; set; } = 20f;

    /// <summary>
    /// Gets the view matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    /// <summary>
    /// Gets the projection matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(Fov, Aspect, Near, Far);
    }

    /// <summary>
    /// Gets the view-projection matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 GetViewProjectionMatrix()
    {
        return GetViewMatrix() * GetProjectionMatrix();
    }

    /// <summary>
    /// Moves the camera by a given delta.
    /// </summary>
    /// <param name="delta"></param>
    public void Move(Vector3 delta)
    {
        Position += delta;
        Target += delta;
    }

    /// <summary>
    /// Rotates the camera around the Y-axis.
    /// </summary>
    /// <param name="angle"></param>
    public void RotateY(float angle)
    {
        var dir = Target - Position;
        var rot = Matrix4x4.CreateFromAxisAngle(Up, angle);
        dir = Vector3.TransformNormal(dir, rot);
        Target = Position + dir;
    }
}

