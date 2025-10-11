using UnityEngine;

public class QuestCompletionRelay : MonoBehaviour
{
    [SerializeField] private string questId;
    [SerializeField] private UnityEngine.Events.UnityEvent onCompleted;
    bool fired;

    void OnEnable()
    {
        if (QuestManager.Instance) QuestManager.Instance.OnQuestsChanged += Check;
        Check();
    }
    void OnDisable()
    {
        if (QuestManager.Instance) QuestManager.Instance.OnQuestsChanged -= Check;
    }

    void Check()
    {
        if (fired || QuestManager.Instance == null) return;
        if (QuestManager.Instance.GetState(questId) == QuestState.Completed)
        {
            fired = true;
            onCompleted?.Invoke();
        }
    }
}