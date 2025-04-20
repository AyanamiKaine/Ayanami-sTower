using System;
using System.Numerics; // Using System.Numerics for Vector3, Quaternion, Matrix4x4

namespace AyanamisTower.NihilEx.ECS
{
    /// <summary>
    /// Defines the type of projection the camera uses.
    /// </summary>
    public enum ProjectionType
    {
        /// <summary>
        /// Perspective projection simulates how human vision works, where objects appear smaller further away.
        /// </summary>
        Perspective,
        /// <summary>
        /// Orthographic projection renders objects without perspective distortion, useful for 2D games or technical visualizations.
        /// </summary>
        Orthographic
    }


    /*
    This is a struct so we can define the members of it using world.component
    this allows us to set the camera component using the flecs explorer.

    I find it really important to quickly change the data of our components.
    */

    /// <summary>
    /// Represents a camera in 3D space, handling view and projection transformations.
    /// </summary>
    public struct Camera
    {
        // --- Core Transform Properties ---
        private Vector3 _position = Vector3.Zero; // Default to origin
        private Quaternion _orientation = Quaternion.Identity; // Default to no rotation
        private Vector3 _worldUpDirection = Vector3.UnitY; // Defines the 'up' direction in the world, used for CreateLookAt

        // --- Projection Properties ---
        private ProjectionType _projectionType = ProjectionType.Perspective;
        private float _fieldOfViewRadians = MathF.PI / 4.0f; // Default: 45 degrees vertical FOV
        private float _aspectRatio = 16.0f / 9.0f; // Common default aspect ratio
        private float _nearPlaneDistance = 0.1f;   // Default near clipping plane
        private float _farPlaneDistance = 1000.0f; // Default far clipping plane

        // --- Cached Matrices ---
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;
        private Matrix4x4 _viewProjectionMatrix; // Combined view * projection

        // --- State Flags ---
        private bool _viewDirty = true;       // Does the view matrix need recalculation?
        private bool _projectionDirty = true; // Does the projection matrix need recalculation?

        #region Public Properties (Position, Orientation, Projection Settings)

