using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestor de UI para mostrar habilidades y hechizos desbloqueados del jugador
/// Se conecta automáticamente con PlayerState para reflejar cambios
/// </summary>
public class PlayerAbilitiesUI : MonoBehaviour
{
    [Header("Referencias UI - Habilidades")]
    [SerializeField] private Transform abilitiesContainer;
    [SerializeField] private GameObject abilityUIPrefab;
    
    [Header("Referencias UI - Hechizos")]
    [SerializeField] private Transform spellsContainer;
    [SerializeField] private GameObject spellUIPrefab;
    
    [Header("Referencias UI - Información")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI manaText;
    
    [Header("Configuración")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private bool showDebugInfo = false;
    
    private PlayerState _playerState;
    private ManaPool _manaPool;
    
    // Cache para optimización
    private readonly List<GameObject> _abilityUIObjects = new();
    private readonly List<GameObject> _spellUIObjects = new();
    
    void Start()
    {
        FindPlayerComponents();
        
        if (_playerState != null)
        {
            // Suscribirse a cambios
            _playerState.OnAbilitiesChanged += RefreshAbilities;
            _playerState.OnSpellsChanged += RefreshSpells;
            _playerState.OnStatsChanged += RefreshStats;
            
            // Inicializar UI
            RefreshAll();
        }
    }
    
    void OnDestroy()
    {
        if (_playerState != null)
        {
            _playerState.OnAbilitiesChanged -= RefreshAbilities;
            _playerState.OnSpellsChanged -= RefreshSpells;
            _playerState.OnStatsChanged -= RefreshStats;
        }
    }
    
    private void FindPlayerComponents()
    {
        // Buscar PlayerState
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerState = player.GetComponent<PlayerState>();
            _manaPool = player.GetComponent<ManaPool>();
        }
        
        if (_playerState == null)
        {
            _playerState = FindObjectOfType<PlayerState>();
            if (_playerState != null)
            {
                _manaPool = _playerState.GetComponent<ManaPool>();
            }
        }
        
        if (_playerState == null)
        {
            Debug.LogWarning("[PlayerAbilitiesUI] No se encontró PlayerState en la escena");
        }
    }
    
    public void RefreshAll()
    {
        RefreshAbilities();
        RefreshSpells();
        RefreshStats();
    }
    
    private void RefreshAbilities()
    {
        if (_playerState == null || abilitiesContainer == null) return;
        
        // Limpiar UI existente
        ClearUIObjects(_abilityUIObjects);
        
        // Crear UI para cada habilidad desbloqueada
        var unlockedAbilities = _playerState.GetUnlockedAbilities();
        foreach (var abilityId in unlockedAbilities)
        {
            CreateAbilityUI(abilityId);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAbilitiesUI] Actualizadas {unlockedAbilities.Count} habilidades");
        }
    }
    
    private void RefreshSpells()
    {
        if (_playerState == null || spellsContainer == null) return;
        
        // Limpiar UI existente
        ClearUIObjects(_spellUIObjects);
        
        // Crear UI para cada hechizo desbloqueado
        var unlockedSpells = _playerState.GetUnlockedSpells();
        foreach (var spellId in unlockedSpells)
        {
            CreateSpellUI(spellId);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAbilitiesUI] Actualizados {unlockedSpells.Count} hechizos");
        }
    }
    
    private void RefreshStats()
    {
        if (_playerState == null) return;
        
        // Actualizar nivel
        if (levelText != null)
        {
            levelText.text = $"Nivel {_playerState.Level}";
        }
        
        // Actualizar maná
        if (manaText != null)
        {
            if (_playerState.MaxMp > 0)
            {
                manaText.text = $"Maná: {_playerState.CurrentMp:0}/{_playerState.MaxMp:0}";
            }
            else
            {
                manaText.text = "Sin maná";
            }
        }
    }
    
    private void CreateAbilityUI(AbilityId abilityId)
    {
        if (abilityUIPrefab == null || abilitiesContainer == null) return;
        
        var uiObject = Instantiate(abilityUIPrefab, abilitiesContainer);
        _abilityUIObjects.Add(uiObject);
        
        // Configurar el UI del ability (esto dependerá de tu prefab)
        var abilityUI = uiObject.GetComponent<AbilityUIElement>();
        if (abilityUI != null)
        {
            abilityUI.SetAbility(abilityId);
        }
        else
        {
            // Fallback: configurar con componentes básicos
            var text = uiObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = abilityId.ToString();
            }
        }
    }
    
    private void CreateSpellUI(SpellId spellId)
    {
        if (spellUIPrefab == null || spellsContainer == null) return;
        
        var uiObject = Instantiate(spellUIPrefab, spellsContainer);
        _spellUIObjects.Add(uiObject);
        
        // Configurar el UI del spell (esto dependerá de tu prefab)
        var spellUI = uiObject.GetComponent<SpellUIElement>();
        if (spellUI != null)
        {
            spellUI.SetSpell(spellId);
        }
        else
        {
            // Fallback: configurar con componentes básicos
            var text = uiObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = spellId.ToString();
            }
        }
    }
    
    private void ClearUIObjects(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        objects.Clear();
    }
    
    // Métodos públicos para uso externo
    public void ForceRefresh()
    {
        RefreshAll();
    }
    
    public int GetAbilitiesCount()
    {
        return _playerState != null ? _playerState.GetUnlockedAbilities().Count : 0;
    }
    
    public int GetSpellsCount()
    {
        return _playerState != null ? _playerState.GetUnlockedSpells().Count : 0;
    }
}

// Interfaces para los elementos UI (para futura implementación)
public interface IAbilityUIElement
{
    void SetAbility(AbilityId abilityId);
}

public interface ISpellUIElement
{
    void SetSpell(SpellId spellId);
}

// Componentes base para elementos UI (opcional)
public class AbilityUIElement : MonoBehaviour, IAbilityUIElement
{
    public void SetAbility(AbilityId abilityId)
    {
        // Implementar según necesidades específicas
        var text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = abilityId.ToString();
        }
    }
}

public class SpellUIElement : MonoBehaviour, ISpellUIElement
{
    public void SetSpell(SpellId spellId)
    {
        // Implementar según necesidades específicas
        var text = GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = spellId.ToString();
        }
    }
}
