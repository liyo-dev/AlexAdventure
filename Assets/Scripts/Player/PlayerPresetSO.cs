using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Player Preset", fileName="PlayerPreset_Default")]
public class PlayerPresetSO : ScriptableObject
{
    [Header("Spawn")]
    [Tooltip("ID del anchor donde debe aparecer el jugador con este preset")]
    public string spawnAnchorId = "Bedroom";

    [Header("Stats")]
    public int   level = 1;
    public float maxHP = 100, currentHP = 100;
    public float maxMP = 50,  currentMP = 50;

    [Header("Desbloqueos")]
    public List<AbilityId> unlockedAbilities = new();
    public List<SpellId>   unlockedSpells    = new();

    [Header("Slots de hechizo (por ID)")]
    public SpellId leftSpellId;
    public SpellId rightSpellId;
    public SpellId specialSpellId;

    [Header("Flags (misiones/estados simples)")]
    public List<string> flags = new();
}