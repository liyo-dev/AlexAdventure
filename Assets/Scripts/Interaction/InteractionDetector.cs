using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class InteractionDetector : MonoBehaviour
{
    [Header("Detección")]
    [SerializeField] private float range = 2.2f;
    [SerializeField] private float focusRadius = 0.35f;
    [SerializeField] private LayerMask interactableMask;
    [Tooltip("Opcional: origen/dirección para el ray (p.ej. pivot de cámara). Si está vacío usa el transform del Player.")]
    [SerializeField] private Transform aimSource;

    [Header("Input (Gamepad)")]
    [Tooltip("Acción GamePlay/Interact (mismo botón que Jump: A). Se habilita solo al enfocar.")]
    [SerializeField] private InputActionReference interactAction;
    [Tooltip("Acción GamePlay/Jump (A). Se deshabilita al enfocar para que no salte.")]
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private bool disableJumpWhenFocused = true;

    private Interactable current;

    private void OnEnable()
    {
        // Escuchamos Interact, pero la tendremos deshabilitada por defecto.
        if (interactAction?.action != null)
        {
            interactAction.action.performed += OnInteract;
            if (interactAction.action.enabled) interactAction.action.Disable();
        }
    }

    private void OnDisable()
    {
        if (interactAction?.action != null)
        {
            interactAction.action.performed -= OnInteract;
            // Deja Interact deshabilitada al salir
            if (interactAction.action.enabled) interactAction.action.Disable();
        }
        // Asegura que Jump vuelve habilitada
        if (disableJumpWhenFocused && jumpAction?.action != null && !jumpAction.action.enabled)
            jumpAction.action.Enable();
    }

    private void Update()
    {
        // Si hay diálogo abierto, no enfocamos nada
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            SetCurrent(null);
            return;
        }

        var nearest = FindNearest();
        SetCurrent(nearest);
    }

    private void OnInteract(InputAction.CallbackContext _)
    {
        if (current != null && current.CanInteract(gameObject))
            current.Interact(gameObject); // aquí ya abrirás diálogo, cofre, etc.
    }

    private void SetCurrent(Interactable next)
    {
        if (current == next) return;

        if (current) current.SetHintVisible(false);
        current = next;
        if (current) current.SetHintVisible(true);

        var ia = interactAction?.action;
        var ja = jumpAction?.action;

        // Habilita Interact SOLO con foco
        if (ia != null)
        {
            if (current && !ia.enabled) ia.Enable();
            else if (!current && ia.enabled) ia.Disable();
        }

        // Deshabilita Jump mientras hay foco (para que A no salte)
        if (disableJumpWhenFocused && ja != null)
        {
            if (current && ja.enabled) ja.Disable();
            else if (!current && !ja.enabled) ja.Enable();
        }
    }

    private Interactable FindNearest()
    {
        var t = aimSource ? aimSource : transform;
        Vector3 origin = t.position + Vector3.up * 1.1f;

        var cols = Physics.OverlapSphere(origin, range, interactableMask, QueryTriggerInteraction.Collide);
        if (cols == null || cols.Length == 0) return null;

        float best = float.MaxValue;
        Interactable winner = null;

        foreach (var c in cols)
        {
            var it = c.GetComponentInParent<Interactable>();
            if (!it || !it.CanInteract(gameObject)) continue;

            float d = Vector3.Distance(origin, it.transform.position);
            if (d < best)
            {
                Vector3 dir = (it.transform.position - origin).normalized;
                if (Physics.SphereCast(origin, focusRadius, dir, out _, d + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                {
                    best = d;
                    winner = it;
                }
            }
        }
        return winner;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var t = aimSource ? aimSource : transform;
        Gizmos.color = new Color(0,1,1,0.35f);
        Gizmos.DrawWireSphere(t.position + Vector3.up * 1.1f, range);
    }
#endif
}
