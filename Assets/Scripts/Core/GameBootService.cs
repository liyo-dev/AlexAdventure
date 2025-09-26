using UnityEngine;

/// <summary>
/// Servicio simple que hace persistir el GameBootProfile entre escenas
/// Su única función es actuar como contenedor estático del ScriptableObject
/// </summary>
public class GameBootService : MonoBehaviour
{
    [Header("Boot Profile")]
    [SerializeField] private GameBootProfile bootProfile;
    
    // Cache estático para acceso global
    private static GameBootProfile _profile;
    private static bool _isInitialized = false;
    
    // Evento para notificar cuando el profile está listo
    public static event System.Action OnProfileReady;
    
    // Propiedad pública para acceder al profile desde cualquier lugar
    public static GameBootProfile Profile => _profile;
    
    void Awake()
    {
        // Si ya tenemos el profile cacheado, destruir este GameObject (evita duplicados)
        if (_isInitialized)
        {
            Debug.Log("[GameBootService] Profile ya está inicializado. Destruyendo duplicado.");
            Destroy(gameObject);
            return;
        }
        
        // Validar que tenemos el profile asignado
        if (bootProfile == null)
        {
            Debug.LogError("[GameBootService] No se ha asignado GameBootProfile en el inspector!");
            Destroy(gameObject);
            return;
        }
        
        // Cachear el profile para acceso global
        _profile = bootProfile;
        _isInitialized = true;
        
        // Hacer que este GameObject persista entre escenas
        DontDestroyOnLoad(gameObject);
        
        Debug.Log($"[GameBootService] GameBootProfile '{bootProfile.name}' cacheado y servicio persistente.");
        
        // Notificar que el profile está listo
        OnProfileReady?.Invoke();
    }
    
    /// <summary>
    /// Verifica si el GameBootProfile está disponible
    /// </summary>
    public static bool IsAvailable => _profile != null && _isInitialized;
}
