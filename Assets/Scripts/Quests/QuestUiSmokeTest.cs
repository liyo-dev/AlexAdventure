using UnityEngine;
using System.Collections;

public class QuestUiSmokeTest : MonoBehaviour
{
    [SerializeField] QuestData testQuest;

    IEnumerator Start()
    {
        // Espera hasta que QuestManager esté creado (desde tu escena Start o bootstrap)
        while (QuestManager.Instance == null) yield return null;

        var qm = QuestManager.Instance;
        if (testQuest && !qm.HasQuest(testQuest.questId))
            qm.AddQuest(testQuest);

        qm.StartQuest(testQuest.questId);
        qm.MarkStepDone(testQuest.questId, 0); // para ver barra
    }
}