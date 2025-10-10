using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCreatorUI : MonoBehaviour
{
    public ModularAutoBuilder builder;

    public void Step(string catName, int step)
    {
        if (!Enum.TryParse(catName, out PartCat cat)) return;
        if (step >= 0) builder.Next(cat, step); else builder.Prev(cat);
    }

    public void RandomizeAll() => builder.RandomizeAll();

    // --- NUEVO: quitar la pieza de una categoría ---
    public void SetNone(string category)
    {
        if (!Enum.TryParse(category, out PartCat cat)) return;
        builder.SetByName(cat, null);
    }

    // Los de abajo son opcionales (ya no los usamos, pero no molestan)
    [Serializable] class DTO { public List<string> k = new(); public List<string> v = new(); }
    const string KEY = "PLAYER_CHAR_SELECTION";

    public void Save()
    {
        var sel = builder.GetSelection();
        var dto = new DTO();
        foreach (var kv in sel) { dto.k.Add(kv.Key.ToString()); dto.v.Add(kv.Value); }
        PlayerPrefs.SetString(KEY, JsonUtility.ToJson(dto));
        PlayerPrefs.Save();
        Debug.Log("[Creator] Selección guardada.");
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(KEY)) return;
        var dto = JsonUtility.FromJson<DTO>(PlayerPrefs.GetString(KEY));
        var dict = new Dictionary<PartCat, string>();
        for (int i = 0; i < dto.k.Count; i++)
            if (Enum.TryParse(dto.k[i], out PartCat cat)) dict[cat] = dto.v[i];
        builder.ApplySelection(dict);
        Debug.Log("[Creator] Selección cargada.");
    }
}