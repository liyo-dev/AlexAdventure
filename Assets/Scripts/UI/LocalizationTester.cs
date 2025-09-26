using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script para testear el sistema de localización desde el inspector
/// Usa el nuevo Input System y las teclas QWERTYUIOP para cambiar idiomas
/// </summary>
public class LocalizationTester : MonoBehaviour
{
    [Header("Test de Localización")]
    [SerializeField] private string[] availableLanguages = { "es", "en" };
    [SerializeField] private int currentLanguageIndex = 0;
    
    [Header("Input Keys (QWER)")]
    [SerializeField] private bool enableKeyboardTesting = true;
    [Tooltip("Q=Español, W=Inglés, E=Alternar entre ambos")]
    [SerializeField] private bool showKeyboardHelp = true;
    
    [Header("Debug")]
    [SerializeField] private bool showCurrentLanguage = true;
    
    private void Start()
    {
        if (showCurrentLanguage && LocalizationManager.Instance != null)
        {
            Debug.Log($"[LocalizationTester] Idioma actual: {LocalizationManager.Instance.CurrentLocale}");
        }
        
        if (showKeyboardHelp && enableKeyboardTesting)
        {
            Debug.Log("[LocalizationTester] Teclas de testing: Q=Español, W=Inglés, E=Alternar entre ambos");
        }
    }
    
    [ContextMenu("Cambiar al siguiente idioma")]
    public void NextLanguage()
    {
        if (availableLanguages.Length == 0) return;
        
        currentLanguageIndex = (currentLanguageIndex + 1) % availableLanguages.Length;
        ChangeToLanguage(availableLanguages[currentLanguageIndex]);
    }
    
    [ContextMenu("Cambiar al idioma anterior")]
    public void PreviousLanguage()
    {
        if (availableLanguages.Length == 0) return;
        
        currentLanguageIndex--;
        if (currentLanguageIndex < 0) currentLanguageIndex = availableLanguages.Length - 1;
        ChangeToLanguage(availableLanguages[currentLanguageIndex]);
    }
    
    public void ChangeToLanguage(string languageCode)
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("[LocalizationTester] LocalizationManager no está disponible");
            return;
        }
        
        LocalizationManager.Instance.ChangeLanguage(languageCode);
        Debug.Log($"[LocalizationTester] Idioma cambiado a: {languageCode}");
    }
    
    // Métodos públicos para UI
    public void ChangeToSpanish() => ChangeToLanguage("es");
    public void ChangeToEnglish() => ChangeToLanguage("en");
    
    // Para usar desde teclas usando el nuevo Input System
    private void Update()
    {
        if (!enableKeyboardTesting) return;
        
        // Teclas específicas para idiomas
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            ChangeToSpanish();
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            ChangeToEnglish();
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Alternar entre los dos idiomas
            NextLanguage();
        }
        // Teclas adicionales para más idiomas si se agregan
        else if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            // Reservada para futuro idioma
            Debug.Log("[LocalizationTester] Tecla Y reservada para futuro idioma");
        }
        else if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            // Mostrar ayuda
            ShowKeyboardHelp();
        }
    }
    
    private void ShowKeyboardHelp()
    {
        Debug.Log("=== LOCALIZATION TESTER - AYUDA DE TECLAS ===");
        Debug.Log("Q = Cambiar a Español");
        Debug.Log("W = Cambiar a Inglés");
        Debug.Log("E = Alternar entre Español e Inglés");
        Debug.Log("U = Mostrar esta ayuda");
        Debug.Log("============================================");
    }
    
    private void OnGUI()
    {
        if (!showCurrentLanguage || LocalizationManager.Instance == null) return;
        
        GUI.Label(new Rect(10, 10, 300, 20), $"Idioma: {LocalizationManager.Instance.CurrentLocale}");
        
        if (enableKeyboardTesting)
        {
            GUI.Label(new Rect(10, 30, 400, 20), "Testing: Q=ES, W=EN, E=Toggle, U=Help");
        }
        
        if (GUI.Button(new Rect(10, 55, 100, 25), "Siguiente"))
        {
            NextLanguage();
        }
        
        if (GUI.Button(new Rect(115, 55, 100, 25), "Anterior"))
        {
            PreviousLanguage();
        }
        
        // Botones específicos para cada idioma
        if (GUI.Button(new Rect(220, 55, 60, 25), "ES"))
        {
            ChangeToSpanish();
        }
        
        if (GUI.Button(new Rect(285, 55, 60, 25), "EN"))
        {
            ChangeToEnglish();
        }
    }
}
