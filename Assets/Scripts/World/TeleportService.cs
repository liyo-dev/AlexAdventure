// Scripts/World/TeleportService.cs
using UnityEngine;
using UnityEngine.AI;
using EasyTransition;

[DisallowMultipleComponent]
public class TeleportService : MonoBehaviour
{
    // ===== Singleton mínimo =====
    private static TeleportService _inst;
    public static TeleportService Inst
    {
        get
        {
            if (_inst != null) return _inst;
#if UNITY_2022_3_OR_NEWER
            _inst = Object.FindFirstObjectByType<TeleportService>(FindObjectsInactive.Include);
#else
#pragma warning disable 618
            _inst = FindObjectOfType<TeleportService>(true);
#pragma warning restore 618
#endif
            return _inst;
        }
    }

    [Header("Transición (EasyTransition)")]
    [SerializeField] private TransitionSettings teleportTransition; // arrastra p.ej. Fade.asset
    [SerializeField] private float transitionDelay; // 0 por defecto implícito
    [SerializeField] private bool useTransitionByDefault = true;

    private void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);
    }

    // ================== API ESTÁTICA mínima ==================

    /// <summary>Teleporta a un anchor por id.</summary>
    public static void TeleportToAnchor(GameObject player, string anchorId, bool? useTransition = null)
    {
        if (!Inst) return;
        var sa = SpawnManager.GetAnchor(anchorId);
        if (!sa)
        {
            Debug.LogWarning($"[TeleportService] Anchor '{anchorId}' no encontrado.");
            return;
        }
        Inst.DoTeleportToAnchor(player, sa.transform, useTransition);
    }

    // ================== API de instancia ==================

    public void DoTeleportToAnchor(GameObject player, Transform anchor, bool? useTransition = null)
    {
        if (!player || !anchor)
        {
            Debug.LogWarning("[TeleportService] Parámetros nulos en TeleportToAnchor.");
            return;
        }

        // Sincronizar anchor actual (SpawnManager y runtimePreset) si conocemos su id
        var sa = anchor.GetComponentInParent<SpawnAnchor>();
        if (sa && !string.IsNullOrEmpty(sa.anchorId))
        {
            SpawnManager.SetCurrentAnchor(sa.anchorId);
        }

        var pos = anchor.position;
        var rot = anchor.rotation;
        bool useTrans = useTransition ?? useTransitionByDefault;

        if (useTrans) TeleportWithTransition(player, pos, rot, anchor);
        else          MoveNow(player, pos, rot, anchor);
    }

    // ================== Núcleo transición / movimiento ==================
    
    private void TeleportWithTransition(GameObject player, Vector3 worldPos, Quaternion worldRot, Transform anchorForEnv)
    {
        var tm = FindTM(); // ← seguro, no usa Instance()
        if (tm == null || !teleportTransition)
        {
            // Si el manager aún no está o no tienes settings, teleporta sin fade (no rompe)
            MoveNow(player, worldPos, worldRot, anchorForEnv);
            return;
        }

        void OnCut()
        {
            MovePlayerSafely(player, worldPos, worldRot);
            ApplyEnvironmentForAnchor(anchorForEnv);
            tm.onTransitionCutPointReached -= OnCut;
        }

        void OnEnd()
        {
            tm.onTransitionEnd -= OnEnd;
        }

        tm.onTransitionCutPointReached += OnCut;
        tm.onTransitionEnd            += OnEnd;

        // OJO: usamos la versión SIN cambio de escena del plugin (la estable)
        tm.Transition(teleportTransition, transitionDelay);
    }

    private void MoveNow(GameObject player, Vector3 pos, Quaternion rot, Transform anchorForEnv)
    {
        MovePlayerSafely(player, pos, rot);
        ApplyEnvironmentForAnchor(anchorForEnv);
    }

    private void MovePlayerSafely(GameObject player, Vector3 pos, Quaternion rot)
    {
        if (!player) return;

        var cc    = player.GetComponent<CharacterController>() ?? player.GetComponentInChildren<CharacterController>(true);
        var agent = player.GetComponent<NavMeshAgent>()        ?? player.GetComponentInChildren<NavMeshAgent>(true);
        var rb    = player.GetComponent<Rigidbody>()           ?? player.GetComponentInChildren<Rigidbody>(true);

        bool ccWas = cc && cc.enabled;
        bool agWas = agent && agent.enabled;

        if (cc)    cc.enabled = false;
        if (agent) agent.enabled = false;

        player.transform.SetPositionAndRotation(pos, rot);

        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (agent) agent.enabled = agWas;
        if (cc)    cc.enabled    = ccWas;
    }

    private void ApplyEnvironmentForAnchor(Transform anchor)
    {
        var ec = EnvironmentController.Instance;
        if (!ec) return;

        AnchorEnvironment env = null;
        if (anchor) env = anchor.GetComponentInParent<AnchorEnvironment>();

        if (env && env.isInterior) ec.ApplyInterior(env);
        else                       ec.ApplyExterior();
    }

    // ================== Utilidades ==================
    
    static EasyTransition.TransitionManager FindTM()
    {
#if UNITY_2022_3_OR_NEWER
        return Object.FindFirstObjectByType<EasyTransition.TransitionManager>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<EasyTransition.TransitionManager>(true);
#endif
    }
}
