using UnityEngine;

public class BossArenaController : MonoBehaviour
{
    [Header("Puertas")]
    public DoorGate doorWest;   // Door_W/Barrier
    public DoorGate doorEast;   // Door_E/Barrier

    [Header("Spawn")]
    public Transform bossSpawn;
    public GameObject bossPrefab;   // tu boss
    public Transform portalSpawn;
    public GameObject portalPrefab; // PF_PortalExit

    [Header("Refs")]
    public RoomGoal roomGoal;
    public string playerTag = "Player";
    public AudioSource musicBoss;    // opcional

    bool started = false;

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (started) return;
        if (!other.CompareTag(playerTag)) return;

        started = true;

        // Cierra la puerta de entrada para bloquear la arena
        if (doorWest) doorWest.Close();
        if (musicBoss) musicBoss.Play();

        GameObject boss = null;

        if (bossPrefab && bossSpawn)
            boss = Instantiate(bossPrefab, bossSpawn.position, bossSpawn.rotation, transform.parent);
        else
            boss = FindExistingBossInRoom(); // por si ya lo dejaste colocado

        if (!boss)
        {
            Debug.LogError("[BossArenaController] No hay boss para esta sala.");
            return;
        }

        // Asegura un EnemyMarker para detectar la muerte (se dispara en OnDestroy)
        var marker = boss.GetComponent<EnemyMarker>();
        if (!marker) marker = boss.AddComponent<EnemyMarker>();
        marker.onEnemyGone += OnBossDead;
    }

    GameObject FindExistingBossInRoom()
    {
        // busca un EnemyMarker ya colocado como hijo de la sala
        var markers = GetComponentsInParent<RoomGoal>(true);
        if (markers.Length > 0)
        {
            var roomRoot = markers[0].transform;
            var existing = roomRoot.GetComponentInChildren<EnemyMarker>(true);
            return existing ? existing.gameObject : null;
        }
        return null;
    }

    void OnBossDead(EnemyMarker _)
    {
        // Abre salida y marca la sala como completada
        if (doorEast) doorEast.Open();
        if (roomGoal) roomGoal.MarkCleared();

        // Crea el portal de salida
        if (portalPrefab && portalSpawn)
            Instantiate(portalPrefab, portalSpawn.position, portalSpawn.rotation, transform.parent);

        // Limpieza del evento
        _.onEnemyGone -= OnBossDead;
    }
}
