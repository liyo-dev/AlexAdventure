using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class RoomSequenceGenerator : MonoBehaviour
{
    [Header("Catálogo")] public RoomCatalog catalog;
    [Header("Secuencia")] [Range(1, 10)] public int midRooms = 3;
    [Header("Espaciado / Fallback")] public float roomSpacing = 35f;
    [Header("Semilla")] public int seed = 0; public bool randomizeOnPlay = true;
    [Header("Contenedor")] public Transform container;

    [Header("Alineación por puertas")]
    public bool snapByDoors = true;
    public float doorGap = 1.5f;

    [Header("Regenerar")] public bool clearBeforeGenerate = true;

    private Random rng;
    private Transform lastPlaced;

    void Start()
    {
        if (randomizeOnPlay && seed == 0)
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        rng = new Random(seed == 0 ? 1234567 : seed);

        // >>> Diferimos 1 frame para que el Inspector cambie de estado sin romperse
        StartCoroutine(GenerateNextFrame());
    }

    IEnumerator GenerateNextFrame()
    {
        yield return null; // espera 1 frame tras entrar en Play

#if UNITY_EDITOR
        // Si el usuario tenía seleccionado algo dentro del contenedor, deselecciona
        if (container && UnityEditor.Selection.activeTransform &&
            UnityEditor.Selection.activeTransform.IsChildOf(container))
        {
            UnityEditor.Selection.activeObject = null;
        }
#endif
        Generate();
    }

    [ContextMenu("Generate Now")]
    public void Generate()
    {
        if (!catalog || !catalog.startRoom || !catalog.bossRoom)
        {
            Debug.LogError("[RoomSequenceGenerator] Falta catalog/start/boss.");
            return;
        }

        if (!container) container = this.transform;
        if (clearBeforeGenerate) ClearChildren(container);

        lastPlaced = null;
        Vector3 pos = Vector3.zero;

        // plan de dificultades
        var plan = BuildDifficultyPlan(midRooms);

        // START
        var start = Instantiate(catalog.startRoom, pos, Quaternion.identity, container).transform;
        WireDoors(start, openWest: true, openEast: true);
        lastPlaced = start;
        pos += Vector3.right * roomSpacing;

        // MIDS
        foreach (var diff in plan)
        {
            var roomPrefab = PickRoom(diff);
            var room = Instantiate(roomPrefab, pos, Quaternion.identity, container).transform;

            WireDoors(room, openWest: true, openEast: false);

            if (snapByDoors && lastPlaced) SnapAfter(lastPlaced, room, doorGap);
            else room.position = pos;

            var goal = room.GetComponentInChildren<RoomGoal>(true);
            var eastDoor = FindDoorGate(room, "Door_E");
            if (goal && eastDoor) goal.onRoomCleared += () => eastDoor.Open();

            lastPlaced = room;
            pos += Vector3.right * roomSpacing;
        }

        // BOSS
        var boss = Instantiate(catalog.bossRoom, pos, Quaternion.identity, container).transform;
        WireDoors(boss, openWest: true, openEast: false);
        if (snapByDoors && lastPlaced) SnapAfter(lastPlaced, boss, doorGap);
    }

    // --- utilidades (sin cambios de fondo) ---
    List<RoomDifficulty> BuildDifficultyPlan(int n)
    {
        var list = new List<RoomDifficulty>();
        for (int i = 0; i < n; i++)
            list.Add(i == 0 ? RoomDifficulty.Easy : (i < n - 1 ? RoomDifficulty.Medium : RoomDifficulty.Hard));
        return list;
    }

    GameObject PickRoom(RoomDifficulty diff)
    {
        var pool = catalog.rooms.Where(r => r.difficulty == diff).ToList();
        if (pool.Count == 0)
        {
            Debug.LogWarning($"[RoomSequenceGenerator] No hay rooms para {diff}. Uso cualquiera.");
            pool = catalog.rooms;
            if (pool.Count == 0) return catalog.startRoom;
        }
        int i = rng.Next(pool.Count);
        return pool[i].prefab;
    }

    void WireDoors(Transform room, bool openWest, bool openEast)
    {
        var west = FindDoorGate(room, "Door_W");
        var east = FindDoorGate(room, "Door_E");
        if (west) { if (openWest) west.Open(); else west.Close(); }
        if (east) { if (openEast) east.Open(); else east.Close(); }
    }

    DoorGate FindDoorGate(Transform room, string doorName)
    {
        var door = room.Find(doorName);
        if (!door) return null;
        var gate = door.GetComponentInChildren<DoorGate>(true);
        if (!gate)
        {
            var barrier = door.Find("Barrier");
            if (barrier) gate = barrier.GetComponent<DoorGate>();
        }
        return gate;
    }

    Transform FindDoor(Transform room, string doorName) => room ? room.Find(doorName) : null;

    void SnapAfter(Transform prevRoom, Transform newRoom, float gap)
    {
        var prevE = FindDoor(prevRoom, "Door_E");
        var newW  = FindDoor(newRoom,  "Door_W");
        if (!prevE || !newW) return;

        Vector3 targetFwd = prevE.forward; targetFwd.y = 0f;
        if (targetFwd.sqrMagnitude < 0.0001f) targetFwd = Vector3.right;

        Vector3 curFwd = newW.forward; curFwd.y = 0f;
        var rotDelta = Quaternion.FromToRotation(curFwd, targetFwd);
        newRoom.rotation = rotDelta * newRoom.rotation;

        newW = FindDoor(newRoom, "Door_W");
        Vector3 newWPos = newW.position;
        Vector3 targetPos = prevE.position + targetFwd * gap;
        Vector3 delta = targetPos - newWPos;
        newRoom.position += delta;
    }

    void ClearChildren(Transform t)
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeTransform && t && UnityEditor.Selection.activeTransform.IsChildOf(t))
            UnityEditor.Selection.activeObject = null;
#endif
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            var go = t.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(go);
#if UNITY_EDITOR
            else UnityEditor.Undo.DestroyObjectImmediate(go);
#else
            else DestroyImmediate(go);
#endif
        }
    }
}
