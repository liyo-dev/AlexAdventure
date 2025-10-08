using UnityEngine;

public class PuzzleTorchGateController : MonoBehaviour
{
    [Header("Refs")]
    public TorchInteract[] torches;
    public RoomGoal roomGoal;          // el de la sala
    public DoorGate exitDoor;          // puerta Este a abrir (opcional si escuchas RoomGoal fuera)

    [Header("Lógica")]
    public int requiredLit = 2;        // cuántas encendidas para abrir

    int currentLit;

    void Awake()
    {
        currentLit = 0;
        foreach (var t in torches)
        {
            if (!t) continue;
            if (t.isLit) currentLit++;
            t.onTorchToggled += OnTorchToggled;
        }
        CheckSolved();
    }

    void OnDestroy()
    {
        foreach (var t in torches) if (t) t.onTorchToggled -= OnTorchToggled;
    }

    void OnTorchToggled(bool nowLit)
    {
        currentLit += nowLit ? 1 : -1;
        CheckSolved();
    }

    void CheckSolved()
    {
        if (currentLit >= requiredLit)
        {
            roomGoal?.MarkCleared();
            if (exitDoor) exitDoor.Open();
            enabled = false;
        }
    }
}