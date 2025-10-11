using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Conecta los botones de acciones (Random y Bake NPC) en runtime
/// </summary>
public class CharacterCreatorActions : MonoBehaviour
{
    public CharacterCreatorUI ui;
    public ModularAutoBuilder builder;

    void Start()
    {
        // Buscar los botones por nombre en el Row_Actions
        Transform actionsRow = transform.Find("Panel_Left/Row_Actions");
        if (!actionsRow)
        {
            Debug.LogWarning("No se encontró Row_Actions");
            return;
        }

        // Conectar botón Random
        Transform randomBtn = actionsRow.Find("Button");
        if (randomBtn)
        {
            var button = randomBtn.GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnRandomClick);
                Debug.Log("Botón Random conectado");
            }
        }

        // Buscar el segundo botón (Bake NPC) y ocultarlo
        int buttonCount = 0;
        foreach (Transform child in actionsRow)
        {
            if (child.name == "Button")
            {
                buttonCount++;
                if (buttonCount == 2)
                {
                    // Ocultar el botón Bake NPC ya que no se va a usar
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    void OnRandomClick()
    {
        Debug.Log("Random presionado");
        if (ui)
            ui.RandomizeAll();
        else
            Debug.LogWarning("UI no asignado en CharacterCreatorActions");
    }
}
