using UnityEngine;

public class LoadScene : MonoBehaviour
{
    public string sceneName;
    public bool loadOnStart = false;

    [Header("Fade override (opcional)")]
    public EasyTransition.TransitionSettings fadeOverride;
    [Min(0)] public float fadeDelay = 0f;

    void Start()
    {
        if (loadOnStart && !string.IsNullOrEmpty(sceneName)) LoadTargetScene();
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("LoadScene: No scene name specified!"); return; }

        if (fadeOverride != null)
            SceneTransitionLoader.Load(sceneName, fadeOverride, fadeDelay);
        else
            SceneTransitionLoader.Load(sceneName); // usa el default del servicio
    }
}