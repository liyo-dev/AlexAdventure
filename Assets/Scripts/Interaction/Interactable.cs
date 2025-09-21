using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Interactable : MonoBehaviour
{
    public enum Mode { OpenDialogue, HandOffToTarget }

    [Header("Modo")]
    [SerializeField] private Mode mode = Mode.OpenDialogue;

    [Header("Hint (icono botón)")]
    [SerializeField] private GameObject hint;           // Canvas/Sprite en World Space
    [SerializeField] private bool hideHintAtStart = true;

    [Header("Uso")]
    [SerializeField] private bool singleUse = false;
    [SerializeField] private bool initiallyEnabled = true;

    [Header("Abrir diálogo")]
    [SerializeField] private DialogueAsset dialogue;     // usado en OpenDialogue

    [Header("Ceder control")]
    [Tooltip("Objeto que implementa IInteractionSession (por ej. UnityEventSession, tu cofre, tu panel, etc.).")]
    [SerializeField] private MonoBehaviour sessionTarget; // debe implementar IInteractionSession

    [Header("Eventos opcionales")]
    public UnityEvent<GameObject> OnInteract;     // justo al pulsar (antes de abrir)
    public UnityEvent OnStarted;                  // cuando arranca (diálogo o sesión)
    public UnityEvent OnFinished;                 // cuando acaba (cierra diálogo o sesión)
    public UnityEvent OnConsumed;                 // primera vez si singleUse

    // ---- estado ----
    bool used, enabledForUse;

    void Awake()
    {
        enabledForUse = initiallyEnabled;
        if (hint && hideHintAtStart) hint.SetActive(false);
    }

    // Llamado por el detector del player
    public void SetHintVisible(bool visible)
    {
        if (hint) hint.SetActive(visible && !used && enabledForUse);
    }

    public bool CanInteract(GameObject _)
    {
        // Evita spam si hay diálogo abierto
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return false;
        return enabledForUse && (!singleUse || !used);
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor)) return;

        OnInteract?.Invoke(interactor);

        switch (mode)
        {
            case Mode.OpenDialogue:
                StartDialogue(interactor);
                break;

            case Mode.HandOffToTarget:
                StartHandOff(interactor);
                break;
        }
    }

    void StartDialogue(GameObject _)
    {
        if (dialogue && DialogueManager.Instance)
        {
            OnStarted?.Invoke();
            DialogueManager.Instance.StartDialogue(dialogue, () =>
            {
                OnFinished?.Invoke();
                AfterUse();
            });
        }
        else
        {
            Debug.LogWarning($"[Interactable] No hay DialogueAsset o DialogueManager para {name}.");
            AfterUse();
        }
    }

    void StartHandOff(GameObject interactor)
    {
        if (sessionTarget is IInteractionSession session)
        {
            OnStarted?.Invoke();
            session.BeginSession(interactor, () =>
            {
                OnFinished?.Invoke();
                AfterUse();
            });
        }
        else
        {
            Debug.LogWarning($"[Interactable] sessionTarget de {name} no implementa IInteractionSession.");
            AfterUse();
        }
    }

    void AfterUse()
    {
        if (singleUse && !used)
        {
            used = true;
            OnConsumed?.Invoke();
        }
        SetHintVisible(false);
    }

    // Helpers
    public void EnableInteraction(bool enable)
    {
        enabledForUse = enable;
        if (!enable) SetHintVisible(false);
    }

    public void SetDialogue(DialogueAsset asset) => dialogue = asset;
    public void SetMode(Mode newMode) => mode = newMode;
    public void SetSessionTarget(MonoBehaviour target) => sessionTarget = target;
}
