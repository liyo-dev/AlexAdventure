using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image portraitImage;

    [Header("Input (solo mando)")]
    [Tooltip("Acción para AVANZAR. Usa UI/Submit (Gamepad South = A).")]
    [SerializeField] private InputActionReference advanceAction;

    [Header("Bloqueo de Inputs")]
    [Tooltip("Referencias a InputActionReference que se deshabilitan mientras el diálogo esté abierto (p.ej. movimiento, ataque, etc.).")]
    [SerializeField] private InputActionReference[] inputActionsToDisable;

    [Header("Opcional")]
    [SerializeField] private bool pauseGameWhileOpen = false;
    [SerializeField] private bool resolveWithLocalizationManager = false; // usa tu LocalizationManager si está en Start

    // Estado
    private DialogueAsset current;
    private int index = -1;
    private Action onEnd;
    public bool IsOpen => current != null;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    void OnEnable()
    {
        if (advanceAction?.action != null)
        {
            if (!advanceAction.action.enabled) advanceAction.action.Enable();
            advanceAction.action.performed += OnAdvance;
        }
    }

    void OnDisable()
    {
        if (advanceAction?.action != null)
            advanceAction.action.performed -= OnAdvance;
    }

    public void StartDialogue(DialogueAsset asset, Action onFinished = null)
    {
        if (asset == null || asset.lines == null || asset.lines.Length == 0) return;

        current = asset;
        onEnd = onFinished;
        index = -1;

        // Mostrar UI
        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }
        if (pauseGameWhileOpen) Time.timeScale = 0f;

        // Bloquear gameplay
        SetGameplayEnabled(false);

        Next(); // pinta primera línea
    }

    public void Advance()
    {
        if (IsOpen) Next();
    }

    public void Close()
    {
        if (!IsOpen) return;

        current = null;
        onEnd?.Invoke();
        onEnd = null;

        // Ocultar UI
        if (pauseGameWhileOpen) Time.timeScale = 1f;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        // Restaurar gameplay
        SetGameplayEnabled(true);
    }

    private void OnAdvance(InputAction.CallbackContext _)
    {
        if (IsOpen) Advance();
    }

    private void Next()
    {
        index++;
        if (current == null || current.lines == null || index >= current.lines.Length)
        {
            Close();
            return;
        }

        var line = current.lines[index];

        if (nameText) nameText.text = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;

        string textToShow = line.text ?? "";
        if (resolveWithLocalizationManager && LocalizationManager.Instance != null)
            textToShow = LocalizationManager.Instance.Get(textToShow, textToShow);

        if (bodyText) bodyText.text = textToShow;

        if (portraitImage)
        {
            if (line.portrait != null)
            {
                portraitImage.sprite = line.portrait;
                portraitImage.enabled = true;
            }
            // Solo ocultar si explícitamente se quiere ocultar
            // Si line.portrait es null, mantener el portrait anterior visible
        }
    }

    private void SetGameplayEnabled(bool enable)
    {
        if (inputActionsToDisable != null)
        {
            foreach (var actionRef in inputActionsToDisable)
            {
                if (actionRef?.action != null)
                {
                    if (enable)
                        actionRef.action.Enable();
                    else
                        actionRef.action.Disable();
                }
            }
        }
    }
}
