using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Referencias UI - Image Health Bar")]
    [SerializeField] private Image healthBarFill; // La Image principal que se llena/vacía
    [SerializeField] private Image healthBarBackground; // Imagen de fondo (opcional)
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private CanvasGroup healthBarCanvasGroup;
    
    [Header("Colores de la Barra")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float warningThreshold = 0.5f;
    [SerializeField] private float criticalThreshold = 0.25f;
    
    [Header("Animaciones")]
    [SerializeField] private bool animateHealthChanges = true;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private bool showDamageFlash = true;
    [SerializeField] private float damageFlashDuration = 0.3f;
    
    [Header("Configuración")]
    [SerializeField] private bool showHealthNumbers = true;
    [SerializeField] private bool autoHide = false;
    [SerializeField] private float autoHideDelay = 3f;
    
    private PlayerHealthSystem _playerHealthSystem;
    private float _targetFillAmount = 1f;
    private float _currentFillAmount = 1f;
    private Coroutine _animationCoroutine;
    private Coroutine _autoHideCoroutine;
    private Coroutine _damageFlashCoroutine;
    
    void Start()
    {
        FindPlayerHealthSystem();
        InitializeUI();
        
        if (autoHide)
            ShowHealthBar();
    }
    
    void OnDestroy()
    {
        if (_playerHealthSystem != null)
        {
            _playerHealthSystem.OnHealthChanged.RemoveListener(UpdateHealthBar);
            _playerHealthSystem.OnDamageTaken.RemoveListener(OnDamageTaken);
        }
    }
    
    private void FindPlayerHealthSystem()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealthSystem = player.GetComponent<PlayerHealthSystem>();
        }
        
        if (_playerHealthSystem == null)
        {
            _playerHealthSystem = FindObjectOfType<PlayerHealthSystem>();
        }
        
        if (_playerHealthSystem != null)
        {
            _playerHealthSystem.OnHealthChanged.AddListener(UpdateHealthBar);
            _playerHealthSystem.OnDamageTaken.AddListener(OnDamageTaken);
            
            Debug.Log("[PlayerHealthUI] Conectado al sistema de salud del jugador");
        }
        else
        {
            Debug.LogWarning("[PlayerHealthUI] No se encontró PlayerHealthSystem");
        }
    }
    
    private void InitializeUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.type = Image.Type.Filled;
            healthBarFill.fillMethod = Image.FillMethod.Horizontal;
            healthBarFill.fillAmount = 1f;
            healthBarFill.color = healthyColor;
        }
        
        UpdateHealthDisplay(1f, 100f, 100f);
    }
    
    public void UpdateHealthBar(float healthRatio)
    {
        _targetFillAmount = Mathf.Clamp01(healthRatio);
        
        float currentHealth = _playerHealthSystem != null ? _playerHealthSystem.CurrentHealth : 0f;
        float maxHealth = _playerHealthSystem != null ? _playerHealthSystem.MaxHealth : 100f;
        
        if (animateHealthChanges)
        {
            if (_animationCoroutine != null)
                StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimateHealthChange(currentHealth, maxHealth));
        }
        else
        {
            _currentFillAmount = _targetFillAmount;
            UpdateHealthDisplay(_currentFillAmount, currentHealth, maxHealth);
        }
        
        if (autoHide)
        {
            ShowHealthBar();
            RestartAutoHideTimer();
        }
    }
    
    private System.Collections.IEnumerator AnimateHealthChange(float currentHealth, float maxHealth)
    {
        while (Mathf.Abs(_currentFillAmount - _targetFillAmount) > 0.01f)
        {
            _currentFillAmount = Mathf.MoveTowards(_currentFillAmount, _targetFillAmount, 
                                                   animationSpeed * Time.deltaTime);
            
            float displayHealth = _currentFillAmount * maxHealth;
            UpdateHealthDisplay(_currentFillAmount, displayHealth, maxHealth);
            
            yield return null;
        }
        
        _currentFillAmount = _targetFillAmount;
        UpdateHealthDisplay(_currentFillAmount, currentHealth, maxHealth);
    }
    
    private void UpdateHealthDisplay(float fillAmount, float current, float max)
    {
        // Actualizar el fillAmount de la Image
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = fillAmount;
            healthBarFill.color = GetHealthColor(fillAmount);
        }
        
        // Actualizar texto si está configurado
        if (healthText != null && showHealthNumbers)
        {
            healthText.text = $"{current:0}/{max:0}";
        }
    }
    
    private Color GetHealthColor(float fillAmount)
    {
        if (fillAmount <= criticalThreshold)
        {
            return criticalColor;
        }
        else if (fillAmount <= warningThreshold)
        {
            float t = (fillAmount - criticalThreshold) / (warningThreshold - criticalThreshold);
            return Color.Lerp(criticalColor, warningColor, t);
        }
        else
        {
            float t = (fillAmount - warningThreshold) / (1f - warningThreshold);
            return Color.Lerp(warningColor, healthyColor, t);
        }
    }
    
    private void OnDamageTaken(float damageAmount, float currentHealth)
    {
        if (showDamageFlash)
        {
            if (_damageFlashCoroutine != null)
                StopCoroutine(_damageFlashCoroutine);
            _damageFlashCoroutine = StartCoroutine(DamageFlashEffect());
        }
    }
    
    private System.Collections.IEnumerator DamageFlashEffect()
    {
        if (healthBarCanvasGroup != null)
        {
            float originalAlpha = healthBarCanvasGroup.alpha;
            
            healthBarCanvasGroup.alpha = 1.5f;
            yield return new WaitForSeconds(damageFlashDuration * 0.3f);
            
            float elapsed = 0f;
            while (elapsed < damageFlashDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (damageFlashDuration * 0.7f);
                healthBarCanvasGroup.alpha = Mathf.Lerp(1.5f, originalAlpha, t);
                yield return null;
            }
            
            healthBarCanvasGroup.alpha = originalAlpha;
        }
        else if (healthBarFill != null)
        {
            // Si no hay CanvasGroup, hacer flash directo en la barra
            Color originalColor = healthBarFill.color;
            Color flashColor = Color.white;
            
            healthBarFill.color = flashColor;
            yield return new WaitForSeconds(damageFlashDuration * 0.3f);
            
            float elapsed = 0f;
            while (elapsed < damageFlashDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (damageFlashDuration * 0.7f);
                healthBarFill.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }
            
            healthBarFill.color = originalColor;
        }
    }
    
    private void ShowHealthBar()
    {
        if (healthBarCanvasGroup != null)
        {
            healthBarCanvasGroup.alpha = 1f;
            healthBarCanvasGroup.interactable = true;
        }
    }
    
    private void HideHealthBar()
    {
        if (healthBarCanvasGroup != null)
        {
            healthBarCanvasGroup.alpha = 0.3f;
            healthBarCanvasGroup.interactable = false;
        }
    }
    
    private void RestartAutoHideTimer()
    {
        if (_autoHideCoroutine != null)
            StopCoroutine(_autoHideCoroutine);
        _autoHideCoroutine = StartCoroutine(AutoHideTimer());
    }
    
    private System.Collections.IEnumerator AutoHideTimer()
    {
        yield return new WaitForSeconds(autoHideDelay);
        HideHealthBar();
    }
    
    public void ForceUpdate()
    {
        if (_playerHealthSystem != null)
        {
            float ratio = _playerHealthSystem.HealthPercentage;
            UpdateHealthBar(ratio);
        }
    }
}
