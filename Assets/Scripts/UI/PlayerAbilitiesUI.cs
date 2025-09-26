﻿using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestor de UI para mostrar habilidades y hechizos desbloqueados del jugador
/// Se conecta automáticamente con GameBootProfile para reflejar cambios
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
    [SerializeField] private bool showDebugInfo; // default false implícito
    [SerializeField] private float refreshInterval = 1f;
    
    private ManaPool _manaPool;
    
    // Cache para optimización
    private readonly List<GameObject> _abilityUIObjects = new();
    private readonly List<GameObject> _spellUIObjects = new();

    // Evita inicializar dos veces si el evento llega más de una vez
    private bool _initialized;
    
    // Reemplazamos Start/Coroutine por esperar al evento del boot service
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
        if (autoRefresh)
        {
            CancelInvoke(nameof(RefreshAll));
        }
    }

    private void HandleProfileReady()
    {
        if (_initialized) return;

        FindPlayerComponents();
        RefreshAll();
        
        if (autoRefresh)
        {
            InvokeRepeating(nameof(RefreshAll), refreshInterval, refreshInterval);
        }

        _initialized = true;
        GameBootService.OnProfileReady -= HandleProfileReady;
    }
    
    private void FindPlayerComponents()
    {
        // Buscar ManaPool del jugador
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _manaPool = player.GetComponent<ManaPool>();
        }
        
        if (_manaPool == null)
        {
            _manaPool = FindAnyObjectByType<ManaPool>();
        }
        
        if (_manaPool == null && showDebugInfo)
        {
            Debug.LogWarning("[PlayerAbilitiesUI] No se encontró ManaPool en la escena");
        }
    }
    
    public void RefreshAll()
    {
        var bootProfile = GameBootService.Profile;
        if (bootProfile == null) return;
        
        var preset = bootProfile.GetActivePresetResolved();
        if (preset == null) return;
        
        RefreshAbilities(preset);
        RefreshSpells(preset);
        RefreshStats(preset);
    }
    
    private void RefreshAbilities(PlayerPresetSO preset)
    {
        if (abilitiesContainer == null || abilityUIPrefab == null) return;
        
        // Limpiar UI existente
        foreach (var obj in _abilityUIObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        _abilityUIObjects.Clear();
        
        // Crear UI para habilidades desbloqueadas
        if (preset.unlockedAbilities != null)
        {
            foreach (var abilityId in preset.unlockedAbilities)
            {
                var uiObject = Instantiate(abilityUIPrefab, abilitiesContainer);
                
                // Configurar el objeto UI (asume que tiene TextMeshProUGUI o similar)
                var text = uiObject.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = abilityId.ToString();
                }
                
                _abilityUIObjects.Add(uiObject);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAbilitiesUI] Abilities refreshed: {preset.unlockedAbilities?.Count ?? 0} abilities");
        }
    }
    
    private void RefreshSpells(PlayerPresetSO preset)
    {
        if (spellsContainer == null || spellUIPrefab == null) return;
        
        // Limpiar UI existente
        foreach (var obj in _spellUIObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        _spellUIObjects.Clear();
        
        // Crear UI para hechizos desbloqueados
        if (preset.unlockedSpells != null)
        {
            foreach (var spellId in preset.unlockedSpells)
            {
                var uiObject = Instantiate(spellUIPrefab, spellsContainer);
                
                // Configurar el objeto UI
                var text = uiObject.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = spellId.ToString();
                }
                
                _spellUIObjects.Add(uiObject);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerAbilitiesUI] Spells refreshed: {preset.unlockedSpells?.Count ?? 0} spells");
        }
    }
    
    private void RefreshStats(PlayerPresetSO preset)
    {
        // Actualizar nivel
        if (levelText != null)
        {
            levelText.text = $"Nivel: {preset.level}";
        }
        
        // Actualizar maná
        if (manaText != null)
        {
            if (_manaPool != null)
            {
                manaText.text = $"Maná: {_manaPool.Current:0}/{_manaPool.Max:0}";
            }
            else
            {
                manaText.text = $"Maná: {preset.currentMP:0}/{preset.maxMP:0}";
            }
        }
    }
    
    // Métodos públicos para refrescar componentes individuales
    public void RefreshAbilities()
    {
        var preset = GameBootService.Profile?.GetActivePresetResolved();
        if (preset != null) RefreshAbilities(preset);
    }
    
    public void RefreshSpells()
    {
        var preset = GameBootService.Profile?.GetActivePresetResolved();
        if (preset != null) RefreshSpells(preset);
    }
    
    public void RefreshStats()
    {
        var preset = GameBootService.Profile?.GetActivePresetResolved();
        if (preset != null) RefreshStats(preset);
    }
    
    // Método para debugging
    [ContextMenu("Refresh All (Debug)")]
    private void DebugRefresh()
    {
        RefreshAll();
    }
}
