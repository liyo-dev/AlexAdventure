using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    [SerializeField] string nextSceneName = "CityHub";
    [SerializeField] string playerTag = "Player";
    [SerializeField] bool async = false;

    void Reset() { var c = GetComponent<Collider>(); c.isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (async) SceneManager.LoadSceneAsync(nextSceneName);
        else SceneManager.LoadScene(nextSceneName);
    }
}