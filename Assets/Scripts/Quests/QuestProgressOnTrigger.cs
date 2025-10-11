
using UnityEngine;

public class QuestProgressOnTrigger : MonoBehaviour
{
    [SerializeField] string questId;
    [SerializeField] int stepIndex;
    [SerializeField] bool once = true;
    bool used;

    void OnTriggerEnter(Collider other)
    {
        if (used || !other.CompareTag("Player")) return;
        QuestManager.Instance?.MarkStepDone(questId, stepIndex);
        if (once) used = true;
    }
}
