using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SavePoint : MonoBehaviour
{
    [Header("Config")]
    public string anchorIdToSet;        // si lo dejas vacío, conserva el actual
    public bool healOnSave = true;
    public bool teleportAfterSave;
    public string teleportAnchorId;

    [Header("Interacción")]
    public KeyCode interactKey = KeyCode.E;
    public string prompt = "Guardar partida (E)";

    CanvasGroup _promptCg;

    // Estado para diferir el guardado si el perfil no está listo
    private bool _pendingSave;
    private GameObject _pendingPlayer;

    void Reset(){ GetComponent<Collider>().isTrigger = true; }

    void OnEnable()
    {
        GameBootService.OnProfileReady += HandleProfileReady;
    }

    void OnDisable()
    {
        GameBootService.OnProfileReady -= HandleProfileReady;
    }

    private void HandleProfileReady()
    {
        if (_pendingSave && _pendingPlayer != null)
        {
            DoSave(_pendingPlayer);
            _pendingSave = false;
            _pendingPlayer = null;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ShowPrompt(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ShowPrompt(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Input.GetKeyDown(interactKey))
        {
            if (GameBootService.IsAvailable)
            {
                DoSave(other.gameObject);
            }
            else
            {
                // Diferir hasta que el GameBootProfile esté listo
                _pendingSave = true;
                _pendingPlayer = other.gameObject;
                Debug.Log("[SavePoint] Perfil no listo. Guardado diferido hasta OnProfileReady");
            }
        }
    }

    void DoSave(GameObject playerGo)
    {
        var bootProfile = GameBootService.Profile;
        if (bootProfile == null)
        {
            Debug.LogError("[SavePoint] GameBootProfile no disponible en GameBootService");
            return;
        }

        if (!string.IsNullOrEmpty(anchorIdToSet))
            SpawnManager.SetCurrentAnchor(anchorIdToSet);

        if (healOnSave && playerGo != null)
        {
            // Curar al jugador a través del PlayerHealthSystem
            var playerHealth = playerGo.GetComponent<PlayerHealthSystem>() ?? playerGo.GetComponentInParent<PlayerHealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.SetCurrentHealth(playerHealth.MaxHealth);
            }
            
            // Curar maná a través del ManaPool
            var manaPool = playerGo.GetComponent<ManaPool>() ?? playerGo.GetComponentInParent<ManaPool>();
            if (manaPool != null)
            {
                manaPool.Init(manaPool.Max, manaPool.Max);
            }
        }

        // Guardar usando GameBootProfile
        var saveSystem = FindFirstObjectByType<SaveSystem>();
        if (saveSystem != null)
        {
            bool success = bootProfile.SaveCurrentGameState(saveSystem);
            
            if (success)
            {
                Debug.Log("[SavePoint] Partida guardada correctamente");
                OnSaveCompleted?.Invoke();
            }
            else
            {
                Debug.LogError("[SavePoint] Error al guardar la partida");
            }
        }
        else
        {
            Debug.LogError("[SavePoint] No se encontró SaveSystem");
        }

        // Teletransporte opcional tras guardar
        if (teleportAfterSave && !string.IsNullOrEmpty(teleportAnchorId) && playerGo != null)
        {
            TeleportService.TeleportToAnchor(playerGo, teleportAnchorId);
        }
    }

    void ShowPrompt(bool show)
    {
        // opcional: si tienes un Canvas local con CanvasGroup para el prompt
        if (!_promptCg) _promptCg = GetComponentInChildren<CanvasGroup>(true);
        if (_promptCg){ _promptCg.alpha = show ? 1f : 0f; _promptCg.blocksRaycasts = show; }
        // si no, pon aquí tu llamada a la UI global (TextMeshPro).
        if (show) Debug.Log(prompt);
    }

    // Evento opcional para notificar cuando la partida se guarda correctamente
    public event System.Action OnSaveCompleted;
}