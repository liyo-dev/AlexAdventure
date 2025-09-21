using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [SerializeField] private List<QuestData> questCatalog = new(); // opcional: catálogo global
    private readonly Dictionary<string, RuntimeQuest> runtime = new();

    public event Action OnQuestsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasQuest(string questId) => runtime.ContainsKey(questId);
    public QuestState GetState(string questId) => HasQuest(questId) ? runtime[questId].State : QuestState.Inactive;

    public void AddQuest(QuestData data)
    {
        if (data == null || string.IsNullOrEmpty(data.questId)) return;
        if (runtime.ContainsKey(data.questId)) return;

        runtime[data.questId] = new RuntimeQuest(data);
        OnQuestsChanged?.Invoke();
    }

    public void Activate(string questId)
    {
        if (!runtime.TryGetValue(questId, out var rq)) return;
        if (rq.State == QuestState.Inactive) rq.State = QuestState.Active;
        OnQuestsChanged?.Invoke();
    }

    public void CompleteStep(string questId, int index)
    {
        if (!runtime.TryGetValue(questId, out var rq)) return;
        if (rq.State != QuestState.Active) return;

        if (index >= 0 && index < rq.Steps.Length)
        {
            rq.Steps[index].completed = true;

            // Si todos los pasos están completados → misión completada.
            if (rq.Steps.All(s => s.completed))
                rq.State = QuestState.Completed;

            OnQuestsChanged?.Invoke();
        }
    }

    public void CompleteByCondition(string conditionId)
    {
        if (string.IsNullOrEmpty(conditionId)) return;

        foreach (var rq in runtime.Values)
        {
            if (rq.State != QuestState.Active) continue;
            for (int i = 0; i < rq.Steps.Length; i++)
            {
                var step = rq.Steps[i];
                if (!step.completed && step.conditionId == conditionId)
                {
                    CompleteStep(rq.Id, i);
                }
            }
        }
    }

    public IEnumerable<RuntimeQuest> GetAll() => runtime.Values;

    // ===== Runtime model =====
    public class RuntimeQuest
    {
        public string Id => Data.questId;
        public QuestData Data { get; }
        public QuestState State { get; set; }
        public QuestStep[] Steps { get; }

        public RuntimeQuest(QuestData data)
        {
            Data = data;
            State = QuestState.Inactive;
            // Copia profunda ligera de pasos para estado en runtime
            Steps = data.steps != null ? data.steps.Select(s => new QuestStep {
                description = s.description, conditionId = s.conditionId, completed = false
            }).ToArray() : new QuestStep[0];
        }
    }
}
