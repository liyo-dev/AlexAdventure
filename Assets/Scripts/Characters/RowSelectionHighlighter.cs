using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RowSelectionHighlighter : MonoBehaviour
{
    [Header("Colores")]
    public Color normalColor   = new Color(0.10f, 0.20f, 0.35f, 1f);
    public Color selectedColor = new Color(1f, 0.95f, 0.20f, 1f);

    readonly List<Image> rows = new();
    int current = -1;

    void Start()
    {
        Refresh();
        // Selecciona la primera por si acaso
        if (rows.Count > 0) SetSelected(0);
    }

    /// Llama a esto tras construir la UI.
    public void Refresh()
    {
        rows.Clear();
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith("Row_", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Coge/crea un Image para poder colorear la fila
            var img = child.GetComponent<Image>();
            if (!img)
            {
                img = child.gameObject.AddComponent<Image>();
                img.raycastTarget = false; // no interferir con botones
            }
            img.color = normalColor;
            rows.Add(img);
        }
        current = -1;
    }

    public int Count => rows.Count;

    public void SetSelected(int index)
    {
        if (rows.Count == 0) return;
        index = Mathf.Clamp(index, 0, rows.Count - 1);
        if (current == index) return;

        if (current >= 0 && current < rows.Count) rows[current].color = normalColor;
        rows[index].color = selectedColor;
        current = index;
    }
}