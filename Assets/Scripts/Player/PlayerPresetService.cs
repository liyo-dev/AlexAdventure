using UnityEngine;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public class PlayerPresetService : MonoBehaviour
{
    [Header("Librería de hechizos (ID → SO)")]
    [SerializeField] private SpellLibrarySO spellLibrary;

    [Header("Opciones")]
    [SerializeField] private bool autoFillEmptySlotsFromUnlocked; // default false por defecto
    [SerializeField] private GameObject instigatorOverride;

    MagicProjectileSpawner _spawner;
    
    // Evitar inicialización doble si el evento llega más de una vez o ya está listo al habilitar
    bool _initialized;

    void Awake()
    {
        _spawner = GetComponent<MagicProjectileSpawner>() ?? gameObject.AddComponent<MagicProjectileSpawner>();
    }

    // Suscribirnos al evento y cubrir el caso de que ya esté disponible
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
        InitializePresetService();
        _initialized = true;
        // Ya no necesitamos el evento tras inicializar
        GameBootService.OnProfileReady -= HandleProfileReady;
    }

    private void InitializePresetService()
    {
        var profile = GameBootService.Profile;
        
        if (!profile || !spellLibrary) 
        { 
            enabled = false; 
            Debug.LogError("[PlayerPresetService] Falta GameBootProfile o SpellLibrary."); 
            return; 
        }

        var preset = profile.GetActivePresetResolved();
        if (!preset) 
        { 
            Debug.LogWarning("[PlayerPresetService] Sin preset activo."); 
            return; 
        }

        // USAR EL ANCHOR DEL PRESET SO
        if (!string.IsNullOrEmpty(preset.spawnAnchorId))
        {
            SpawnManager.SetCurrentAnchor(preset.spawnAnchorId);
        }

        // Configurar hechizos del preset
        ConfigureSpells(preset);
    }

    private void ConfigureSpells(PlayerPresetSO preset)
    {
        // Resolver IDs respetando None
        var leftId = preset.leftSpellId;
        var rightId = preset.rightSpellId;
        var specialId = preset.specialSpellId;

        var left = leftId == SpellId.None ? null : spellLibrary.Get(leftId);
        var right = rightId == SpellId.None ? null : spellLibrary.Get(rightId);
        var special = specialId == SpellId.None ? null : spellLibrary.Get(specialId);

        // Validar tipos de slot
        if (left && left.slotType == SpellSlotType.SpecialOnly) left = null;
        if (right && right.slotType == SpellSlotType.SpecialOnly) right = null;
        if (special && special.slotType != SpellSlotType.SpecialOnly) special = null;

        // Evitar duplicados en slots izq/der
        if (left && right && leftId == rightId) right = null;

        // Auto-completar slots vacíos (opcional)
        if (autoFillEmptySlotsFromUnlocked && preset.unlockedSpells != null)
        {
            MagicSpellSO FindFirstAvailable(bool requireSpecial, SpellId avoid)
            {
                foreach (var id in preset.unlockedSpells)
                {
                    if (id == SpellId.None || id == avoid) continue;
                    var s = spellLibrary.Get(id);
                    if (!s) continue;
                    if (requireSpecial && s.slotType != SpellSlotType.SpecialOnly) continue;
                    if (!requireSpecial && s.slotType == SpellSlotType.SpecialOnly) continue;
                    return s;
                }
                return null;
            }

            if (!left) left = FindFirstAvailable(false, rightId);
            if (!right) right = FindFirstAvailable(false, leftId);
            if (!special) special = FindFirstAvailable(true, SpellId.None);
        }

        // Aplicar configuración al spawner
        _spawner.SetSpells(left, right, special);
        _spawner.SetInstigator(instigatorOverride ? instigatorOverride : gameObject);

        Debug.Log($"[PlayerPresetService] Hechizos configurados - L:{left?.name} R:{right?.name} S:{special?.name}");
    }
}
