using UnityEngine;

[System.Serializable]
public class QuestStep
{
    [Tooltip("Texto que se mostrará en el registro de misión")]
    public string description;
    [Tooltip("ID de evento/condición opcional para autocompletar este paso")]
    public string conditionId;
    public bool completed;
}