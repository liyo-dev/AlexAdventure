using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Refs")]
    private SaveSystem _saveSystem;
    public Button continueButton;

    [Header("World Scene")]
    public string worldScene = "MainWorld";

    [Header("Fade override (opcional)")]
    public EasyTransition.TransitionSettings fadeOverride;
    [Min(0)] public float fadeDelay = 0f;

    [Header("UI / Animación (DOTween)")]
    public CanvasGroup rootGroup;                         // si está vacío, se añade
    public List<RectTransform> animatedItems = new();     // si está vacío, se auto-rellena
    [Min(0f)] public float introDelay = 0.05f;
    [Min(0f)] public float introStagger = 0.04f;
    [Min(0f)] public float introDuration = 0.35f;
    public float introYOffset = 40f;

    // interno
    EventSystem _es;
    GameObject _defaultSelection;
    Sequence _introSeq;

    void Awake()
    {
        Application.runInBackground = true; // ayuda en editor/foco
        _es = EventSystem.current;

        if (!rootGroup)
        {
            rootGroup = GetComponent<CanvasGroup>();
            if (!rootGroup) rootGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (animatedItems == null || animatedItems.Count == 0)
        {
            foreach (var s in GetComponentsInChildren<Selectable>(true))
                if (s) animatedItems.Add(s.transform as RectTransform);
        }

        _defaultSelection = continueButton
            ? continueButton.gameObject
            : (animatedItems.Count > 0 ? animatedItems[0]?.gameObject : null);
    }

    void OnEnable()
    {
        // Al volver al menú, asegúrate de que hay selección
        EnsureUISelection();
    }

    void Start()
    {
        _saveSystem = FindFirstObjectByType<SaveSystem>();
        if (continueButton != null)
            continueButton.interactable = (_saveSystem != null) && _saveSystem.HasSave();

        EnsureUISelection();
        PlayIntro();
    }

    void Update()
    {
        KeepUIFocusForGamepad();
    }

    // ---------------- UI Focus Keeper ----------------
    void EnsureUISelection()
    {
        if (_es == null) return;

        if (_es.currentSelectedGameObject == null || !_es.currentSelectedGameObject.activeInHierarchy)
        {
            if (_defaultSelection == null)
            {
                var firstSel = GetComponentInChildren<Selectable>(true);
                if (firstSel) _defaultSelection = firstSel.gameObject;
            }
            if (_defaultSelection != null)
                _es.SetSelectedGameObject(_defaultSelection);
        }
    }

    void KeepUIFocusForGamepad()
    {
        if (_es == null) return;

        bool wantsPadFocus = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            var gp = Gamepad.current;
            wantsPadFocus =
                gp.dpad.up.wasPressedThisFrame || gp.dpad.down.wasPressedThisFrame ||
                Mathf.Abs(gp.leftStick.ReadValue().y) > 0.25f ||
                gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame;
        }
        else
        {
            wantsPadFocus = Keyboard.current != null && (
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.sKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame
            );
        }
#else
        wantsPadFocus = Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f || Input.GetButtonDown("Submit");
#endif

        if (wantsPadFocus && (_es.currentSelectedGameObject == null || !_es.currentSelectedGameObject.activeInHierarchy))
            EnsureUISelection();
    }

    // ---------------- DOTween Intro ----------------
    void PlayIntro()
    {
        // Estado inicial
        rootGroup.alpha = 0f;
        _introSeq?.Kill();
        _introSeq = DOTween.Sequence().SetUpdate(true);

        // Fade-in del root con DOTween.To (evita ambigüedad de DOFade con CanvasGroup)
        _introSeq.AppendInterval(introDelay);
        _introSeq.Append(
            DOTween.To(() => rootGroup.alpha, a => rootGroup.alpha = a, 1f, 0.2f)
        );

        // Slide + fade de cada item con stagger
        float delayAcc = 0f;
        foreach (var rt in animatedItems)
        {
            if (!rt) continue;

            Vector2 finalPos = rt.anchoredPosition;
            rt.anchoredPosition = finalPos + new Vector2(0f, -introYOffset);

            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            _introSeq.Insert(introDelay + delayAcc, rt.DOAnchorPos(finalPos, introDuration).SetEase(Ease.OutCubic));
            _introSeq.Insert(introDelay + delayAcc, cg.DOFade(1f, introDuration * 0.9f));

            delayAcc += introStagger;
        }
    }

    // ---------------- Botones ----------------
    public void OnNewGame()
    {
        _saveSystem?.Delete();
        Load("Prologo");
    }

    public void OnContinue()
    {
        if (_saveSystem != null && _saveSystem.HasSave())
            Load(worldScene);
        else
            OnNewGame();
    }

    void Load(string sceneName)
    {
        if (fadeOverride != null)
            SceneTransitionLoader.Load(sceneName, fadeOverride, fadeDelay);
        else
            SceneTransitionLoader.Load(sceneName);
    }
}
