using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class QuestLogItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI firstStepDesc; // Descripción del primer paso activo
    [SerializeField] private TextMeshProUGUI statePillText;
    [SerializeField] private Image statePillBg;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressFill;      // barra de progreso (Fill)
    [SerializeField] private Transform stepsRoot;     // contenedor de steps
    [SerializeField] private QuestStepItemUI stepPrefab;
    [SerializeField] private GameObject stepsContainer; // el GameObject completo para show/hide

    [Header("Estilos")]
    [SerializeField] private Color colorInactive = new Color(0.6f,0.6f,0.6f);
    [SerializeField] private Color colorActive = new Color(0.2f,0.6f,1f);
    [SerializeField] private Color colorCompleted = new Color(0.3f,0.8f,0.4f);

    private bool _isExpanded = false;

    public void Bind(QuestManager.RuntimeQuest data)
    {
        // Título de la quest
        string display = string.IsNullOrEmpty(data.Data.displayName) ? data.Id : data.Data.displayName;
        if (questName) questName.text = display;

        // Obtener steps una sola vez para todo el método
        var steps = data.Steps ?? Array.Empty<QuestStep>();

        // Descripción del primer paso no completado
        if (firstStepDesc)
        {
            string stepDescription = "";
            
            // Buscar el primer paso no completado
            foreach (var step in steps)
            {
                if (!step.completed)
                {
                    stepDescription = step.description;
                    break;
                }
            }
            
            // Si todos están completados, mostrar el último paso
            if (string.IsNullOrEmpty(stepDescription) && steps.Length > 0)
            {
                stepDescription = steps[steps.Length - 1].description;
            }
            
            firstStepDesc.text = stepDescription;
        }

        switch (data.State)
        {
            case QuestState.Inactive:
                if (statePillText) statePillText.text = "Inactiva";
                if (statePillBg) statePillBg.color = colorInactive;
                break;
            case QuestState.Active:
                if (statePillText) statePillText.text = "Activa";
                if (statePillBg) statePillBg.color = colorActive;
                break;
            case QuestState.Completed:
                if (statePillText) statePillText.text = "Completada";
                if (statePillBg) statePillBg.color = colorCompleted;
                break;
        }

        // Calcular progreso
        int done = 0;
        foreach (var s in steps) if (s.completed) done++;
        float pct = (steps.Length == 0) ? 1f : (float)done / steps.Length;

        if (progressFill) progressFill.fillAmount = pct;
        if (progressText) progressText.text = steps.Length > 0 ? $"{done}/{steps.Length}" : "—";

        if (stepsRoot && stepPrefab)
        {
            for (int i = stepsRoot.childCount - 1; i >= 0; i--)
                GameObject.Destroy(stepsRoot.GetChild(i).gameObject);

            for (int i = 0; i < steps.Length; i++)
            {
                var item = GameObject.Instantiate(stepPrefab, stepsRoot);
                item.Bind(steps[i]);
            }
        }

        // Inicialmente colapsado
        SetExpanded(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleExpanded();
    }

    public void ToggleExpanded()
    {
        SetExpanded(!_isExpanded);
    }

    public void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
        if (stepsContainer)
        {
            stepsContainer.SetActive(_isExpanded);
        }
    }
}
