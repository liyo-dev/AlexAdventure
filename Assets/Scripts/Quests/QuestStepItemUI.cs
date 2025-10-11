using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestStepItemUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Sprite spritePending;
    [SerializeField] private Sprite spriteDone;
    [SerializeField] private Color colorPending = Color.white;
    [SerializeField] private Color colorDone = Color.white;

    public void Bind(QuestStep step)
    {
        if (label) label.text = step?.description ?? "";
        if (icon)
        {
            bool done = step != null && step.completed;
            icon.sprite = done ? spriteDone : spritePending;
            icon.color  = done ? colorDone   : colorPending;
        }
    }
}