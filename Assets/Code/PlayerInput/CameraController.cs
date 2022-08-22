using UnityEngine;
using Mirror;
using Assets.Code.Networking.Messaging;

namespace Assets.Code.PlayerInput
{
    /// <summary>
    /// A GameObject component to allow players to control their camera.
    /// This should be attached to the GameObject with the Camera component.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Tooltip("How fast should the camera move?")]
        [SerializeField] private float _panSpeed;

        [Tooltip("How fast should the camera zoom in and out?")]
        [SerializeField] private float _zoomSpeed;

        [Tooltip("How far in should the camera zoom?")]
        [SerializeField] private float _minOrthographicSize;

        [Tooltip("How far out should the camera zoom?")]
        [SerializeField] private float _maxOrthographicSize;

        [Tooltip("The camera component to control.")]
        [SerializeField] private Camera _cam;

        /// <summary>
        /// The target position to move to.
        /// </summary>
        private Vector3 _targetPosition;

        /// <summary>
        /// The camera's current speed.
        /// </summary>
        private Vector3 velocity;

        /// <summary>
        /// The smoothing amount to be applied to camera movement.
        /// A smaller value means the target position will be reached faster.
        /// </summary>
        private const float SmoothSpeed = 0.1f;

        /// <summary>
        /// The target zoom / orthographic camera size.
        /// </summary>
        private float _targetSize;

        /// <summary>
        /// The camera's current scroll speed.
        /// </summary>
        private float _scrollVelocity;

        /// <summary>
        /// Set the initial target values and register the set camera view 
        /// handler method.
        /// </summary>
        private void Awake()
        {
            _targetPosition = transform.position;
            _targetSize = _cam.orthographicSize;
            NetworkClient.RegisterHandler<SetCameraViewMessage>(SetCameraPositionFromServer);
        }

        /// <summary>
        /// Move the camera based on input every frame.
        /// </summary>
        private void Update()
        {
            MoveCamera();   
        }

        /// <summary>
        /// Move the camera when WASD or arrow keys are pressed.
        /// Zoom in and out when the scroll wheel is moved.
        /// </summary>
        private void MoveCamera()
        {
            Vector3 axisInputs = new Vector2(Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            // Set target position based on input for frame.
            if (axisInputs.y > 0f)
            {
                _targetPosition.z += _panSpeed * Time.deltaTime;
            }
            else if (axisInputs.y < 0f)
            {
                _targetPosition.z -= _panSpeed * Time.deltaTime;
            }
            if (axisInputs.x > 0f)
            {
                _targetPosition.x += _panSpeed * Time.deltaTime;
            } 
            else if (axisInputs.x < 0)
            {
                _targetPosition.x -= _panSpeed * Time.deltaTime;
            }

            // Set target orthographic size based on scroll input for frame.
            _targetSize += -Input.mouseScrollDelta.y * _zoomSpeed;
            _targetSize = Mathf.Clamp(_targetSize, _minOrthographicSize, _maxOrthographicSize);
            // Assign new pan speed based on current target size. Makes rapid zooming in/out faster.
            _panSpeed = _targetSize * 1.5f + 1;

            // Finally, make adjustments to the camera's position and 
            // orthographic size for this frame.
            transform.position = Vector3.SmoothDamp(transform.position,
                _targetPosition, ref velocity, SmoothSpeed);
            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize,
                _targetSize, ref _scrollVelocity, SmoothSpeed);
            // Adjust y coordinate to imitate zoom for spatial audio listeners.
            transform.position = new Vector3(transform.position.x,
                _cam.orthographicSize * 2, transform.position.z);
        }

        /// <summary>
        /// Set the camera position to a given position.
        /// </summary>
        /// <param name="position">The position to set the camera to.</param>
        public void SetCameraPosition(Vector3 position)
        {
            _targetPosition = position;
            transform.position = position;
        }

        /// <summary>
        /// Set the camera position to a given position after receiving a 
        /// SetCameraViewMessage from the server.
        /// </summary>
        /// <param name="position">The position to set the camera to.</param>
        public void SetCameraPositionFromServer(NetworkConnection conn,
            SetCameraViewMessage msg)
        {
            SetCameraPosition(msg.Position);
            Debug.Log("Set camera position from server to: " + msg.Position);
        }
    }
}