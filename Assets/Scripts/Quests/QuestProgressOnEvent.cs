
using UnityEngine;

public class QuestProgressOnEvent : MonoBehaviour
{
    [SerializeField] string questId;
    [SerializeField] int stepIndex;

    public void Mark()
    {
        QuestManager.Instance?.MarkStepDone(questId, stepIndex);
    }
}
