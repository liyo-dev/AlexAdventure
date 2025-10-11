using UnityEngine;

public class SimpleQuestPickup : MonoBehaviour
{
    [SerializeField] string questId;
    [SerializeField] int stepIndex;

    public void Pick()
    {
        var qm = QuestManager.Instance;
        if (qm == null) return;
        if (qm.GetState(questId) == QuestState.Active)
            qm.MarkStepDone(questId, stepIndex);
    }
}