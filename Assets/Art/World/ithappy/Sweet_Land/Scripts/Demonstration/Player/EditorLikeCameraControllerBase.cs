using UnityEngine;

namespace ithappy
{
    public class EditorLikeCameraControllerBase : MonoBehaviour
    {
        [Header("Movement Settings")] 
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _fastMoveMultiplier = 2f;
        [SerializeField] private float _rotationSpeed = 2f;

        [Header("Zoom Settings")] 
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minZoomDistance = 2f;
        [SerializeField] private float _maxZoomDistance = 50f;

        private Transform _cameraTransform;
        private Transform _pivot;
        private Vector3 _moveVector;
        protected bool _isRotating;
        private float _rotationX;

        protected virtual void Awake()
        {
            _pivot = new GameObject("Camera Pivot").transform;
            _pivot.position = transform.position;
            _pivot.rotation = transform.rotation;
            
            _cameraTransform = GetComponentInChildren<Camera>().transform;
            _cameraTransform.SetParent(_pivot);
            _cameraTransform.localPosition = new Vector3(0, 0, -10f);
            _cameraTransform.LookAt(_pivot.position);

            _rotationX = _pivot.eulerAngles.x;
        }

        protected void HandleMovement(Vector3 moveDirection, bool isShifted)
        {
            float speed = _moveSpeed * (isShifted ? _fastMoveMultiplier : 1f);
            
            _moveVector.x = moveDirection.x;
            _moveVector.z = moveDirection.z;
            _moveVector.y = 0;
            
            _pivot.Translate(_moveVector * (speed * Time.deltaTime), Space.Self);
            _pivot.Translate(Vector3.up * moveDirection.y * (speed * Time.deltaTime), Space.World);
        }

        protected void HandleRotation(Vector2 rotationDirection)
        {
            if (_isRotating)
            {
                float mouseX = rotationDirection.x * _rotationSpeed;
                float mouseY = rotationDirection.y * _rotationSpeed;
                
                float rotationY = _pivot.eulerAngles.y + mouseX;
                
                _rotationX -= mouseY;
                _rotationX = Mathf.Clamp(_rotationX, -89f, 89f);
                
                _pivot.rotation = Quaternion.Euler(_rotationX, rotationY, 0);
            }
        }

        protected void HandleZoom(float scroll)
        {
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 zoomDirection = _cameraTransform.localPosition.normalized;
                float currentDistance = _cameraTransform.localPosition.magnitude;
                float newDistance = Mathf.Clamp(currentDistance - scroll * _zoomSpeed, _minZoomDistance, _maxZoomDistance);

                _cameraTransform.localPosition = zoomDirection * newDistance;
            }
        }
    }
}