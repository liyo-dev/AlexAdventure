using UnityEngine;
using UnityEngine.UI;

public class UIButtonSetNone : MonoBehaviour
{
    public CharacterCreatorUI ui;   // referencia al CharacterCreatorUI del Panel
    public string category;         // "Hair", "Head", "Cloak", "Body", etc.

    void Reset()
    {
        // auto-detectar categoría a partir de "Row_*"
        var t = transform;
        while (t != null && !t.name.StartsWith("Row_")) t = t.parent;
        if (t != null) category = t.name.Substring("Row_".Length);
    }

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(ApplyNone);
    }

    void ApplyNone()
    {
        if (ui == null) return;
        ui.SetNone(category);
    }
}