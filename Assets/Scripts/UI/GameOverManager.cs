using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona la pantalla de Game Over (mostrar/ocultar, pausar el juego y reiniciar escena).
/// Asignar en el inspector un GameObject que contenga la UI de Game Over (Canvas con panel).
/// Nota: versi�n simplificada � no gestiona animaciones ni desactiva componentes del jugador.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Tooltip("Referencia al objeto de UI que act�a como pantalla de Game Over. Puede estar desactivado por defecto.")]
    [SerializeField] private GameObject gameOverUI;

    [Tooltip("Si est� activado, al mostrar GameOver se pausar� el juego con Time.timeScale = 0.")]
    [SerializeField] private bool pauseOnGameOver = true;

    [Header("Escenas")]
    [Tooltip("Nombre de la escena de mundo que se carga al 'Cargar partida'. Igual que en MainMenuController.worldScene.")]
    [SerializeField] private string worldScene = "MainWorld";

    [Tooltip("Nombre de la escena del men� principal.")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("UI / Navegaci�n")]
    public Button loadLastSaveButton; // asignar desde inspector
    public Button backToMenuButton; // asignar desde inspector
    public CanvasGroup rootGroup; // opcional
    public RectTransform[] selectableItems; // opcional: orden de selecci�n

    [Header("Comportamiento")]
    [Tooltip("Retraso en segundos antes de mostrar la UI de Game Over para permitir que la animaci�n de muerte y la barra de vida terminen.")]
    [SerializeField] private float delayBeforeShow = 0.75f;

    // internos para mantener foco
    EventSystem _es;
    GameObject _defaultSelection;

    private bool _isGameOverShown = false;
    private Coroutine _showCoroutine = null;
    // Solo cuando AttachUIButtonListeners() haya sido llamado permitimos ejecutar las acciones p�blicas.
    bool _allowActions = false;

    public bool IsShown => _isGameOverShown;
    [Header("Navegaci�n")]
    [Tooltip("Tiempo entre repeticiones de navegaci�n cuando se mantiene el D-Pad o stick.")]
    [Range(0.05f, 0.5f)] public float navRepeatDelay = 0.2f;
    float _navCooldown;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsurePersistentIfPlacedInStartScene()
    {
        try
        {
#if UNITY_2022_3_OR_NEWER
            var existing = UnityEngine.Object.FindFirstObjectByType<GameOverManager>(FindObjectsInactive.Include);
#else
#pragma warning disable 618
            var existing = UnityEngine.Object.FindObjectOfType<GameOverManager>(true);
#pragma warning restore 618
#endif
            if (existing != null)
            {
                if (existing.transform.root != null)
                    UnityEngine.Object.DontDestroyOnLoad(existing.transform.root.gameObject);
                else
                    UnityEngine.Object.DontDestroyOnLoad(existing.gameObject);
            }

            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(es);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"EnsurePersistentIfPlacedInStartScene failed: {ex}");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Opcional: no destruir al cambiar de escena si quieres persistencia
        // DontDestroyOnLoad(gameObject);
        // Si est� en la escena inicial, preferimos persistir (se puede desactivar en inspector si no quieres)
        if (gameObject.scene.isLoaded && gameObject.scene.buildIndex == 0)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        // Preparar refs UI
        _es = EventSystem.current;
        if (rootGroup == null && gameOverUI != null)
            rootGroup = gameOverUI.GetComponent<CanvasGroup>() ?? gameOverUI.AddComponent<CanvasGroup>();

        // Si no se han proporcionado selectableItems, rellenar desde child Selectables
        if (selectableItems == null || selectableItems.Length == 0)
        {
#pragma warning disable 300
            var selectables = gameOverUI != null ? gameOverUI.GetComponentsInChildren<Selectable>(true) : GetComponentsInChildren<Selectable>(true);
            if (selectables != null && selectables.Length > 0)
            {
                selectableItems = new RectTransform[selectables.Length];
                for (int i = 0; i < selectables.Length; i++)
                {
                    var rt = selectables[i].transform as RectTransform;
                    selectableItems[i] = rt;
                }
            }
#pragma warning restore 300
        }

        // Default selection prefer LoadLastSaveButton
        GameObject fallback = null;
        if (selectableItems != null)
        {
            for (int i = 0; i < selectableItems.Length; i++)
            {
                var rt = selectableItems[i];
                if (rt != null) { fallback = rt.gameObject; break; }
            }
        }
        _defaultSelection = loadLastSaveButton ? loadLastSaveButton.gameObject : fallback;

        // No sobrescribimos ni re-sincronizamos los UnityEvents en Awake. Los listeners se a�adir�n
        // y quitar�n cuando el men� se muestre/oculte para evitar callbacks fuera de contexto.
        // Asegurar estado inicial no interactivo para evitar que botones respondan si el GameObject
        // se activa inadvertidamente.
        if (rootGroup != null)
        {
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }
        // Solo afectar botones que pertenecen al panel de GameOver para no interferir con HUD
        if (loadLastSaveButton != null)
        {
            if (gameOverUI != null && loadLastSaveButton.transform.IsChildOf(gameOverUI.transform))
                loadLastSaveButton.interactable = false;
            else
                Debug.LogWarning("[GameOverManager] loadLastSaveButton no es hijo de gameOverUI; no se modificar� en Awake.");
        }
        if (backToMenuButton != null)
        {
            if (gameOverUI != null && backToMenuButton.transform.IsChildOf(gameOverUI.transform))
                backToMenuButton.interactable = false;
            else
                Debug.LogWarning("[GameOverManager] backToMenuButton no es hijo de gameOverUI; no se modificar� en Awake.");
        }
    }

    private void OnEnable()
    {
        // No manejar la pausa/cursores aqu�: OnEnable se llama cuando el GO se activa, pero
        // el men� de Game Over puede estar oculto. Usar ShowGameOver/HideGameOver para eso.
    }

    private void OnDisable()
    {
        // No manejar la reanudaci�n aqu� por la misma raz�n.
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        // Solo procesar inputs cuando el Game Over est� realmente mostrado, la UI est� activa y las acciones est�n permitidas
        if (!_isGameOverShown || !_allowActions || (gameOverUI != null && !gameOverUI.activeInHierarchy)) return;
#else
        if (!_isGameOverShown || !_allowActions || (gameOverUI != null && !gameOverUI.activeInHierarchy)) return;
#endif

        KeepUIFocusForGamepad();
        HandleNavigationInput();

        // Cancel/back behavior: llevar al men� principal
#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            var gp = Gamepad.current;
            // Start / buttonSouth (A) -> continuar (ocultar GameOver)
            if (gp.startButton.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame)
            {
                HideGameOver();
                return;
            }
            // buttonEast (B) -> volver al men� principal (cancel)
            if (gp.buttonEast.wasPressedThisFrame)
            {
                OnBackToMainMenu();
                return;
            }
        }
        else if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBackToMainMenu();
                return;
            }
            if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                HideGameOver();
                return;
            }
        }
