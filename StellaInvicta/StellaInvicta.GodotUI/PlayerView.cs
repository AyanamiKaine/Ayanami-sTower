using Godot;
using System;
namespace StellaInvicta.GodotUI
{

    public partial class PlayerView : Node3D
    {
        [Export]
        public NodePath TargetNodePath; // Path to the Node3D you want to orbit

        private Node3D _target;
        [Export]
        private float _orbitAngle = 0; // Current angle of rotation
        [Export]
        private float _orbitSpeed = 0.5f; // Speed of rotation (adjust as needed)
        [Export]
        private float _orbitRadius = 5.0f; // Distance from the target (adjust as needed)

        public override void _Ready()
        {
            _target = GetNode<Node3D>(TargetNodePath);
            if (_target == null)
            {
                GD.PrintErr("Target node not found!");
            }
        }

        public override void _Process(double delta)
        {
            if (_target != null)
            {
                _orbitAngle += _orbitSpeed * (float) delta;

                // Calculate the new camera position based on the orbit angle and radius
                Vector3 orbitPosition = new Vector3(
                _orbitRadius * Mathf.Cos(_orbitAngle),
                GlobalPosition.Y, // Maintain the same Y position
                _orbitRadius * Mathf.Sin(_orbitAngle)
            );

                // Set the camera's global position and make it look at the target
                GlobalPosition = _target.GlobalPosition + orbitPosition;
                LookAt(_target.GlobalPosition, Vector3.Up);
            }
        }
    }
}