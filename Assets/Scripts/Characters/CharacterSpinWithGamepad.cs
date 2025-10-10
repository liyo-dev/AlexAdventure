using UnityEngine;

public class CharacterSpinWithGamepad : MonoBehaviour
{
    public float rotateSpeed = 120f;
    public float deadZone = 0.15f;
    private PlayerControls input;

    void Awake(){ input = new PlayerControls(); input.Enable(); }
    void OnDestroy(){ input?.Disable(); }

    void Update()
    {
        Vector2 look = input.GamePlay.CameraLook.ReadValue<Vector2>();
        float x = Mathf.Abs(look.x) > deadZone ? look.x : 0f;
        if (x != 0f)
            transform.Rotate(0f, x * rotateSpeed * Time.unscaledDeltaTime, 0f, Space.World);
    }
}