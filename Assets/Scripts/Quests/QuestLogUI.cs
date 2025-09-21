using UnityEngine;
using TMPro;
using System.Linq;
using System.Text;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsChanged -= Refresh;
    }

    void Refresh()
    {
        if (text == null || QuestManager.Instance == null) return;

        var sb = new StringBuilder();
        foreach (var rq in QuestManager.Instance.GetAll().OrderBy(q => q.State))
        {
            sb.AppendLine($"<b>{rq.Data.title}</b>  [{rq.State}]");
            for (int i = 0; i < rq.Steps.Length; i++)
            {
                var s = rq.Steps[i];
                string mark = s.completed ? "•" : "○";
                sb.AppendLine($"  {mark} {s.description}");
            }
            sb.AppendLine();
        }
        text.text = sb.ToString();
    }
}