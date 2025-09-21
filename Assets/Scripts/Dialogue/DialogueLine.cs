using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    [Tooltip("Vacío si es narrador/carta")]
    public string speakerName;

    [TextArea(2,5)]
    public string text;

    [Tooltip("Opcional")]
    public Sprite portrait;
}