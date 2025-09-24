using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerState : MonoBehaviour
{
    // ===== RUNTIME STATS =====
    public int    Level      { get; private set; }
    public float  MaxHp      { get; private set; }
    public float  CurrentHp  { get; private set; }
    public float  MaxMp      { get; private set; }
    public float  CurrentMp  { get; private set; }
    public string LastSpawnAnchorId { get; private set; }

    // Desbloqueos / flags (runtime)
    readonly HashSet<AbilityId> _abilities = new();
    readonly HashSet<SpellId>   _spells    = new();
    readonly HashSet<string>    _flags     = new();

    // Referencias a otros componentes del jugador (opcional)
    ManaPool _mana;
    PlayerHealthSystem _healthSystem;

    // Eventos para notificar cambios
    public System.Action OnAbilitiesChanged;
    public System.Action OnSpellsChanged;
    public System.Action OnFlagsChanged;
    public System.Action OnStatsChanged;

    void Awake()
    {
        // Buscar componentes relacionados (no requeridos)
        _mana = GetComponent<ManaPool>();
        _healthSystem = GetComponent<PlayerHealthSystem>();
    }

    // ===== CARGA DESDE SAVE / PRESET =====

    public void LoadFromSave(PlayerSaveData d)
    {
        if (d == null) return;

        Level     = Mathf.Max(1, d.level);
        MaxHp     = Mathf.Max(1f, d.maxHp);
        CurrentHp = Mathf.Clamp(d.currentHp, 0f, MaxHp);
        MaxMp     = Mathf.Max(0f, d.maxMp);
        CurrentMp = Mathf.Clamp(d.currentMp, 0f, MaxMp);
        if (!string.IsNullOrEmpty(d.lastSpawnAnchorId)) LastSpawnAnchorId = d.lastSpawnAnchorId;

        _abilities.Clear(); if (d.abilities != null) foreach (var a in d.abilities) _abilities.Add(a);
        _spells.Clear();    if (d.spells    != null) foreach (var s in d.spells)    _spells.Add(s);
        _flags.Clear();     if (d.flags     != null) foreach (var f in d.flags)     _flags.Add(f);

        ApplyToComponents();
    }

    public void ApplyPreset(PlayerPresetSO p, string spawnAnchorId = null)
    {
        if (!p) return;

        Level     = Mathf.Max(1, p.level);
        MaxHp     = Mathf.Max(1f, p.maxHP);
        CurrentHp = Mathf.Clamp(p.currentHP, 0f, MaxHp);
        MaxMp     = Mathf.Max(0f, p.maxMP);
        CurrentMp = Mathf.Clamp(p.currentMP, 0f, MaxMp);
        if (!string.IsNullOrEmpty(spawnAnchorId)) LastSpawnAnchorId = spawnAnchorId;

        _abilities.Clear(); if (p.unlockedAbilities != null) foreach (var a in p.unlockedAbilities) _abilities.Add(a);
        _spells.Clear();    if (p.unlockedSpells    != null) foreach (var s in p.unlockedSpells)    _spells.Add(s);
        _flags.Clear();     if (p.flags             != null) foreach (var f in p.flags)             _flags.Add(f);

        ApplyToComponents();
    }

    // ===== GUARDADO =====

    public PlayerSaveData CreateSave()
    {
        return new PlayerSaveData
        {
            lastSpawnAnchorId = string.IsNullOrEmpty(LastSpawnAnchorId) ? SpawnManager.CurrentAnchorId : LastSpawnAnchorId,
            level     = Level,
            maxHp     = MaxHp,     currentHp = CurrentHp,
            maxMp     = MaxMp,     currentMp = CurrentMp,
            abilities = new List<AbilityId>(_abilities),
            spells    = new List<SpellId>(_spells),
            flags     = new List<string>(_flags)
        };
    }

    // ===== SINCRONIZACIÓN CON COMPONENTES =====

    public void ApplyToComponents()
    {
        // Sincronizar con ManaPool
        if (_mana) 
        {
            _mana.Init(MaxMp, CurrentMp);
        }

        // IMPORTANTE: Ya no usamos Damageable para el jugador
        // El PlayerHealthSystem se sincroniza automáticamente mediante eventos
        
        OnStatsChanged?.Invoke();
    }

    // ===== API SIMPLE =====

    public void SetSpawnAnchor(string anchorId)
    {
        if (!string.IsNullOrEmpty(anchorId)) LastSpawnAnchorId = anchorId;
    }

    public bool HasAbility(AbilityId a) => _abilities.Contains(a);
    public bool HasSpell(SpellId s)     => _spells.Contains(s);
    public bool HasFlag(string f)       => _flags.Contains(f);

    public void UnlockAbility(AbilityId a) { if (_abilities.Add(a)) OnAbilitiesChanged?.Invoke(); }
    public void UnlockSpell(SpellId s)     { if (_spells.Add(s))    OnSpellsChanged?.Invoke(); }
    public void SetFlag(string f, bool v)
    {
        bool ch = v ? _flags.Add(f) : _flags.Remove(f);
        if (ch) OnFlagsChanged?.Invoke();
    }

    // Setters de stats (por si HUD / debug / gameplay)
    public void SetLevel(int v)       { Level = Mathf.Max(1, v); OnStatsChanged?.Invoke(); }
    public void SetMaxHealth(float v) { MaxHp = Mathf.Max(1f, v); CurrentHp = Mathf.Clamp(CurrentHp, 0f, MaxHp); OnStatsChanged?.Invoke(); }
    public void SetHealth(float v)    { CurrentHp = Mathf.Clamp(v, 0f, MaxHp); OnStatsChanged?.Invoke(); }
    public void SetMaxMana(float v)   { MaxMp = Mathf.Max(0f, v); CurrentMp = Mathf.Clamp(CurrentMp, 0f, MaxMp); ApplyToComponents(); }
    public void SetMana(float v)      { CurrentMp = Mathf.Clamp(v, 0f, MaxMp); if (_mana) _mana.Init(MaxMp, CurrentMp); OnStatsChanged?.Invoke(); }

    // Snapshots para SaveSystem
    public List<AbilityId> GetAbilitiesSnapshot() => new(_abilities);
    public List<SpellId>   GetSpellsSnapshot()    => new(_spells);
    public List<string>    GetFlagsSnapshot()     => new(_flags);

    // ===== MÉTODOS PÚBLICOS PARA SISTEMAS EXTERNOS =====
    
    /// <summary>Obtiene todas las habilidades desbloqueadas</summary>
    public IReadOnlyCollection<AbilityId> GetUnlockedAbilities() => _abilities;
    
    /// <summary>Obtiene todos los hechizos desbloqueados</summary>
    public IReadOnlyCollection<SpellId> GetUnlockedSpells() => _spells;
    
    /// <summary>Obtiene todas las flags activas</summary>
    public IReadOnlyCollection<string> GetActiveFlags() => _flags;

    /// <summary>Verifica si el jugador está vivo</summary>
    public bool IsAlive => CurrentHp > 0f;
    
    /// <summary>Obtiene el porcentaje de vida (0-1)</summary>
    public float HealthPercentage => MaxHp > 0 ? CurrentHp / MaxHp : 0f;
    
    /// <summary>Obtiene el porcentaje de maná (0-1)</summary>
    public float ManaPercentage => MaxMp > 0 ? CurrentMp / MaxMp : 0f;

#if UNITY_EDITOR
    void OnValidate()
    {
        MaxHp = Mathf.Max(1f, MaxHp);
        MaxMp = Mathf.Max(0f, MaxMp);
        if (Application.isPlaying)
        {
            CurrentHp = Mathf.Clamp(CurrentHp, 0f, MaxHp);
            CurrentMp = Mathf.Clamp(CurrentMp, 0f, MaxMp);
        }
    }
#endif
}