#else
        // Legacy input: Submit resumes, Cancel/back goes to main menu
        if (Input.GetButtonDown("Submit"))
        {
            HideGameOver();
            return;
        }
        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackToMainMenu();
            return;
        }
#endif
    }

// ---------------- UI Focus Keeper ----------------
#pragma warning disable 300
    void EnsureUISelection()
    {
        if (_es == null) _es = EventSystem.current;
        if (_es == null) return;

        if (_es.currentSelectedGameObject == null || !_es.currentSelectedGameObject.activeInHierarchy)
        {
            var toSelect = _defaultSelection;
            if (toSelect == null)
            {
                var firstSel = gameOverUI != null ? gameOverUI.GetComponentInChildren<Selectable>(true) : GetComponentInChildren<Selectable>(true);
                if (!ReferenceEquals(firstSel, null)) toSelect = firstSel.gameObject;
            }
            if (toSelect != null)
            {
                _defaultSelection = toSelect;
                _es.SetSelectedGameObject(toSelect);
            }
        }
    }
#pragma warning restore 300

    void KeepUIFocusForGamepad()
    {
        if (_es == null) _es = EventSystem.current;
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

    void HandleNavigationInput()
    {
        if (selectableItems == null || selectableItems.Length == 0) return;
        if (_es == null) _es = EventSystem.current;
        if (_es == null) return;

        if (_navCooldown > 0f)
        {
            _navCooldown -= Time.unscaledDeltaTime;
            if (_navCooldown > 0f) return;
            _navCooldown = 0f;
        }

        bool moveUp = false;
        bool moveDown = false;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            var gp = Gamepad.current;
            moveUp |= gp.dpad.up.wasPressedThisFrame;
            moveDown |= gp.dpad.down.wasPressedThisFrame;

            var stickY = gp.leftStick.ReadValue().y;
            moveUp |= stickY > 0.6f;
            moveDown |= stickY < -0.6f;
        }
        else if (Keyboard.current != null)
        {
            moveUp |= Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
            moveDown |= Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame;
        }
#else
        float axis = Input.GetAxisRaw("Vertical");
        moveUp |= Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || axis > 0.6f;
        moveDown |= Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || axis < -0.6f;
#endif

        if (moveUp)
        {
            MoveSelection(-1);
            _navCooldown = navRepeatDelay;
        }
        else if (moveDown)
        {
            MoveSelection(+1);
            _navCooldown = navRepeatDelay;
        }
    }

    void MoveSelection(int direction)
    {
        if (selectableItems == null || selectableItems.Length == 0) return;
        EnsureUISelection();
        if (_es == null) return;

        var ordered = GetOrderedSelectables();
        if (ordered.Count == 0) return;

        var current = _es.currentSelectedGameObject;
        int currentIndex = 0;
        if (current != null)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i] != null && ordered[i].gameObject == current)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, ordered.Count - 1);
        if (nextIndex == currentIndex) return;

        var next = ordered[nextIndex];
        if (next != null)
        {
            _defaultSelection = next.gameObject;
            _es.SetSelectedGameObject(next.gameObject);
            next.Select();
        }
    }

    readonly System.Collections.Generic.List<Selectable> _orderedSelectables = new System.Collections.Generic.List<Selectable>();

    System.Collections.Generic.List<Selectable> GetOrderedSelectables()
    {
        _orderedSelectables.Clear();
        if (selectableItems != null && selectableItems.Length > 0)
        {
            foreach (var rt in selectableItems)
            {
                if (!rt) continue;
                var sel = rt.GetComponent<Selectable>();
                if (sel != null)
                    _orderedSelectables.Add(sel);
            }
        }
        else if (gameOverUI != null)
        {
            var selectables = gameOverUI.GetComponentsInChildren<Selectable>(true);
            foreach (var sel in selectables)
                if (sel != null)
                    _orderedSelectables.Add(sel);
        }
        return _orderedSelectables;
    }

    // Attach/detach listeners: solo a�adimos/quitan nuestro callback, no tocamos listeners serializados.
    void AttachUIButtonListeners()
    {
        if (rootGroup != null)
        {
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;
        }

        // Solo afectar botones que pertenecen al panel de GameOver
        if (loadLastSaveButton != null)
        {
            if (gameOverUI != null && loadLastSaveButton.transform.IsChildOf(gameOverUI.transform))
            {
                loadLastSaveButton.onClick.RemoveListener(OnContinueButtonPressed);
                loadLastSaveButton.onClick.AddListener(OnContinueButtonPressed);
                loadLastSaveButton.interactable = true;
            }
            else
            {
                Debug.LogWarning("[GameOverManager] loadLastSaveButton no pertenece a gameOverUI � no se a�adir� listener");
            }
        }

        if (backToMenuButton != null)
        {
            if (gameOverUI != null && backToMenuButton.transform.IsChildOf(gameOverUI.transform))
            {
                backToMenuButton.onClick.RemoveListener(OnBackToMainMenu);
                backToMenuButton.onClick.AddListener(OnBackToMainMenu);
                backToMenuButton.interactable = true;
            }
            else
            {
                Debug.LogWarning("[GameOverManager] backToMenuButton no pertenece a gameOverUI � no se a�adir� listener");
            }
        }

        // Permitir la ejecuci�n de las acciones p�blicas ahora que los listeners est�n adjuntos
        _allowActions = true;
    }

    void DetachUIButtonListeners()
    {
        if (rootGroup != null)
        {
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        if (loadLastSaveButton != null && gameOverUI != null && loadLastSaveButton.transform.IsChildOf(gameOverUI.transform))
        {
            loadLastSaveButton.onClick.RemoveListener(OnContinueButtonPressed);
            loadLastSaveButton.interactable = false;
        }

        if (backToMenuButton != null && gameOverUI != null && backToMenuButton.transform.IsChildOf(gameOverUI.transform))
        {
            backToMenuButton.onClick.RemoveListener(OnBackToMainMenu);
            backToMenuButton.interactable = false;
        }

        // Desactivar la ejecuci�n de acciones p�blicas
        _allowActions = false;
    }

    /// <summary>
    /// Muestra la pantalla de Game Over. Pausa el juego si est� configurado.
    /// </summary>
    public void ShowGameOver()
    {
        // Evitar reentradas: si ya est� mostrado o en proceso de mostrarse, ignorar
        if (_isGameOverShown || _showCoroutine != null) return;

        // Iniciar la coroutine que espera en tiempo real para permitir animaciones/efectos
        _showCoroutine = StartCoroutine(ShowGameOverRoutine());
    }

    private System.Collections.IEnumerator ShowGameOverRoutine()
    {
        if (delayBeforeShow > 0f)
        {
            // Esperar en tiempo real para que animaciones y barras puedan terminar aunque Time.timeScale siga en 1
            yield return new WaitForSecondsRealtime(delayBeforeShow);
        }

        _showCoroutine = null;
        _isGameOverShown = true;

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        if (pauseOnGameOver)
            Time.timeScale = 0f;

        // Asegurar selecci�n inicial del UI
        EnsureUISelection();

        // Adjuntar solo nuestros listeners y activar la interacci�n
        AttachUIButtonListeners();

        Debug.Log("[GameOverManager] Game Over mostrado");
    }

    /// <summary>
    /// Oculta la pantalla de Game Over y reanuda el juego.
    /// </summary>
    public void HideGameOver()
    {
        // Si estamos en espera para mostrar, cancelarla
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        if (!_isGameOverShown) return;
        _isGameOverShown = false;

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (pauseOnGameOver)
            Time.timeScale = 1f;

        // Quitar nuestros listeners y desactivar interacci�n
        DetachUIButtonListeners();

        Debug.Log("[GameOverManager] Game Over ocultado");
    }

    /// <summary>
    /// Reinicia la escena actual. Asegura que Time.timeScale se restablece.
    /// </summary>
    public void RestartLevel()
    {
        if (pauseOnGameOver)
            Time.timeScale = 1f;

        // Antes de recargar la escena, asegurarnos de que el runtimePreset contiene los valores actuales de HP/MP
        try
        {
            var profile = GameBootService.Profile;
            if (profile != null)
            {
                var preset = profile.GetActivePresetResolved();
                if (preset != null)
                {
                    // Intentar obtener PlayerHealthSystem del jugador
                    PlayerHealthSystem phs = UnityEngine.Object.FindFirstObjectByType<PlayerHealthSystem>();
                    if (phs == null)
                    {
                        var playerGo = GameObject.FindWithTag("Player");
                        if (playerGo != null)
                            phs = playerGo.GetComponent<PlayerHealthSystem>();
                    }

                    if (phs != null)
                    {
                        preset.currentHP = phs.CurrentHealth;
                        preset.maxHP = phs.MaxHealth;
                        Debug.Log($"[GameOverManager] Runtime preset HP actualizado: {preset.currentHP}/{preset.maxHP}");
                    }
                    else
                    {
                        Debug.LogWarning("[GameOverManager] No se encontr� PlayerHealthSystem para sincronizar HP antes de reiniciar");
                    }

                    // Manejar Man�: si el preset no tiene la ability de magia, respetar 0/0
                    if (preset.abilities != null && !preset.abilities.magic)
                    {
                        preset.currentMP = 0f;
                        preset.maxMP = 0f;
                        Debug.Log("[GameOverManager] Preset indica que no tiene magia -> MP seteado a 0/0 antes de reiniciar");
                    }
                    else
                    {
                        // Obtener ManaPool del jugador
                        ManaPool mana = UnityEngine.Object.FindFirstObjectByType<ManaPool>();
                        if (mana == null)
                        {
                            var playerGo = GameObject.FindWithTag("Player");
                            if (playerGo != null)
                                mana = playerGo.GetComponentInChildren<ManaPool>() ?? playerGo.GetComponent<ManaPool>();
                        }

                        if (mana != null)
                        {
                            preset.maxMP = mana.Max;
                            preset.currentMP = mana.Current;
                            Debug.Log($"[GameOverManager] Runtime preset MP actualizado: {preset.currentMP}/{preset.maxMP}");
                        }
                        else
                        {
                            Debug.LogWarning("[GameOverManager] No se encontr� ManaPool para sincronizar MP antes de reiniciar (dejando valores del preset)");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameOverManager] Profile disponible pero GetActivePresetResolved() devolvi� null");
                }
            }
            else
            {
                Debug.LogWarning("[GameOverManager] GameBootService.Profile no est� disponible al reiniciar nivel");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[GameOverManager] Error sincronizando preset antes de reiniciar: {e}");
        }

        var active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.name);
    }

    /// <summary>
    /// Llamado desde el bot�n "Continuar" en la UI. Wrapper p�blico que garantiza la l�gica correcta
    /// y deja un log claro para depuraci�n del mapeo del bot�n.
    /// </summary>
    public void OnContinueButtonPressed()
    {
        if (!_allowActions || !_isGameOverShown)
        {
            Debug.LogWarning("[GameOverManager] OnContinueButtonPressed ignorado porque el men� no permite acciones ahora.");
            Debug.Log(new System.Diagnostics.StackTrace().ToString());
            return;
        }

        Debug.Log("[GameOverManager] OnContinueButtonPressed invoked -> ocultando GameOver (resumir juego)");
        HideGameOver();
    }

    /// <summary>
    /// Llamado desde el bot�n "Cargar partida" en la UI. Reutiliza la l�gica de MainMenuController.OnContinue().
    /// </summary>
    public void OnLoadLastSave()
    {
        if (!_allowActions || !_isGameOverShown)
        {
            Debug.LogWarning("[GameOverManager] OnLoadLastSave ignorado porque el men� no permite acciones ahora.");
            Debug.Log(new System.Diagnostics.StackTrace().ToString());
            return;
        }

        Debug.Log($"[GameOverManager] OnLoadLastSave invoked. worldScene='{worldScene}', mainMenuScene='{mainMenuScene}'");
        // Restaurar timescale antes de hacer la carga/scene load
        if (pauseOnGameOver)
            Time.timeScale = 1f;

        var saveSystem = UnityEngine.Object.FindFirstObjectByType<SaveSystem>();
        bool hasSave = saveSystem != null && saveSystem.HasSave();
        Debug.Log($"[GameOverManager] SaveSystem found={ (saveSystem!=null) }, HasSave={hasSave}");

        if (saveSystem != null && saveSystem.HasSave())
        {
            if (GameBootService.IsAvailable)
                GameBootService.Profile?.LoadProfile(saveSystem);

            Debug.Log($"[GameOverManager] Loading scene '{worldScene}' from save");
            // Llamada din�mica a SceneTransitionLoader.Load para evitar dependencia dura
            var loaderType = System.Type.GetType("SceneTransitionLoader, Assembly-CSharp");
            if (loaderType != null)
            {
                var method = loaderType.GetMethod("Load", new System.Type[] { typeof(string) });
                method?.Invoke(null, new object[] { worldScene });
            }
            else
            {
                SceneManager.LoadScene(worldScene);
            }
        }
        else
        {
            Debug.Log("[GameOverManager] No hay partida guardada; empezar nueva partida");
            // Si no hay save, empezar nueva partida (igual que MainMenuController)
            GameBootService.NewGameReset();

            Debug.Log($"[GameOverManager] Loading scene '{worldScene}' for new game");
            var loaderType = System.Type.GetType("SceneTransitionLoader, Assembly-CSharp");
            if (loaderType != null)
            {
                var method = loaderType.GetMethod("Load", new System.Type[] { typeof(string) });
                method?.Invoke(null, new object[] { worldScene });
            }
            else
            {
                SceneManager.LoadScene(worldScene);
            }
        }
    }

    /// <summary>
    /// Llamado desde el bot�n "Volver al men� principal".
    /// </summary>
    public void OnBackToMainMenu()
    {
        if (!_allowActions || !_isGameOverShown)
        {
            Debug.LogWarning("[GameOverManager] OnBackToMainMenu ignorado porque el men� no permite acciones ahora.");
            Debug.Log(new System.Diagnostics.StackTrace().ToString());
            return;
        }

        Debug.Log($"[GameOverManager] OnBackToMainMenu invoked. mainMenuScene='{mainMenuScene}'");
        if (pauseOnGameOver)
            Time.timeScale = 1f;


        var loaderType = System.Type.GetType("SceneTransitionLoader, Assembly-CSharp");
        if (loaderType != null)
        {
            var method = loaderType.GetMethod("Load", new System.Type[] { typeof(string) });
            method?.Invoke(null, new object[] { mainMenuScene });
        }
        else
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }

    /// <summary>
    /// Modo helper para notificar Game Over desde otros scripts de forma segura.
    /// </summary>
    public static void NotifyGameOver()
    {
        if (Instance != null)
            Instance.ShowGameOver();
        else
            Debug.LogWarning("[GameOverManager] No hay instancia disponible para mostrar Game Over.");
    }
}


