using UnityEngine;

[DisallowMultipleComponent]
public class QuestStarterOnInteract : MonoBehaviour
{
    [SerializeField] private string questIdToStart = "";
    [SerializeField] private bool onlyOnce = true;
    bool used;

    // Llama a este método desde el OnFinished del Interactable (o desde UnityEvent del propio diálogo)
    public void StartQuestNow()
    {
        if (used) return;
        QuestManager.Instance?.StartQuest(questIdToStart);
        used = onlyOnce;
    }
}