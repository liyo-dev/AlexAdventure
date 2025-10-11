using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSetNone : MonoBehaviour
{
    public CharacterCreatorUI ui;
    public string category;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (ui == null) ui = FindFirstObjectByType<CharacterCreatorUI>();
            if (ui == null) return;

            ui.SetNone(category);
        });
    }
}