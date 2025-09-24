using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de salud específico para el jugador que se integra con PlayerState
/// </summary>
[RequireComponent(typeof(PlayerState), typeof(Animator))]
public class PlayerHealthSystem : MonoBehaviour
{
    [Header("Configuración de Daño")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [Tooltip("Si está activo, el jugador no puede morir (útil para testing)")]
    [SerializeField] private bool godMode = false;
    
    [Header("Animaciones")]
    [SerializeField] private string damageAnimationName = "TakeDamage";
    [SerializeField] private string deathAnimationName = "Death";
    [SerializeField] private string healAnimationName = "Heal";
    [SerializeField] private int upperBodyLayer = 1; // Layer para animaciones del torso superior
    
    [Header("Efectos Visuales")]
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private GameObject damageVFX;
    [SerializeField] private GameObject healVFX;
    
    [Header("Invulnerabilidad Visual")]
    [SerializeField] private float invulnerabilityFlashRate = 0.1f;
    
    [Header("Camera Shake (opcional)")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.3f;
    
    [Header("Knockback (opcional)")]
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private AnimationCurve knockbackCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Eventos")]
    public UnityEvent<float> OnHealthChanged; // healthPercentage (0-1)
    public UnityEvent<float, float> OnDamageTaken; // (damageAmount, currentHealth)
    public UnityEvent<float, float> OnHealed; // (healAmount, currentHealth)
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerRevived;
    
    // Componentes
    private PlayerState _playerState;
    private Animator _animator;
    private AudioSource _audioSource;
    private Renderer[] _renderers;
    private Material[] _originalMaterials;
    
    // Estado
    private bool _isInvulnerable = false;
    private bool _isDead = false;
    private float _invulnerableUntil = -999f;
    
    // Corrutinas
    private Coroutine _invulnerabilityFlashCoroutine;
    private Coroutine _damageFlashCoroutine;
    
    // Propiedades públicas
    public bool IsAlive => _playerState.CurrentHp > 0 && !_isDead;
    public bool IsInvulnerable => _isInvulnerable || Time.time < _invulnerableUntil;
    public float CurrentHealth => _playerState.CurrentHp;
    public float MaxHealth => _playerState.MaxHp;
    public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
    public bool IsGodModeActive => godMode;
    
    // Eventos C#
    public event Action<float> OnHealthPercentageChanged;
    public event Action<float> OnDamageReceived;
    public event Action OnDied;
    
    void Awake()
    {
        _playerState = GetComponent<PlayerState>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        
        // Si no hay AudioSource, creamos uno
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }
        
        // Obtener renderers para efectos visuales
        _renderers = GetComponentsInChildren<Renderer>();
        CacheOriginalMaterials();
    }
    
    void Start()
    {
        // Usar StartCoroutine para esperar un frame y asegurar que PlayerState se haya inicializado
        StartCoroutine(DelayedInitialization());
    }
    
    private IEnumerator DelayedInitialization()
    {
        // Esperar hasta el final del frame para asegurar que todos los componentes se han inicializado
        yield return new WaitForEndOfFrame();
        
        // Suscribirse a cambios en PlayerState
        if (_playerState != null)
        {
            _playerState.OnStatsChanged += HandleStatsChanged;
            
            // Verificar si PlayerState tiene datos válidos, si no, esperar un poco más
            int maxAttempts = 10;
            int attempts = 0;
            
            while ((_playerState.MaxHp <= 1f || _playerState.CurrentHp <= 0f) && attempts < maxAttempts)
            {
                Debug.Log($"[PlayerHealthSystem] Esperando inicialización de PlayerState... Intento {attempts + 1}");
                yield return new WaitForSeconds(0.1f);
                attempts++;
            }
            
            // Inicializar el estado basado en PlayerState actual
            InitializeFromPlayerState();
        }
        
        // Notificar UI inicial después de la inicialización
        UpdateUI();
    }
    
    void OnDestroy()
    {
        if (_playerState != null)
        {
            _playerState.OnStatsChanged -= HandleStatsChanged;
        }
    }
    
    private void CacheOriginalMaterials()
    {
        if (_renderers == null) return;
        
        _originalMaterials = new Material[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                // Crear una instancia del material para evitar modificar el material compartido
                _originalMaterials[i] = new Material(_renderers[i].material);
                _renderers[i].material = _originalMaterials[i];
            }
        }
        
        Debug.Log($"[PlayerHealthSystem] Materiales originales cacheados para {_originalMaterials.Length} renderers");
    }
    
