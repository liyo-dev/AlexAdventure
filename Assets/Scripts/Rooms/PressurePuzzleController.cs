using UnityEngine;

public class PressurePuzzleController : MonoBehaviour
{
    public PressurePlate[] plates;
    public RoomGoal roomGoal;
    public DoorGate exitDoor;

    void Awake()
    {
        if (plates == null || plates.Length == 0)
            plates = GetComponentsInChildren<PressurePlate>(true);
        CheckSolved();
    }

    // Llamado por las placas vía SendMessageUpwards
    void OnPlateStateChanged(PressurePlate _)
    {
        CheckSolved();
    }

    void CheckSolved()
    {
        foreach (var p in plates)
            if (p && !p.isPressed) return;

        // todas pulsadas
        roomGoal?.MarkCleared();
        exitDoor?.Open();
        enabled = false;
    }
}