        /// <summary>
        /// Gets or sets the camera's position in world space.
        /// Marks the View matrix as dirty when changed.
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _viewDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the camera's orientation using a Quaternion.
        /// Marks the View matrix as dirty when changed.
        /// </summary>
        public Quaternion Orientation
        {
            get => _orientation;
            set
            {
                // Normalize to ensure it remains a valid rotation quaternion
                var normalizedValue = Quaternion.Normalize(value);
                if (_orientation != normalizedValue)
                {
                    _orientation = normalizedValue;
                    _viewDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 'up' direction vector in world space. Typically Vector3.UnitY.
        /// Used to orient the camera correctly when calculating the view matrix.
        /// Marks the View matrix as dirty when changed.
        /// </summary>
        public Vector3 WorldUpDirection
        {
            get => _worldUpDirection;
            set
            {
                // Normalize for safety, although typically unit vectors are used
                var normalizedValue = Vector3.Normalize(value);
                if (_worldUpDirection != normalizedValue)
                {
                    _worldUpDirection = normalizedValue;
                    _viewDirty = true;
                }
            }
        }


        /// <summary>
        /// Gets or sets the type of projection (Perspective or Orthographic).
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public ProjectionType ProjectionMode
        {
            get => _projectionType;
            set
            {
                if (_projectionType != value)
                {
                    _projectionType = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the vertical field of view in degrees (for Perspective projection).
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public float FieldOfViewDegrees
        {
            get => _fieldOfViewRadians * (180.0f / MathF.PI); // Convert radians to degrees for getting
            set
            {
                // Convert degrees to radians for storing
                float radians = Math.Clamp(value, 1.0f, 179.0f) * (MathF.PI / 180.0f);
                if (_fieldOfViewRadians != radians)
                {
                    _fieldOfViewRadians = radians;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the vertical field of view in radians (for Perspective projection).
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public float FieldOfViewRadians
        {
            get => _fieldOfViewRadians;
            set
            {
                // Clamp values to prevent issues
                float clampedRadians = Math.Clamp(value, 0.01f, MathF.PI - 0.01f);
                if (_fieldOfViewRadians != clampedRadians)
                {
                    _fieldOfViewRadians = clampedRadians;
                    _projectionDirty = true;
                }
            }
        }


        /// <summary>
        /// Gets or sets the aspect ratio (Width / Height) of the view surface.
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public float AspectRatio
        {
            get => _aspectRatio;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Aspect ratio must be positive.");
                if (_aspectRatio != value)
                {
                    _aspectRatio = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance to the near clipping plane.
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public float NearPlane
        {
            get => _nearPlaneDistance;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Near plane distance must be positive.");
                if (_nearPlaneDistance != value)
                {
                    _nearPlaneDistance = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance to the far clipping plane.
        /// Marks the Projection matrix as dirty when changed.
        /// </summary>
        public float FarPlane
        {
            get => _farPlaneDistance;
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Far plane distance must be positive.");
                if (value <= _nearPlaneDistance) throw new ArgumentOutOfRangeException(nameof(value), "Far plane must be further than near plane.");
                if (_farPlaneDistance != value)
                {
                    _farPlaneDistance = value;
                    _projectionDirty = true;
                }
            }
        }

        #endregion

        #region Derived Direction Vectors

        /// <summary>
        /// Gets the camera's forward direction vector in world space, derived from its orientation.
        /// Assumes a right-handed coordinate system where -Z is forward in view space.
        /// </summary>
        public Vector3 Forward => Vector3.Normalize(Vector3.Transform(-Vector3.UnitZ, _orientation));

        /// <summary>
        /// Gets the camera's right direction vector in world space, derived from its orientation.
        /// Assumes a right-handed coordinate system where +X is right in view space.
        /// </summary>
        public Vector3 Right => Vector3.Normalize(Vector3.Transform(Vector3.UnitX, _orientation));

        /// <summary>
        /// Gets the camera's local up direction vector in world space, derived from its orientation.
        /// Assumes a right-handed coordinate system where +Y is up in view space.
        /// </summary>
        public Vector3 Up => Vector3.Normalize(Vector3.Transform(Vector3.UnitY, _orientation));

        #endregion

        #region Calculated Matrix Getters

        /// <summary>
        /// Gets the View matrix, recalculating it if necessary.
        /// Transforms coordinates from World Space to View (Camera) Space.
        /// </summary>
        public Matrix4x4 ViewMatrix
        {
            get
            {
                if (_viewDirty) UpdateViewMatrix();
                return _viewMatrix;
            }
        }

        /// <summary>
        /// Gets the Projection matrix, recalculating it if necessary.
        /// Transforms coordinates from View Space to Clip Space.
        /// </summary>
        public Matrix4x4 ProjectionMatrix
        {
            get
            {
                if (_projectionDirty) UpdateProjectionMatrix();
                return _projectionMatrix;
            }
        }

        /// <summary>
        /// Gets the combined View-Projection matrix, recalculating it if necessary.
        /// Transforms coordinates directly from World Space to Clip Space.
        /// </summary>
        public Matrix4x4 ViewProjectionMatrix
        {
            get
            {
                // If either dependent matrix is dirty, recalculate and combine
                if (_viewDirty || _projectionDirty)
                {
                    // Ensure underlying matrices are updated first
                    if (_viewDirty) UpdateViewMatrix();
                    if (_projectionDirty) UpdateProjectionMatrix();

                    // Calculate the combined matrix (Order is important: View * Projection)
                    _viewProjectionMatrix = Matrix4x4.Multiply(_viewMatrix, _projectionMatrix);
                }
                return _viewProjectionMatrix;
            }
        }

        #endregion

        #region Matrix Update Methods

        /// <summary>
        /// Recalculates the View matrix based on Position, Orientation, and WorldUpDirection.
        /// Uses the CreateLookAt method, determining the target point from the forward vector.
        /// </summary>
        public void UpdateViewMatrix()
        {
            // Determine the target point by looking 'forward' from the current position
            Vector3 lookAtTarget = _position + Forward;

            // Create the view matrix
            _viewMatrix = Matrix4x4.CreateLookAt(_position, lookAtTarget, _worldUpDirection);

            // Mark as clean
            _viewDirty = false;
            // Note: ViewProjectionMatrix getter will handle its own update when accessed
        }

        /// <summary>
        /// Recalculates the Projection matrix based on the ProjectionMode and relevant parameters.
        /// </summary>
        public void UpdateProjectionMatrix()
        {
            switch (_projectionType)
            {
                case ProjectionType.Perspective:
                    _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                        _fieldOfViewRadians,
                        _aspectRatio,
                        _nearPlaneDistance,
                        _farPlaneDistance);
                    break;

                case ProjectionType.Orthographic:
                    // Basic orthographic projection - requires defining width/height
                    // Example: Calculate width/height based on a desired vertical size
                    // float orthoViewHeight = 10.0f; // Or get from a property
                    // float orthoViewWidth = orthoViewHeight * _aspectRatio;
                    // _projectionMatrix = Matrix4x4.CreateOrthographic(orthoViewWidth, orthoViewHeight, _nearPlaneDistance, _farPlaneDistance);

                    // Placeholder: Use Identity or throw if Orthographic is selected but not fully supported yet
                    _projectionMatrix = Matrix4x4.Identity;
                    Console.Error.WriteLine("Warning: Orthographic projection not fully implemented in Camera.UpdateProjectionMatrix(). Using Identity.");
                    // Or throw new NotImplementedException("Orthographic projection calculation not implemented.");
                    break;
                default:
                    _projectionMatrix = Matrix4x4.Identity; // Should not happen
                    break;
            }

            // Mark as clean
            _projectionDirty = false;
            // Note: ViewProjectionMatrix getter will handle its own update when accessed
        }

        /// <summary>
        /// Ensures both View and Projection matrices (and therefore ViewProjection) are up-to-date.
        /// Call this if you need to guarantee freshness before getting matrix values,
        /// though the getters handle this automatically via dirty flags.
        /// </summary>
        public void EnsureMatricesUpdated()
        {
            if (_viewDirty) UpdateViewMatrix();
            if (_projectionDirty) UpdateProjectionMatrix();
            // Trigger the ViewProjection getter to force recalculation if needed
            _ = this.ViewProjectionMatrix;
        }


        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new Camera instance with default settings.
        /// </summary>
        public Camera()
        {
            // Matrices will be calculated on first access due to dirty flags
        }

        /// <summary>
        /// Creates a new Camera instance with specified initial transform and settings.
        /// </summary>
        /// <param name="position">Initial world position.</param>
        /// <param name="lookAtTarget">The point in world space the camera should initially look towards.</param>
        /// <param name="worldUp">The world's up direction (typically Vector3.UnitY).</param>
        /// <param name="aspectRatio">The viewport aspect ratio (width/height).</param>
        /// <param name="fovDegrees">Perspective field of view in degrees.</param>
        /// <param name="nearPlane">Near clip plane distance.</param>
        /// <param name="farPlane">Far clip plane distance.</param>
        public Camera(Vector3 position, Vector3 lookAtTarget, Vector3 worldUp, float aspectRatio, float fovDegrees = 45.0f, float nearPlane = 0.1f, float farPlane = 1000.0f)
        {
            _position = position;
            _worldUpDirection = Vector3.Normalize(worldUp);

            // Calculate initial orientation based on look-at target
            Matrix4x4 lookAtMatrix = Matrix4x4.CreateLookAt(position, lookAtTarget, worldUp);
            _orientation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(Matrix4x4.Transpose(lookAtMatrix))); // Transpose needed as CreateLookAt creates the View matrix

            _aspectRatio = aspectRatio;
            FieldOfViewDegrees = fovDegrees; // Use property setter for conversion/clamping
            _nearPlaneDistance = nearPlane;
            _farPlaneDistance = farPlane;

            // Ensure matrices are calculated based on these initial values
            _viewDirty = true;
            _projectionDirty = true;
            EnsureMatricesUpdated(); // Force calculation now
        }

        #endregion
    }
}