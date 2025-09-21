using UnityEngine;

[CreateAssetMenu(menuName = "Game/Quest", fileName = "Q_NewQuest")]
public class QuestData : ScriptableObject
{
    public string questId;
    public string title;
    [TextArea(2,5)] public string description;
    public QuestStep[] steps;
}