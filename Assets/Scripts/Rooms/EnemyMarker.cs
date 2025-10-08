using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    public System.Action<EnemyMarker> onEnemyGone;
    void OnDestroy(){ onEnemyGone?.Invoke(this); }
}