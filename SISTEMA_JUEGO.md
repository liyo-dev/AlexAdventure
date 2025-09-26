# Sistema de Juego - Oblivion
## Documentación Técnica Completa

### 📋 Índice
- [Arquitectura General](#arquitectura-general)
- [Sistema de Localización](#sistema-de-localización)
- [GameBootService y GameBootProfile](#gamebootservice-y-gamebootprofile)
- [Sistema de Salud del Jugador](#sistema-de-salud-del-jugador)
- [Sistema de Spawn y Anchors](#sistema-de-spawn-y-anchors)
- [Sistema de Interacciones](#sistema-de-interacciones)
- [Sistema de UI](#sistema-de-ui)
- [Sistema de Save/Load](#sistema-de-saveload)
- [Guías de Uso](#guías-de-uso)

---

## 🏗️ Arquitectura General

El juego utiliza una **arquitectura centralizada** basada en **GameBootService** que gestiona el **GameBootProfile** desde la escena start. Se eliminó el PlayerState para simplificar la gestión de datos y se reemplazó el sistema singleton automático por un servicio explícito.

### Componentes Principales:
- **GameBootService** - Servicio en escena start que gestiona el GameBootProfile
- **GameBootProfile** - SO con configuración y estado del juego (sin singleton automático)
- **PlayerPresetSO** - Datos del jugador (vida, maná, habilidades, etc.)
- **PlayerHealthSystem** - Gestión específica de vida del jugador
- **ManaPool** - Gestión de maná
- **SpawnManager** - Control de posición y anchors
- **LocalizationManager** - Sistema multiidioma

### Flujo de Inicialización:
1. **Escena Start** → GameBootService carga GameBootProfile SO
2. **Otras escenas** → Scripts esperan a `GameBootService.IsReady()`
3. **Acceso** → `GameBootService.GetProfile()` en lugar de singleton automático

---

## 🌍 Sistema de Localización

### LocalizationManager.cs
Sistema completo de localización con soporte para múltiples idiomas.

**Características:**
- Carga automática desde archivos JSON en `Resources/Localization/`
- Sistema de fallback al idioma por defecto
- Soporte para textos UI y subtítulos
- Cambio dinámico de idioma

**Archivos JSON:**
- `ui_es.json` - Textos de interfaz en español
- `ui_en.json` - Textos de interfaz en inglés  
- `prologue_es.json` - Diálogos del prólogo en español

### Enumerados de Localización
```csharp
public enum UITextId 
{
    MainMenu_NewGame,
    MainMenu_Continue,
    Settings_Language,
    UI_Health,
    UI_Mana,
    Interact_Press,
    // ... más IDs
}

public enum DialogueId
{
    NPC_Villager_01,
    Object_Sign_01,
    Tutorial_Movement,
    // ... más IDs
}
```

### LocalizedUI.cs
Componente para textos UI que se actualizan automáticamente:
```csharp
[SerializeField] private UITextId textId;
[SerializeField] private string fallbackText = "";
```

### LocalizedMessage.cs
Para mostrar mensajes simples usando DialogueManager:
```csharp
[SerializeField] private DialogueId messageId = DialogueId.Object_Sign_01;
public void ShowMessage() // Muestra el mensaje localizado
```

### LocalizationTester.cs
Para testing de idiomas usando nuevo Input System:
- **Tecla Q** - Español
- **Tecla W** - Inglés  
- **Tecla E** - Alternar entre ambos
- **Tecla U** - Ayuda
- Métodos públicos para UI: `ChangeToSpanish()`, `ChangeToEnglish()`

### SubtitleController.cs
Sistema de subtítulos integrado con localización:
```csharp
// Métodos principales
public void ShowLineById(string id)           // Usa LocalizationManager
public void ShowLineByIdTimed(string id, float time)
public void ShowSubtitleEvent(string id)     // Para Animation Events
```

**Características:**
- Actualización automática al cambiar idioma
- Espera a LocalizationManager antes de suscribirse
- Compatible con Timeline y Animation Events

---

## 🎮 GameBootService y GameBootProfile

### GameBootService.cs
**Servicio principal** que gestiona el GameBootProfile desde la escena start.

```csharp
public class GameBootService : MonoBehaviour
{
    [SerializeField] private GameBootProfile bootProfile;
    
    // API estática
    public static GameBootService Instance { get; }
    public static GameBootProfile Profile => Instance?.bootProfile;
    
    // Métodos principales
    public static GameBootProfile GetProfile()
    public static bool IsReady()
    public static PlayerPresetSO GetActivePreset()
}
```

**Configuración:**
1. Crear GameObject en escena start
2. Añadir componente GameBootService
3. Asignar GameBootProfile SO en inspector
4. Se hace DontDestroyOnLoad automáticamente

### GameBootProfile.cs (SO sin Singleton)
```csharp
[CreateAssetMenu(fileName = "GameBootProfile", menuName = "Game/Boot Profile")]
public class GameBootProfile : ScriptableObject
{
    [Header("Arranque")]
    public string sceneToLoad = "MainWorld";
    public string defaultAnchorId = "Bedroom";
    public PlayerPresetSO defaultPlayerPreset;

    [Header("Boot Settings")]
    public bool usePresetInsteadOfSave = false;
    public PlayerPresetSO bootPreset;
    public string startAnchorId = "Bedroom";

    [Header("Runtime Fallback")]
    public PlayerPresetSO runtimePreset;
}
```

### Métodos Principales
```csharp
// Obtener preset activo
PlayerPresetSO GetActivePresetResolved()

// Gestión de save/load
bool SaveCurrentGameState(SaveSystem saveSystem)
bool LoadProfile(SaveSystem saveSystem)

// Configuración desde save
void SetRuntimePresetFromSave(PlayerSaveData data, PlayerPresetSO template)
```

### Flujo de Inicialización
1. **Modo Preset** - Si `usePresetInsteadOfSave = true`, usa `bootPreset`
2. **Modo Normal** - Carga save y crea `runtimePreset`
3. **Fallback** - Usa `defaultPlayerPreset` si no hay otros

### Patrón de Uso en Scripts
```csharp
private IEnumerator DelayedInitialization()
{
    // Esperar hasta que GameBootService esté disponible
    while (!GameBootService.IsReady())
    {
        yield return new WaitForSeconds(0.1f);
    }
    
    // Usar el servicio
    var bootProfile = GameBootService.GetProfile();
    var preset = bootProfile?.GetActivePresetResolved();
    // ... lógica del script
}
```

---

## 💚 Sistema de Salud del Jugador

### PlayerHealthSystem.cs
Sistema independiente que se sincroniza con GameBootService.

**Configuración:**
```csharp
[Header("Configuración de Daño")]
[SerializeField] private float invulnerabilityDuration = 1f;
[SerializeField] private bool godMode = false;

[Header("Efectos Visuales")]
[SerializeField] private float damageFlashDuration = 0.2f;
[SerializeField] private Color damageFlashColor = Color.red;
[SerializeField] private GameObject damageVFX;
```

**API Principal:**
```csharp
// Propiedades
bool IsAlive { get; }
float CurrentHealth { get; }
float MaxHealth { get; }
float HealthPercentage { get; }

// Métodos
bool TakeDamage(float amount)
bool Heal(float amount)
void Kill()
void Revive(float healthPercentage = 1f)
void SetMaxHealth(float newMaxHp)
void SetCurrentHealth(float newCurrentHp)
```

**Eventos:**
```csharp
public UnityEvent<float> OnHealthChanged;
public UnityEvent<float, float> OnDamageTaken;
public UnityEvent OnPlayerDeath;
public UnityEvent OnPlayerRevived;
```

**Sincronización Automática:**
- Lee valores iniciales del GameBootService al arrancar
- Actualiza el GameBootProfile cuando cambia la vida
- Efectos visuales, sonoros y animaciones integradas

---

## 🔵 Sistema de Maná

### ManaPool.cs
```csharp
public float Max { get; }
public float Current { get; }

public void Init(float maxMP, float currentMP)
public bool TrySpend(float amount)
public void Refill(float amount)
```

Se sincroniza automáticamente con GameBootService para persistencia.

---

## 📍 Sistema de Spawn y Anchors

### PlayerPresetSO - Campo Anchor
```csharp
[Header("Spawn")]
[Tooltip("ID del anchor donde debe aparecer el jugador con este preset")]
public string spawnAnchorId = "Bedroom";
```

### SpawnManager.cs
```csharp
public static string CurrentAnchorId { get; }
public static event Action<string> OnAnchorChanged;

public static void SetCurrentAnchor(string id)
public static void PlaceAtAnchor(GameObject player, string anchorId, bool immediate = true)
```

**Inicialización:**
- Espera a GameBootService antes de inicializar
- Lee anchor por defecto del GameBootProfile

### WorldBootstrap.cs
Inicialización del mundo:
1. Espera a `GameBootService.IsReady()`
2. Aplica preset o carga save
3. Establece anchor y coloca jugador

---

## 🎯 Sistema de Interacciones

### Interactable.cs
```csharp
public enum Mode { OpenDialogue, HandOffToTarget }

[Header("Modo")]
[SerializeField] private Mode mode = Mode.OpenDialogue;

[Header("Abrir diálogo")]
[SerializeField] private DialogueAsset dialogue;

[Header("Ceder control")]
[SerializeField] private MonoBehaviour sessionTarget; // IInteractionSession
```

**Controles:**
- **Botón A del mando** - Interacción principal
- **Input System** - Compatible con nuevo sistema de Unity

### InteractionDetector.cs
Detecta objetos interactuables cerca del jugador.

### Scripts de Mundo

#### SavePoint.cs
```csharp
[Header("Config")]
public string anchorIdToSet;
public bool healOnSave = true;
public KeyCode interactKey = KeyCode.E;
```
- Cura al jugador via PlayerHealthSystem + ManaPool
- Guarda usando `GameBootService.GetProfile().SaveCurrentGameState()`

#### PortalTrigger.cs
```csharp
public string targetAnchorId;
public string requiredFlag;      // Flag requerida para usar
public string setFlagOnEnter;    // Flag a establecer al entrar
```
- Verifica flags en `GameBootService.GetProfile().preset.flags`
- Teletransporta usando `TeleportService`

#### AnchorSetter.cs
```csharp
public string anchorId;
```
- Establece anchor actual via `SpawnManager.SetCurrentAnchor()`
- Usa GameBootService para verificaciones

---

## 🖼️ Sistema de UI

### PlayerAbilitiesUI.cs
Muestra habilidades y hechizos desbloqueados:
```csharp
[Header("Referencias UI - Habilidades")]
[SerializeField] private Transform abilitiesContainer;
[SerializeField] private GameObject abilityUIPrefab;

[Header("Referencias UI - Información")]
[SerializeField] private TextMeshProUGUI levelText;
[SerializeField] private TextMeshProUGUI manaText;

[Header("Configuración")]
[SerializeField] private bool autoRefresh = true;
[SerializeField] private float refreshInterval = 1f;
```

**Características:**
- Espera a GameBootService antes de inicializar
- Se actualiza automáticamente desde GameBootService
- Refresh por intervalo configurable
- Muestra nivel, vida, maná, habilidades y hechizos
- Usa `DestroyImmediate()` para limpiar UI

**Patrón de inicialización:**
```csharp
private IEnumerator DelayedInitialization()
{
    while (!GameBootService.IsReady())
        yield return new WaitForSeconds(0.1f);
    
    FindPlayerComponents();
    RefreshAll();
    
    if (autoRefresh)
        InvokeRepeating(nameof(RefreshAll), refreshInterval, refreshInterval);
}
```

### PlayerHealthUI.cs
UI específica para barra de vida:
- Se conecta automáticamente con PlayerHealthSystem
- Actualización en tiempo real via eventos

---

## 🌊 Sistema de Flotación

### PlayerWaterFloat.cs
Sistema para flotar en agua:
```csharp
[Header("Configuración de Flotación")]
[SerializeField] private float buoyancyForce = 15f;
[SerializeField] private float waterDrag = 2f;
[SerializeField] private LayerMask waterLayerMask = -1;
```

**Funcionamiento:**
- Detecta entrada/salida del agua via Triggers
- Aplica fuerza de flotación basada en profundidad
- Cambia propiedades físicas (drag, angular drag)
- Sistema de debug con Gizmos

---

## 💾 Sistema de Save/Load

### PlayerSaveData.cs
```csharp
[Serializable]
public class PlayerSaveData
{
    public string lastSpawnAnchorId;
    public int level;
    public float maxHp, currentHp;
    public float maxMp, currentMp;
    public List<AbilityId> abilities;
    public List<SpellId> spells;
    public List<string> flags;
    
    // Métodos actualizados para GameBootService
    public static PlayerSaveData FromGameBootProfile()
    public void ApplyToGameBootProfile()
}
```

**Métodos actualizados:**
- `FromGameBootProfile()` - Usa `GameBootService.GetProfile()`
- `ApplyToGameBootProfile()` - Aplica datos al servicio
- Métodos obsoletos marcados con `[System.Obsolete]`

### Flujo de Save/Load

**Save:**
1. `GameBootService.GetProfile().SaveCurrentGameState()` → 
2. `UpdateRuntimePresetFromCurrentState()` → 
3. Obtiene datos de PlayerHealthSystem + ManaPool →
4. `BuildSaveDataFromProfile()` → 
5. `SaveSystem.Save()`

**Load:**
1. `SaveSystem.Load()` →
2. `GameBootProfile.SetRuntimePresetFromSave()` →
3. PlayerHealthSystem lee desde GameBootService →
4. ManaPool se sincroniza automáticamente

---

## 📚 Guías de Uso

### Configuración Inicial del Juego

1. **Crear GameBootService en escena start:**
   ```csharp
   // Crear GameObject en escena start
   // Añadir GameBootService component
   // Asignar GameBootProfile SO en inspector
   ```

2. **Crear GameBootProfile SO:**
   ```csharp
   Create → Game → Boot Profile
   ```
   - Configurar `defaultPlayerPreset`
   - Establecer `defaultAnchorId`
   - **NO colocar en Resources** (se asigna en GameBootService)

3. **Configurar PlayerPresetSO:**
   ```csharp
   Create → Game → Player Preset
   ```
   - Establecer `spawnAnchorId`
   - Configurar stats iniciales
   - Definir habilidades/hechizos

4. **Configurar LocalizationManager:**
   - Crear GameObject con LocalizationManager
   - Colocar archivos JSON en `Resources/Localization/`

### Patrón de Script que usa GameBootService

```csharp
public class MyScript : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        // Esperar hasta que GameBootService esté disponible
        while (!GameBootService.IsReady())
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        InitializeMyScript();
    }
    
    private void InitializeMyScript()
    {
        var bootProfile = GameBootService.GetProfile();
        if (bootProfile == null) return;
        
        var preset = bootProfile.GetActivePresetResolved();
        // ... tu lógica aquí
    }
}
```

### Agregando Nuevos Textos Localizados

1. **Para UI:**
   ```csharp
   // En Identifiers.cs
   public enum UITextId 
   {
       MyNewButton,  // Agregar aquí
   }
   ```

2. **En archivos JSON:**
   ```json
   {
     "texts": [
       {
         "key": "MyNewButton",
         "value": "Mi Nuevo Botón"
       }
     ]
   }
   ```

3. **En UI:**
   ```csharp
   // Agregar LocalizedUI component
   textId = UITextId.MyNewButton
   fallbackText = "Fallback text"
   ```

### Creando Nuevas Interacciones

1. **Diálogo Simple:**
   ```csharp
   // En el objeto
   Interactable component:
   - Mode = OpenDialogue
   - dialogue = tu DialogueAsset
   ```

2. **Interacción Personalizada:**
   ```csharp
   // Crear script que implemente IInteractionSession
   public class MyCustomInteraction : MonoBehaviour, IInteractionSession
   {
       public void BeginSession(GameObject player, System.Action onComplete)
       {
           // Tu lógica aquí
           onComplete?.Invoke();
       }
   }
   
   // En Interactable:
   - Mode = HandOffToTarget  
   - sessionTarget = MyCustomInteraction component
   ```

### Sistema de Flags

```csharp
// Verificar flag
var preset = GameBootService.GetProfile()?.GetActivePresetResolved();
bool hasFlag = preset?.flags?.Contains("myFlag") ?? false;

// Establecer flag
var bootProfile = GameBootService.GetProfile();
var preset = bootProfile?.GetActivePresetResolved();
if (preset != null)
{
    if (preset.flags == null) preset.flags = new List<string>();
    if (!preset.flags.Contains("myFlag"))
        preset.flags.Add("myFlag");
}
```

### Debugging y Testing

**LocalizationTester:**
- Tecla Q para español
- Tecla W para inglés
- Tecla E para alternar
- Tecla U para ayuda
- Métodos públicos para UI de settings

**GameBootService Debug:**
```csharp
var bootProfile = GameBootService.GetProfile();
Debug.Log($"Preset activo: {bootProfile?.GetActivePresetResolved()?.name}");
Debug.Log($"Anchor actual: {SpawnManager.CurrentAnchorId}");
Debug.Log($"Servicio listo: {GameBootService.IsReady()}");
```

**PlayerHealthSystem Testing:**
```csharp
playerHealth.TestDamage(25f);
playerHealth.TestHeal(50f);
playerHealth.SetGodMode(true);
```

### Flujo de Desarrollo Recomendado

1. **Setup inicial:** GameBootService en start + GameBootProfile + PlayerPreset + LocalizationManager
2. **Configurar anchors:** SpawnAnchor objects en el mundo
3. **UI localizada:** Usar LocalizedUI en todos los textos
4. **Interacciones:** Interactable + DialogueAssets o IInteractionSession
5. **Save points:** SavePoint objects para guardar progreso
6. **Testing:** LocalizationTester + debug keys

---

## 🚀 Características del Sistema

### ✅ **Ventajas:**
- **Centralizado** - GameBootService gestiona todo desde escena start
- **Modular** - Cada sistema es independiente pero sincronizado
- **Persistente** - Save/Load automático integrado
- **Localizable** - Sistema completo multiidioma
- **Flexible** - Fácil testing con presets
- **Robusto** - Manejo de errores y fallbacks
- **Explícito** - No singleton automático, control manual del servicio

### 🎯 **Casos de Uso:**
- **RPG/Adventure** - Sistema completo de progresión
- **Narrativa** - Diálogos y textos localizados
- **Testing** - Presets para diferentes estados del juego
- **Multiidioma** - Soporte completo de localización

### 🔧 **Mantenimiento:**
- **Logs claros** - Cada sistema reporta su estado
- **Debugging** - Tools integradas para testing
- **Modular** - Fácil modificar sistemas independientes
- **Documentado** - Código auto-explicativo con comentarios
- **Sin singleton mágico** - GameBootService explícito y controlado

### 📋 **Diferencias vs Versión Anterior:**
- ❌ **Eliminado**: `GameBootProfile.Instance` (singleton automático)
- ❌ **Eliminado**: Carga automática desde `Resources/GameBootProfile`
- ❌ **Eliminado**: PlayerState (simplificado)
- ✅ **Nuevo**: GameBootService en escena start
- ✅ **Nuevo**: Patrón `GameBootService.IsReady()` + `GetProfile()`
- ✅ **Nuevo**: Inicialización explícita con corrutinas DelayedInitialization
- ✅ **Mejorado**: SubtitleController con sincronización de LocalizationManager
- ✅ **Mejorado**: LocalizationTester con nuevo Input System

---

*Documentación actualizada: Diciembre 2024 - Versión GameBootService*
