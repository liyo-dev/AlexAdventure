using UnityEngine;

public class WorldBootstrap : MonoBehaviour
{
    private SaveSystem _saveSystem;
    private bool _initialized;

    void OnEnable()
    {
        GameBootService.OnProfileReady += HandleProfileReady;
        if (GameBootService.IsAvailable)
        {
            HandleProfileReady();
        }
    }

    void OnDisable()
    {
        GameBootService.OnProfileReady -= HandleProfileReady;
    }

    private void HandleProfileReady()
    {
        if (_initialized) return;
        InitializeWorld();
        _initialized = true;
        GameBootService.OnProfileReady -= HandleProfileReady;
    }

    private void InitializeWorld()
    {
        var bootProfile = GameBootService.Profile;
        if (bootProfile == null)
        {
            Debug.LogError("[WorldBootstrap] ¡No se encontró GameBootProfile en GameBootService!");
            return;
        }

        _saveSystem = FindFirstObjectByType<SaveSystem>();

        // 1) Modo PRESET (test): ignora el save
        if (bootProfile.ShouldBootFromPreset())
        {
            var anchor = bootProfile.GetStartAnchorOrDefault();
            SpawnManager.SetCurrentAnchor(anchor);

            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo) SpawnManager.TeleportTo(anchor, true);

            Debug.Log("[WorldBootstrap] Iniciado en modo PRESET");
            return;
        }

        // 2) Flujo normal: intentar cargar partida; si no, usar anchor del preset activo
        string anchorId = bootProfile.GetStartAnchorOrDefault();

        if (_saveSystem != null && _saveSystem.Load(out var data))
        {
            if (!string.IsNullOrEmpty(data.lastSpawnAnchorId))
                anchorId = data.lastSpawnAnchorId;

            // Actualizar runtimePreset con los datos del save
            var slotTemplate = bootProfile.bootPreset ? bootProfile.bootPreset : bootProfile.defaultPlayerPreset;
            bootProfile.SetRuntimePresetFromSave(data, slotTemplate);

            Debug.Log("[WorldBootstrap] Save cargado correctamente");
        }
        else
        {
            Debug.Log("[WorldBootstrap] Sin save disponible, usando configuración por defecto");
        }

        // 3) Colocar jugador
        SpawnManager.SetCurrentAnchor(anchorId);
        var player = GameObject.FindWithTag("Player");
        if (player) 
        {
            SpawnManager.TeleportTo(anchorId, true);
            Debug.Log($"[WorldBootstrap] Jugador colocado en anchor: {anchorId}");
        }
        else
        {
            Debug.LogWarning("[WorldBootstrap] No se encontró el jugador con tag 'Player'");
        }
    }
}
