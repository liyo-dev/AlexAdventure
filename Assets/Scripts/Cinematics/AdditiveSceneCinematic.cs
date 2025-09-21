using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;

public class AdditiveSceneCinematic : MonoBehaviour
{
    [Header("Cinematic Scene")]
    [SerializeField] private string cinematicSceneName = "";   // usado en runtime

#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset cinematicSceneAsset; // solo para drag&drop en editor
    void OnValidate()
    {
        if (cinematicSceneAsset != null)
            cinematicSceneName = cinematicSceneAsset.name; // copiamos el nombre real de la escena
    }
#endif

    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool directorInAdditive = true;

    [Header("Gameplay Lock")]
    [SerializeField] private GameObject[] toDisableDuringCinematic;

    AsyncOperation loadOp;
    PlayableDirector director;

    IEnumerator Start()
    {
        if (!playOnStart) yield break;
        yield return Play();
    }

    public IEnumerator Play()
    {
        if (string.IsNullOrEmpty(cinematicSceneName))
        {
            Debug.LogWarning("[AdditiveSceneCinematic] No hay escena asignada.");
            yield break;
        }

        foreach (var go in toDisableDuringCinematic) if (go) go.SetActive(false);

        loadOp = SceneManager.LoadSceneAsync(cinematicSceneName, LoadSceneMode.Additive);
        yield return loadOp;

        if (directorInAdditive)
        {
            var scn = SceneManager.GetSceneByName(cinematicSceneName);
            foreach (var root in scn.GetRootGameObjects())
            {
                director = director ?? root.GetComponentInChildren<PlayableDirector>(true);
                if (director) break;
            }
        }
        else
        {
            director = GetComponentInChildren<PlayableDirector>(true);
        }

        if (director)
        {
            director.stopped += OnDirectorStopped;
            director.Play();
        }
        else
        {
            Debug.LogWarning("[AdditiveSceneCinematic] No se encontró PlayableDirector; descargando escena.");
            yield return Unload();
        }
    }

    void OnDirectorStopped(PlayableDirector d)
    {
        d.stopped -= OnDirectorStopped;
        StartCoroutine(Unload());
    }

    IEnumerator Unload()
    {
        var scn = SceneManager.GetSceneByName(cinematicSceneName);
        if (scn.isLoaded)
            yield return SceneManager.UnloadSceneAsync(scn);

        foreach (var go in toDisableDuringCinematic) if (go) go.SetActive(true);
    }
}
