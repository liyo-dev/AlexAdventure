using ithappy;
using UnityEngine;

namespace ithappy
{
    public abstract class PlayerCharacterInputBase : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] protected float rotationSpeed = 3f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private Transform _cameraParent;
        [SerializeField] private CharacterAnimator _characterAnimator;
        [SerializeField] private float accelerationTime = 0.1f;
        [SerializeField] private float decelerationTime = 0.1f;
        [SerializeField] private float airControlForce = 5f;

        [Header("Camera Collision")] [SerializeField]
        private float cameraDistance = 3f;

        [SerializeField] private float cameraCollisionOffset = 0.2f;
        [SerializeField] private float cameraMinDistance = 0.5f;
        [SerializeField] private LayerMask cameraCollisionLayer;

        private Transform cameraTransform;
        private Rigidbody rb;
        private float rotationX = 0f;
        private bool isGrounded;
        private Vector3 originalCameraLocalPos;
        private float currentCameraDistance;
        private float currentMoveSpeed;
        private float currentAnimationMoveSpeed;
        private Vector3 lastMoveDirection;
        private Vector3 targetMoveDirection;
        private bool isMoving;
        private float sphereRadius = 0.2f;
        private Transform thisTransform;
        protected float cachedMouseX;
        protected float cachedMouseY;
        protected float cachedHorizontal;
        protected float cachedVertical;

        public Transform CameraParent => _cameraParent;

        protected virtual void Awake()
        {
            thisTransform = transform;
            rb = GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            cameraTransform = Camera.main.transform;
            originalCameraLocalPos = cameraTransform.localPosition;
            currentCameraDistance = cameraDistance;
            currentMoveSpeed = 0f;
            currentAnimationMoveSpeed = 0f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected virtual void OnDestroy()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void FixedUpdate()
        {
            CheckGrounded();

            if (isGrounded)
            {
                HandleGroundMovement();
            }
            else
            {
                HandleAirMovement();
            }

            HandleRotation();
            HandleCameraCollision();
        }
        
        protected virtual void Update()
        {
            UpdateAnimationSpeed();
        }

        private void HandleGroundMovement()
        {
            targetMoveDirection = (cameraTransform.forward * cachedVertical + cameraTransform.right * cachedHorizontal)
                .normalized;
            targetMoveDirection.y = 0f;

            bool hasInput = targetMoveDirection.magnitude > 0.1f;

            if (hasInput)
            {
                lastMoveDirection = targetMoveDirection;
                isMoving = true;
                currentMoveSpeed = moveSpeed;
                currentAnimationMoveSpeed = Mathf.Lerp(currentAnimationMoveSpeed, moveSpeed,
                    Time.deltaTime * (1f / accelerationTime));

                Vector3 moveVelocity = lastMoveDirection * currentMoveSpeed;
                rb.linearVelocity = new Vector3(moveVelocity.x, rb.linearVelocity.y, moveVelocity.z);
            }
            else
            {
                currentMoveSpeed = 0f;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

                if (isMoving)
                {
                    currentAnimationMoveSpeed = Mathf.Lerp(currentAnimationMoveSpeed, 0f,
                        Time.deltaTime * (1f / decelerationTime));

                    if (currentAnimationMoveSpeed < 0.01f)
                    {
                        currentAnimationMoveSpeed = 0f;
                        isMoving = false;
                    }
                }
            }
        }

        private void HandleAirMovement()
        {
            if (Mathf.Abs(cachedHorizontal) > 0.1f || Mathf.Abs(cachedVertical) > 0.1f)
            {
                Vector3 airDirection =
                    (cameraTransform.forward * cachedVertical + cameraTransform.right * cachedHorizontal).normalized;
                airDirection.y = 0f;

                rb.AddForce(airDirection * airControlForce, ForceMode.Force);

                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                if (horizontalVelocity.magnitude > moveSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * moveSpeed;
                    rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
                }
            }

            currentAnimationMoveSpeed = Mathf.Lerp(currentAnimationMoveSpeed, 0f,
                Time.deltaTime * (1f / decelerationTime));
        }
        
        private void UpdateAnimationSpeed()
        {
            _characterAnimator.SetMoveSpeed(currentAnimationMoveSpeed);
        }

        private void HandleRotation()
        {
            thisTransform.Rotate(Vector3.up * cachedMouseX);
            
            rotationX -= cachedMouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            _cameraParent.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
        
        protected void HandleJump()
        {
            if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _characterAnimator.Jump();
            }
        }
        
        private void CheckGrounded()
        {
            Vector3 origin = thisTransform.position + Vector3.up * (sphereRadius * 2);
    
            isGrounded = Physics.SphereCast(origin, 
                sphereRadius, 
                Vector3.down, 
                out RaycastHit hit, 
                sphereRadius + groundCheckDistance, 
                groundLayer);
            
            Debug.DrawRay(origin, Vector3.down * (sphereRadius + groundCheckDistance), 
                isGrounded ? Color.green : Color.red);
        }
        
        protected void HandleAnimations()
        {
            _characterAnimator.Hello();
        }
        
        private void HandleCameraCollision()
        {
            RaycastHit hit;
            Vector3 cameraDesiredPosition = _cameraParent.TransformPoint(new Vector3(0, 0, -cameraDistance));

            if (Physics.Linecast(_cameraParent.position, cameraDesiredPosition, out hit, cameraCollisionLayer))
            {
                currentCameraDistance = Mathf.Clamp(
                    (hit.distance - cameraCollisionOffset),
                    cameraMinDistance,
                    cameraDistance);
            }
            else
            {
                currentCameraDistance = cameraDistance;
            }

            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition,
                new Vector3(0, 0, -currentCameraDistance),
                Time.deltaTime * 10f);
        }

        private void OnDrawGizmosSelected()
        {
            float sphereRadius = 0.2f;
            Vector3 origin = transform.position + Vector3.up * (sphereRadius * 2);
    
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin + Vector3.down * (sphereRadius + groundCheckDistance), sphereRadius);
            Gizmos.DrawLine(origin, origin + Vector3.down * (sphereRadius + groundCheckDistance));

            if (_cameraParent != null)
            {
                Gizmos.color = Color.blue;
                Vector3 cameraDesiredPosition = _cameraParent.TransformPoint(new Vector3(0, 0, -cameraDistance));
                Gizmos.DrawLine(_cameraParent.position, cameraDesiredPosition);
            }
        }
    }
}