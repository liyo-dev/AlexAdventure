using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedUI : MonoBehaviour
{
    [SerializeField] private UITextId textId;
    [SerializeField] private string fallbackText = "";
    
    private TextMeshProUGUI _tmp;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        if (string.IsNullOrEmpty(fallbackText))
            fallbackText = _tmp.text;
        
        Refresh();
        
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLocaleChanged += Refresh;
    }

    void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLocaleChanged -= Refresh;
    }

    public void Refresh()
    {
        if (LocalizationManager.Instance == null) 
        {
            _tmp.text = fallbackText;
            return;
        }
        
        string key = textId.ToString();
        _tmp.text = LocalizationManager.Instance.Get(key, fallbackText);
    }

    public void SetTextId(UITextId newTextId)
    {
        textId = newTextId;
        Refresh();
    }

    // Para uso desde inspector/debugging
    [ContextMenu("Refresh Text")]
    void RefreshFromContext() => Refresh();
}
