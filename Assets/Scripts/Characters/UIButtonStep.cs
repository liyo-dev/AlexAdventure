using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonStep : MonoBehaviour
{
    public CharacterCreatorUI ui;
    public string category;
    public int step = 1;

    void Awake() => GetComponent<Button>().onClick.AddListener(() => ui.Step(category, step));
}