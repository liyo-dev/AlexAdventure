using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HoldToSkipUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image buttonIcon;      // tu sprite del botón (opcional)
    [SerializeField] private Image progressCircle;  // Image con Type=Filled (Radial)
    [SerializeField] private CanvasGroup group;     // opcional

    [Header("Comportamiento")]
    [SerializeField, Min(0.2f)] private float holdSeconds = 1.25f;
    [SerializeField] private bool showOnlyWhileHolding = true;
    [SerializeField] private float fadeIn = 0.12f;
    [SerializeField] private float fadeOut = 0.12f;
    [SerializeField] private bool disableSelfOnSkip = true;

    [Header("Input")]
    [Tooltip("Acción a mantener. ASÍG-NALA: UI/Submit o la que quieras. (Si queda vacío, usa <Gamepad>/buttonSouth)")]
    [SerializeField] private InputActionReference holdActionRef;

    [Header("Eventos")]
    public UnityEvent OnSkipCompleted;

    // ---- estado ----
    private InputAction holdAction; // resuelta en runtime
    private bool holding;
    private float heldTime;
    private float targetAlpha;
    private bool completed;
    private InputAction fallback;   // por si no asignas nada

    void Awake()
    {
        if (!group) group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (!progressCircle)
        {
            // intenta encontrar una Image "circular"
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img == buttonIcon) continue;
                if (img.type == Image.Type.Filled) { progressCircle = img; break; }
            }
        }

        if (progressCircle)
        {
            progressCircle.type = Image.Type.Filled;
            if (progressCircle.fillMethod != Image.FillMethod.Radial360)
                progressCircle.fillMethod = Image.FillMethod.Radial360;
            progressCircle.fillOrigin = 2;
            progressCircle.fillClockwise = true;
            progressCircle.fillAmount = 0f;
        }

        targetAlpha = showOnlyWhileHolding ? 0f : 1f;
        ApplyAlphaInstant(targetAlpha);
    }

    void OnEnable()
    {
        // Resolver acción
        if (holdActionRef != null && holdActionRef.action != null)
        {
            holdAction = holdActionRef.action;
        }
        else
        {
            // Fallback: botón A de gamepad
            fallback = new InputAction("HoldToSkipFallback", InputActionType.Button, "<Gamepad>/buttonSouth");
            fallback.Enable();
            holdAction = fallback;
        }

        if (holdAction != null)
        {
            if (!holdAction.enabled) holdAction.Enable();
            holdAction.started  += OnHoldStarted;
            holdAction.canceled += OnHoldCanceled;
        }

        ResetHold();
    }

    void OnDisable()
    {
        if (holdAction != null)
        {
            holdAction.started  -= OnHoldStarted;
            holdAction.canceled -= OnHoldCanceled;
        }
        if (fallback != null)
        {
            fallback.Disable();
            fallback.Dispose();
            fallback = null;
        }
        ResetHold();
    }

    void Update()
    {
        // Fade UI
        if (group)
        {
            float speed = (targetAlpha > group.alpha) ? (1f / Mathf.Max(0.0001f, fadeIn))
                                                      : (1f / Mathf.Max(0.0001f, fadeOut));
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, Time.unscaledDeltaTime * speed);
            group.blocksRaycasts = group.alpha > 0.001f;
            group.interactable   = group.blocksRaycasts;
        }

        // Progreso
        if (holding && !completed)
        {
            heldTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(heldTime / holdSeconds);
            if (progressCircle) progressCircle.fillAmount = t;

            if (t >= 1f)
            {
                completed = true;
                OnSkipCompleted?.Invoke();
                if (disableSelfOnSkip) gameObject.SetActive(false);
            }
        }
    }

    private void OnHoldStarted(InputAction.CallbackContext _)
    {
        holding = true;
        heldTime = 0f;
        completed = false;
        if (progressCircle) progressCircle.fillAmount = 0f;
        if (showOnlyWhileHolding) targetAlpha = 1f;
    }

    private void OnHoldCanceled(InputAction.CallbackContext _)
    {
        holding = false;
        if (!completed)
        {
            heldTime = 0f;
            if (progressCircle) progressCircle.fillAmount = 0f;
        }
        if (showOnlyWhileHolding) targetAlpha = 0f;
    }

    private void ResetHold()
    {
        holding = false;
        completed = false;
        heldTime = 0f;
        if (progressCircle) progressCircle.fillAmount = 0f;
        targetAlpha = showOnlyWhileHolding ? 0f : 1f;
        ApplyAlphaInstant(targetAlpha);
    }

    private void ApplyAlphaInstant(float a)
    {
        if (!group) return;
        group.alpha = a;
        group.blocksRaycasts = a > 0.001f;
        group.interactable   = group.blocksRaycasts;
    }

    // API
    public void SetHoldSeconds(float seconds) => holdSeconds = Mathf.Max(0.2f, seconds);
    public void SetIcon(Sprite s) { if (buttonIcon) buttonIcon.sprite = s; }
}
