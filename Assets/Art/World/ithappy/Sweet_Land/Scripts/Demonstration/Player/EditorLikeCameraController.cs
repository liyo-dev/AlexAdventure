using UnityEngine;

namespace ithappy
{
    public class EditorLikeCameraController : EditorLikeCameraControllerBase
    {

        private void LateUpdate()
        {
            Vector3 moveDirection = Vector3.zero;
            moveDirection.x = Input.GetAxis("Horizontal");
            moveDirection.z = Input.GetAxis("Vertical");
            moveDirection.y = Input.GetKey(KeyCode.Q) ? -1 : Input.GetKey(KeyCode.E) ? 1 : 0;
            HandleMovement(moveDirection, Input.GetKey(KeyCode.LeftShift));
            
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _isRotating = true;
            }
            
            if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _isRotating = false;
            }
            HandleRotation(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")));
            HandleZoom(Input.GetAxis("Mouse ScrollWheel"));
        }
    }
}