    private void InitializeFromPlayerState()
    {
        if (_playerState == null) return;
        
        // Establecer el estado inicial basado en PlayerState SIN disparar eventos de cambio
        _isDead = _playerState.CurrentHp <= 0;
        
        Debug.Log($"[PlayerHealthSystem] Inicializado con vida: {_playerState.CurrentHp:0.1f}/{_playerState.MaxHp:0.1f} ({HealthPercentage:P1}) - Estado: {(_isDead ? "MUERTO" : "VIVO")}");
    }
    
    private void HandleStatsChanged()
    {
        if (_playerState == null) return;
        
        // Solo procesar cambios de muerte/vida si realmente cambió el estado
        bool shouldBeDead = _playerState.CurrentHp <= 0;
        
        // IMPORTANTE: Solo disparar eventos si hay un cambio REAL de estado
        if (shouldBeDead && !_isDead)
        {
            Die();
        }
        else if (!shouldBeDead && _isDead)
        {
            Revive();
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// Aplica daño al jugador
    /// </summary>
    public bool TakeDamage(float damageAmount)
    {
        if (!IsAlive || damageAmount <= 0f || godMode) return false;
        if (IsInvulnerable) return false;
        
        float oldHealth = _playerState.CurrentHp;
        float newHealth = Mathf.Max(0f, oldHealth - damageAmount);
        
        // Aplicar el daño al PlayerState
        _playerState.SetHealth(newHealth);
        
        // Activar invulnerabilidad
        StartInvulnerability();
        
        // IMPORTANTE: Ejecutar animación INMEDIATAMENTE, antes que otros efectos
        TriggerAnimation(damageAnimationName);
        
        // Efectos visuales y sonoros (inmediatos)
        PlayDamageEffects();
        
        // Aplicar knockback DESPUÉS de la animación (con un pequeño delay)
        if (enableKnockback)
        {
            StartCoroutine(DelayedKnockback());
        }
        
        // Notificar eventos
        OnDamageTaken?.Invoke(damageAmount, newHealth);
        OnDamageReceived?.Invoke(damageAmount);
        
        Debug.Log($"[PlayerHealth] Jugador recibió {damageAmount} daño. Vida: {newHealth}/{MaxHealth}");
        
        return true;
    }
    
    private IEnumerator DelayedKnockback()
    {
        // Pequeño delay para que la animación empiece primero
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(ApplyKnockback());
    }
    
    /// <summary>
    /// Cura al jugador
    /// </summary>
    public bool Heal(float healAmount)
    {
        if (healAmount <= 0f) return false;
        if (_playerState.CurrentHp >= _playerState.MaxHp) return false;
        
        float oldHealth = _playerState.CurrentHp;
        float newHealth = Mathf.Min(_playerState.MaxHp, oldHealth + healAmount);
        float actualHeal = newHealth - oldHealth;
        
        if (actualHeal <= 0f) return false;
        
        // Aplicar curación (esto activará HandleStatsChanged que detectará si revivió)
        _playerState.SetHealth(newHealth);
        
        // Efectos solo si el jugador está vivo DESPUÉS de la curación
        if (_playerState.CurrentHp > 0)
        {
            PlayHealEffects();
            TriggerAnimation(healAnimationName);
        }
        
        // Notificar eventos siempre
        OnHealed?.Invoke(actualHeal, newHealth);
        
        Debug.Log($"[PlayerHealth] Jugador curado {actualHeal}. Vida: {newHealth}/{MaxHealth} - Estado: {(newHealth > 0 ? "VIVO" : "MUERTO")}");
        
        return true;
    }
    
    /// <summary>
    /// Mata instantáneamente al jugador
    /// </summary>
    public void Kill()
    {
        if (!IsAlive || godMode) return;
        
        _playerState.SetHealth(0f);
        // HandleStatsChanged se encargará de llamar Die()
    }
    
    /// <summary>
    /// Revive al jugador con vida completa
    /// </summary>
    public void Revive(float healthPercentage = 1f)
    {
        healthPercentage = Mathf.Clamp01(healthPercentage);
        float newHealth = _playerState.MaxHp * healthPercentage;
        
        _playerState.SetHealth(newHealth);
        // HandleStatsChanged se encargará de llamar Revive()
    }
    
    private void Die()
    {
        if (_isDead) return;
        
        _isDead = true;
        
        // Efectos
        TriggerAnimation(deathAnimationName);
        PlaySound(deathSound);
        
        // Notificar eventos
        OnPlayerDeath?.Invoke();
        OnDied?.Invoke();
        
        Debug.Log("[PlayerHealth] ¡El jugador ha muerto!");
    }
    
    private void Revive()
    {
        if (!_isDead) return;
        
        _isDead = false;
        
        // Notificar eventos
        OnPlayerRevived?.Invoke();
        
        Debug.Log("[PlayerHealth] ¡El jugador ha revivido!");
    }
    
    private void StartInvulnerability()
    {
        _invulnerableUntil = Time.time + invulnerabilityDuration;
        
        if (invulnerabilityDuration > 0f)
        {
            StartInvulnerabilityFlash();
        }
    }
    
    private void PlayDamageEffects()
    {
        StartDamageFlash();
        SpawnVFX(damageVFX);
        PlaySound(damageSound);
        
        if (enableCameraShake)
        {
            StartCoroutine(CameraShake());
        }
    }
    
    private void PlayHealEffects()
    {
        SpawnVFX(healVFX);
        PlaySound(healSound);
    }
    
    private void StartDamageFlash()
    {
        if (_damageFlashCoroutine != null)
            StopCoroutine(_damageFlashCoroutine);
        _damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }
    
    private IEnumerator DamageFlashCoroutine()
    {
        SetRenderersColor(damageFlashColor);
        yield return new WaitForSeconds(damageFlashDuration);
        RestoreOriginalMaterials();
    }
    
    private void StartInvulnerabilityFlash()
    {
        if (_invulnerabilityFlashCoroutine != null)
            StopCoroutine(_invulnerabilityFlashCoroutine);
        _invulnerabilityFlashCoroutine = StartCoroutine(InvulnerabilityFlashCoroutine());
    }
    
    private IEnumerator InvulnerabilityFlashCoroutine()
    {
        while (Time.time < _invulnerableUntil)
        {
            SetRenderersVisibility(false);
            yield return new WaitForSeconds(invulnerabilityFlashRate);
            SetRenderersVisibility(true);
            yield return new WaitForSeconds(invulnerabilityFlashRate);
        }
        SetRenderersVisibility(true);
    }
    
    private void SetRenderersColor(Color color)
    {
        if (_renderers == null || _originalMaterials == null) return;
        
        for (int i = 0; i < _renderers.Length && i < _originalMaterials.Length; i++)
        {
            if (_renderers[i] != null && _originalMaterials[i] != null)
            {
                _renderers[i].material.color = color;
            }
        }
    }
    
    private void SetRenderersVisibility(bool visible)
    {
        if (_renderers == null) return;
        
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }
    
    private void RestoreOriginalMaterials()
    {
        if (_renderers == null || _originalMaterials == null) return;
        
        for (int i = 0; i < _renderers.Length && i < _originalMaterials.Length; i++)
        {
            if (_renderers[i] != null && _originalMaterials[i] != null)
            {
                // Restaurar el color original del material
                _renderers[i].material.color = _originalMaterials[i].color;
            }
        }
        
        Debug.Log("[PlayerHealthSystem] Materiales restaurados al color original");
    }
    
    private void SpawnVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab != null)
        {
            var vfx = Instantiate(vfxPrefab, transform.position, transform.rotation);
            Destroy(vfx, 3f);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }
    
    private void TriggerAnimation(string animationName)
    {
        if (_animator != null && !string.IsNullOrEmpty(animationName))
        {
            // Asegurar que el layer esté activo
            if (upperBodyLayer > 0)
            {
                _animator.SetLayerWeight(upperBodyLayer, 1f);
            }
            
            // Reproducir la animación directamente por nombre
            _animator.Play(animationName);
            
            Debug.Log($"[PlayerHealthSystem] Reproduciendo animación: {animationName} en layer {upperBodyLayer}");
        }
        else
        {
            Debug.LogWarning($"[PlayerHealthSystem] No se puede reproducir animación - Animator: {(_animator != null ? "OK" : "NULL")}, AnimationName: '{animationName}'");
        }
    }
    
    private void UpdateUI()
    {
        float healthPercentage = HealthPercentage;
        OnHealthChanged?.Invoke(healthPercentage);
        OnHealthPercentageChanged?.Invoke(healthPercentage);
    }
    
    private IEnumerator CameraShake()
    {
        Vector3 originalPosition = transform.localPosition;
        
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeIntensity;
            transform.localPosition = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localPosition = originalPosition;
    }
    
    private IEnumerator ApplyKnockback()
    {
        Vector3 knockbackDirection = -transform.forward; // Empujar en la dirección opuesta a donde mira el jugador
        float elapsed = 0f;
        
        while (elapsed < knockbackDuration)
        {
            float t = elapsed / knockbackDuration;
            float curveValue = knockbackCurve.Evaluate(t);
            
            // Aplicar fuerza de empuje
            transform.position += knockbackDirection * (knockbackForce * curveValue * Time.deltaTime);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    // Métodos públicos para configuración
    public void SetGodMode(bool enabled)
    {
        godMode = enabled;
        Debug.Log($"[PlayerHealth] God Mode: {(enabled ? "ACTIVADO" : "DESACTIVADO")}");
    }
    
    public void SetInvulnerabilityDuration(float duration)
    {
        invulnerabilityDuration = Mathf.Max(0f, duration);
    }
    
    // Métodos de testing
    public void TestDamage(float amount)
    {
        TakeDamage(amount);
    }
    
    public void TestHeal(float amount)
    {
        Heal(amount);
    }
    
    // ===== MÉTODOS PARA ANIMATION EVENTS =====
    // Estos métodos son llamados por Animation Events en las animaciones
    
    /// <summary>
    /// Llamado por Animation Event cuando se ejecuta un hechizo/magia
    /// </summary>
    public void OnMagicExecute()
    {
        Debug.Log("[PlayerHealthSystem] Animation Event: OnMagicExecute - Ignorando para sistema de salud");
        // NO hacer nada aquí para evitar interferencias con el sistema de salud
    }
    
    /// <summary>
    /// Llamado por Animation Event cuando termina el cast de magia
    /// </summary>
    public void OnMagicCastEnd()
    {
        Debug.Log("[PlayerHealthSystem] Animation Event: OnMagicCastEnd - Ignorando para sistema de salud");
        // NO hacer nada aquí para evitar interferencias con el sistema de salud
    }
    
    /// <summary>
    /// Llamado por Animation Event para efectos de daño (opcional)
    /// </summary>
    public void OnDamageAnimationComplete()
    {
        Debug.Log("[PlayerHealthSystem] Animation Event: OnDamageAnimationComplete");
        // Este método SÍ puede tener lógica útil cuando termina la animación de daño
    }
}
