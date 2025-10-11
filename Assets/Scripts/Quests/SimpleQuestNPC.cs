using UnityEngine;

public class SimpleQuestNPC : MonoBehaviour
{
    [Header("Quest")]
    [SerializeField] QuestData questData;
    [SerializeField] int talkStepIndex = 0;

    [Header("Diálogos")]
    [SerializeField] DialogueAsset dlgBefore;       // Antes de tener la misión
    [SerializeField] DialogueAsset dlgInProgress;   // Si hay más pasos
    [SerializeField] DialogueAsset dlgTurnIn;       // Entrega
    [SerializeField] DialogueAsset dlgCompleted;    // Ya completada

    // Llama a esto desde Interactable → On Interact()
    public void Interact()
    {
        var qm = QuestManager.Instance;
        if (qm == null || questData == null) return;

        string questId = questData.questId;

        switch (qm.GetState(questId))
        {
            case QuestState.Inactive:
                Play(dlgBefore);
                break;

            case QuestState.Active:
                // Marca el paso de hablar con este NPC
                if (!qm.IsStepCompleted(questId, talkStepIndex))
                    qm.MarkStepDone(questId, talkStepIndex);

                if (qm.AreAllStepsCompleted(questId))
                {
                    Play(dlgTurnIn, () =>
                    {
                        qm.CompleteQuest(questId);
                        Play(dlgCompleted);
                    });
                }
                else
                {
                    Play(dlgInProgress);
                }
                break;

            case QuestState.Completed:
                Play(dlgCompleted);
                break;
        }
    }

    void Play(DialogueAsset dlg, System.Action onClosed = null)
    {
        if (dlg == null) { onClosed?.Invoke(); return; }
        DialogueManager.Instance.StartDialogue(dlg, onClosed); // tu StartDialogue con callback
    }
}