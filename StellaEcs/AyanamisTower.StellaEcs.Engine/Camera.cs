using System;
using System.Numerics;

namespace AyanamisTower.StellaEcs.Engine;

/// <summary>
/// Represents a camera in 3D space with FPS-style controls.
/// </summary>
public class Camera
{
    private Vector3 _position = new Vector3(0, 0, 5);
    private float _yaw = 0f; // Rotation around Y-axis (left/right)
    private float _pitch = 0f; // Rotation around X-axis (up/down)
    private Vector3 _forward = -Vector3.UnitZ;
    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;

    /// <summary>
    /// Position of the camera in 3D space.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            UpdateVectors();
        }
    }

    /// <summary>
    /// Target point the camera is looking at (computed from position + forward).
    /// </summary>
    public Vector3 Target => _position + _forward;

    /// <summary>
    /// Up direction of the camera.
    /// </summary>
    public Vector3 Up => _up;

    /// <summary>
    /// Forward direction of the camera.
    /// </summary>
    public Vector3 Forward => _forward;

    /// <summary>
    /// Right direction of the camera.
    /// </summary>
    public Vector3 Right => _right;

    /// <summary>
    /// Yaw rotation in radians (rotation around Y-axis).
    /// </summary>
    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            UpdateVectors();
        }
    }

    /// <summary>
    /// Pitch rotation in radians (rotation around X-axis), clamped to prevent gimbal lock.
    /// </summary>
    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = Math.Clamp(value, -MathF.PI / 2f + 0.01f, MathF.PI / 2f - 0.01f);
            UpdateVectors();
        }
    }
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
    /// Updates the camera's forward, right, and up vectors based on yaw and pitch.
    /// </summary>
    private void UpdateVectors()
    {
        // Calculate forward vector from yaw and pitch
        var forward = new Vector3(
            MathF.Cos(_pitch) * MathF.Cos(_yaw),
            MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Sin(_yaw)
        );
        _forward = Vector3.Normalize(forward);

        // Calculate right vector (cross product of world up and forward)
        _right = Vector3.Normalize(Vector3.Cross(_forward, Vector3.UnitY));

        // Calculate up vector (cross product of right and forward)
        _up = Vector3.Normalize(Vector3.Cross(_right, _forward));
    }

    /// <summary>
    /// Moves the camera relative to its current orientation.
    /// </summary>
    /// <param name="forward">Forward/backward movement</param>
    /// <param name="right">Left/right movement</param>
    /// <param name="up">Up/down movement</param>
    public void MoveRelative(float forward, float right, float up)
    {
        _position += _forward * forward + _right * right + Vector3.UnitY * up;
    }

    /// <summary>
    /// Moves the camera by a world-space delta vector.
    /// </summary>
    /// <param name="delta"></param>
    public void Move(Vector3 delta)
    {
        _position += delta;
    }

    /// <summary>
    /// Rotates the camera by the given yaw and pitch deltas.
    /// </summary>
    /// <param name="deltaYaw">Change in yaw (left/right)</param>
    /// <param name="deltaPitch">Change in pitch (up/down)</param>
    public void Rotate(float deltaYaw, float deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch += deltaPitch;
    }

    /// <summary>
    /// Rotates the camera around the Y-axis (legacy method for compatibility).
    /// </summary>
    /// <param name="angle"></param>
    public void RotateY(float angle)
    {
        Yaw += angle;
    }

    /// <summary>
    /// Looks at a specific target point.
    /// </summary>
    /// <param name="target">The point to look at</param>
    public void LookAt(Vector3 target)
    {
        var direction = Vector3.Normalize(target - _position);

        // Calculate yaw and pitch from direction vector
        _yaw = MathF.Atan2(direction.Z, direction.X);
        _pitch = MathF.Asin(direction.Y);

        UpdateVectors();
    }
}

