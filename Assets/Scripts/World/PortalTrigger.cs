using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalTrigger : MonoBehaviour
{
    public string targetAnchorId;
    public string requiredFlag;
    public string setFlagOnEnter;

    private bool _pendingUse;

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
        if (_pendingUse)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                ProcessPortal(player);
            }
            _pendingUse = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameBootService.IsAvailable)
        {
            ProcessPortal(other.gameObject);
        }
        else
        {
            // Diferir hasta que el GameBootProfile esté listo
            _pendingUse = true;
        }
    }

    private void ProcessPortal(GameObject player)
    {
        if (string.IsNullOrEmpty(targetAnchorId))
        {
            Debug.LogWarning("[PortalTrigger] targetAnchorId vacío");
            return;
        }

        var bootProfile = GameBootService.Profile;
        if (bootProfile == null)
        {
            Debug.LogError("[PortalTrigger] GameBootProfile no disponible en GameBootService");
            return;
        }

        var preset = bootProfile.GetActivePresetResolved();
        if (preset == null)
        {
            Debug.LogError("[PortalTrigger] No hay preset activo");
            return;
        }

        // Verificar flag requerida
        if (!string.IsNullOrEmpty(requiredFlag))
        {
            if (preset.flags == null || !preset.flags.Contains(requiredFlag))
            {
                Debug.Log($"[PortalTrigger] Flag requerida '{requiredFlag}' no encontrada. Portal bloqueado.");
                return;
            }
        }

        // Establecer flag al entrar
        if (!string.IsNullOrEmpty(setFlagOnEnter))
        {
            if (preset.flags == null)
                preset.flags = new System.Collections.Generic.List<string>();
            
            if (!preset.flags.Contains(setFlagOnEnter))
            {
                preset.flags.Add(setFlagOnEnter);
                Debug.Log($"[PortalTrigger] Flag '{setFlagOnEnter}' establecida");
            }
        }

        SpawnManager.TeleportTo(targetAnchorId, true);
        // NO guardar aquí
    }
}