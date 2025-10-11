using UnityEngine;

[CreateAssetMenu(menuName = "Game/Quest", fileName = "Q_NewQuest")]
public class QuestData : ScriptableObject
{
    public string questId;          
    public string displayName;
    [TextArea] public string description;

    [System.Serializable]
    public class Step
    {
        public string description;   
        public string conditionId;
    }
    public Step[] steps;
}