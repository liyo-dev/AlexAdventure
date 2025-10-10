using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CreatorGamepadController : MonoBehaviour
{
    public ModularAutoBuilder builder;
    public RowSelectionHighlighter highlighter; // arrastra Panel aquí

    public List<PartCat> order = new()
    {
        PartCat.Body, PartCat.Cloak, PartCat.Head, PartCat.Hair, PartCat.Eyes,
        PartCat.Mouth, PartCat.Hat, PartCat.Eyebrow, PartCat.Accessory,
        PartCat.OHS, PartCat.Shield, PartCat.Bow
    };

    int rowIndex = 0;
    PlayerControls input;

    void Awake()
    {
        input = new PlayerControls();
        input.Enable();
    }

    void Start()
    {
        // Asegura filas detectadas y resaltado inicial
        if (highlighter)
        {
            highlighter.Refresh();
            highlighter.SetSelected(rowIndex);
        }
    }

    void OnDestroy() => input?.Disable();

    void Update()
    {
        HandleDpadRows();
        HandleActions();
    }

    void HandleDpadRows()
    {
        var gp = Gamepad.current;
        if (gp != null)
        {
            bool moved = false;

            if (gp.dpad.up.wasPressedThisFrame)
            {
                rowIndex = Mathf.Max(0, rowIndex - 1);
                moved = true;
            }
            else if (gp.dpad.down.wasPressedThisFrame)
            {
                rowIndex = Mathf.Min(order.Count - 1, rowIndex + 1);
                moved = true;
            }

            if (moved && highlighter) highlighter.SetSelected(rowIndex);
        }
    }

    void HandleActions()
    {
        var cat = order[rowIndex];

        // A -> siguiente
        if (input.UI.Submit.WasPressedThisFrame())
            builder.Next(cat);

        // X -> anterior (usamos AttackMagicWest del mapa GamePlay)
        if (input.GamePlay.AttackMagicWest.WasPressedThisFrame())
            builder.Prev(cat);

        // B -> on/off
        if (input.UI.Cancel.WasPressedThisFrame())
        {
            var sel = builder.GetSelection();
            if (sel.ContainsKey(cat)) builder.SetByName(cat, null);
            else                      builder.SetByIndex(cat, 0);
        }
    }
}
