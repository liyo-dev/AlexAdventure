using UnityEngine;
using UnityEngine.SceneManagement;
using EasyTransition;
using System.Collections;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class SceneTransitionLoader : MonoBehaviour
{
    private static SceneTransitionLoader _inst;
    public static SceneTransitionLoader Instance => _inst;

    [Header("Valores por defecto")]
    public TransitionSettings defaultSettings;     // ← asigna tu Fade.asset aquí
    [Min(0)] public float defaultDelay = 0f;
    [Min(0)] public float waitForManagerSec = 0.5f;

    void Awake()
    {
        if (_inst == null)
        {
            _inst = this;
            DontDestroyOnLoad(gameObject);
            return;
        }
        if (_inst != this)
        {
            // Prefiere la instancia que tenga defaultSettings asignado
            bool prevOk = _inst.defaultSettings != null;
            bool thisOk = this.defaultSettings != null;
            if (!prevOk && thisOk)
            {
                Destroy(_inst.gameObject);
                _inst = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }
    }

    // APIs públicas
    public static void Load(string sceneName)
    {
        if (_inst) _inst.StartCoroutine(_inst.LoadRoutine(sceneName, _inst.defaultSettings, _inst.defaultDelay));
        else SceneManager.LoadScene(sceneName);
    }

    public static void Load(string sceneName, TransitionSettings settings, float delay = 0f)
    {
        if (_inst) _inst.StartCoroutine(_inst.LoadRoutine(sceneName, settings ?? _inst.defaultSettings, delay));
        else SceneManager.LoadScene(sceneName);
    }

    // Núcleo: transición SIN cambio de escena + carga en el "cut"
    IEnumerator LoadRoutine(string sceneName, TransitionSettings settings, float delay)
    {
        // Si seguimos sin settings, no llames al plugin (evita su error)
        if (settings == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // Espera a que exista el TransitionManager (Start entra aditivo)
        TransitionManager tm = null;
        float t = 0f;
        while (t < waitForManagerSec && (tm = FindTM()) == null)
        {
            yield return null;
            t += Time.unscaledDeltaTime;
        }
        if (tm == null) tm = FindTM();

        if (tm == null)
        {
            SceneManager.LoadScene(sceneName); // fallback seguro
            yield break;
        }

        // Suscriptores locales y auto-desuscripción
        UnityAction onCut = null;
        UnityAction onEnd = null;

        onCut = () =>
        {
            // Momento negro: cambiamos la escena
            SceneManager.LoadScene(sceneName);
            tm.onTransitionCutPointReached -= onCut;
        };

        onEnd = () =>
        {
            tm.onTransitionEnd -= onEnd;
            // aquí podrías volver a habilitar input si lo desactivaste al empezar
        };

        tm.onTransitionCutPointReached += onCut;
        tm.onTransitionEnd            += onEnd;

        // Importante: usamos la VERSIÓN SIN CAMBIO DE ESCENA del plugin.
        // Esa ruta sí pone runningTransition = false al terminar.
        tm.Transition(settings, delay);
    }

    static TransitionManager FindTM()
    {
#if UNITY_2022_3_OR_NEWER
        return Object.FindFirstObjectByType<TransitionManager>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<TransitionManager>(true);
#endif
    }
}
