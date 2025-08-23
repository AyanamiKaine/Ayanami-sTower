using System;
using System.Numerics;
using AyanamisTower.StellaEcs.HighPrecisionMath;

namespace AyanamisTower.StellaEcs.StellaInvicta;


/// <summary>
/// Represents a camera in 3D space with FPS-style controls.
/// </summary>
public class Camera
{
    private Vector3Double _position = new(0, 0, 5);
    private double _yaw = 0f; // Rotation around Y-axis (left/right)
    private double _pitch = 0f; // Rotation around X-axis (up/down)
    private Vector3Double _forward = -Vector3Double.UnitZ;
    private Vector3Double _right = Vector3Double.UnitX;
    private Vector3Double _up = Vector3Double.UnitY;

    /// <summary>
    /// Initializes a new instance of the <see cref="Camera"/> class.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="target"></param>
    /// <param name="up"></param>
    public Camera(Vector3Double position, Vector3Double target, Vector3Double up)
    {
        _position = position;
        _up = Vector3Double.Normalize(up);
        // Initialize orientation based on the desired target
        LookAt(target);
    }

    /// <summary>
    /// Position of the camera in 3D space.
    /// </summary>
    public Vector3Double Position
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
    public Vector3Double Target => _position + _forward;

    /// <summary>
    /// Up direction of the camera.
    /// </summary>
    public Vector3Double Up => _up;

    /// <summary>
    /// Forward direction of the camera.
    /// </summary>
    public Vector3Double Forward => _forward;

    /// <summary>
    /// Right direction of the camera.
    /// </summary>
    public Vector3Double Right => _right;

    /// <summary>
    /// Yaw rotation in radians (rotation around Y-axis).
    /// </summary>
    public double Yaw
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
    public double Pitch
    {
        get => _pitch;
        set
        {
            _pitch = Math.Clamp(value, -Math.PI / 2f + 0.01f, Math.PI / 2f - 0.01f);
            UpdateVectors();
        }
    }
    /// <summary>
    /// Field of view of the camera.
    /// </summary>
    public double Fov { get; set; } = MathF.PI / 3f;
    /// <summary>
    /// Aspect ratio of the camera.
    /// </summary>
    public double Aspect { get; set; } = 16f / 9f;
    /// <summary>
    /// Near clipping plane distance.
    /// </summary>
    public double Near { get; set; } = 0.1f;
    /// <summary>
    /// Far clipping plane distance.
    /// </summary>
    public double Far { get; set; } = 20f;

    /// <summary>
    /// Gets the view matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4Double GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    /// <summary>
    /// Gets the projection matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4Double GetProjectionMatrix()
    {
        return Matrix4x4Double.CreatePerspectiveFieldOfView(Fov, Aspect, Near, Far);
    }

    /// <summary>
    /// Gets the view-projection matrix for the camera.
    /// </summary>
    /// <returns></returns>
    public Matrix4x4Double GetViewProjectionMatrix()
    {
        return GetViewMatrix() * GetProjectionMatrix();
    }

    /// <summary>
    /// Gets the inverse of the view matrix (camera world transform).
    /// </summary>
    public Matrix4x4Double GetCameraWorldMatrix()
    {
        Matrix4x4Double.Invert(GetViewMatrix(), out var inv);
        return inv;
    }

    /// <summary>
    /// Updates the camera's forward, right, and up vectors based on yaw and pitch.
    /// </summary>
    private void UpdateVectors()
    {
        // Calculate forward vector from yaw and pitch
        var forward = new Vector3Double(
            Math.Cos(_pitch) * Math.Cos(_yaw),
            Math.Sin(_pitch),
            Math.Cos(_pitch) * Math.Sin(_yaw)
        );
        _forward = Vector3Double.Normalize(forward);

        // Calculate right vector (cross product of world up and forward)
        _right = Vector3Double.Normalize(Vector3Double.Cross(_forward, Vector3Double.UnitY));

        // Calculate up vector (cross product of right and forward)
        _up = Vector3Double.Normalize(Vector3Double.Cross(_right, _forward));
    }

    /// <summary>
    /// Moves the camera relative to its current orientation.
    /// </summary>
    /// <param name="forward">Forward/backward movement</param>
    /// <param name="right">Left/right movement</param>
    /// <param name="up">Up/down movement</param>
    public void MoveRelative(double forward, double right, double up)
    {
        _position += _forward * forward + _right * right + Vector3Double.UnitY * up;
    }

    /// <summary>
    /// Moves the camera by a world-space delta vector.
    /// </summary>
    /// <param name="delta"></param>
    public void Move(Vector3Double delta)
    {
        _position += delta;
    }

    /// <summary>
    /// Rotates the camera by the given yaw and pitch deltas.
    /// </summary>
    /// <param name="deltaYaw">Change in yaw (left/right)</param>
    /// <param name="deltaPitch">Change in pitch (up/down)</param>
    public void Rotate(double deltaYaw, double deltaPitch)
    {
        Yaw += deltaYaw;
        Pitch += deltaPitch;
    }

    /// <summary>
    /// Rotates the camera around the Y-axis (legacy method for compatibility).
    /// </summary>
    /// <param name="angle"></param>
    public void RotateY(double angle)
    {
        Yaw += angle;
    }

    /// <summary>
    /// Looks at a specific target point.
    /// </summary>
    /// <param name="target">The point to look at</param>
    public void LookAt(Vector3Double target)
    {
        var direction = Vector3Double.Normalize(target - _position);

        // Calculate yaw and pitch from direction vector
        _yaw = Math.Atan2(direction.Z, direction.X);
        _pitch = Math.Asin(direction.Y);

        UpdateVectors();
    }
}
