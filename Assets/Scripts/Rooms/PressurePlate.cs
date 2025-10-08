using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Quién la activa")]
    public LayerMask activatorLayers;         // Player, Default, Props...
    public bool requireMass = false;
    public float minMass = 10f;               // si requireMass, filtra por masa

    [Header("Estado")]
    public bool isPressed = false;

    [Header("Feedback (opcional)")]
    public Transform visual;                  // baja/sube este transform
    public float pressedOffsetY = -0.05f;
    public AudioSource sfxPress, sfxRelease;

    int insideCount = 0;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        visual = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActivator(other)) return;
        insideCount++;
        UpdateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsActivator(other)) return;
        insideCount = Mathf.Max(0, insideCount - 1);
        UpdateState();
    }

    bool IsActivator(Collider col)
    {
        if ((activatorLayers.value & (1 << col.gameObject.layer)) == 0) return false;
        if (!requireMass) return true;
        var rb = col.attachedRigidbody;
        return rb && rb.mass >= minMass;
    }

    void UpdateState()
    {
        bool newPressed = insideCount > 0;
        if (newPressed == isPressed) return;
        isPressed = newPressed;

        if (visual)
        {
            var p = visual.localPosition;
            p.y = isPressed ? pressedOffsetY : 0f;
            visual.localPosition = p;
        }
        if (isPressed) sfxPress?.Play(); else sfxRelease?.Play();

        // Avisar a quien escuche
        SendMessageUpwards("OnPlateStateChanged", this, SendMessageOptions.DontRequireReceiver);
    }
}