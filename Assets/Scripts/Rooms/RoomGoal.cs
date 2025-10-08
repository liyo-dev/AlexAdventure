using UnityEngine;
using UnityEngine.Events;

public class RoomGoal : MonoBehaviour
{
    public UnityEvent OnRoomCleared;
    public System.Action onRoomCleared;

    public void MarkCleared()
    {
        OnRoomCleared?.Invoke();
        onRoomCleared?.Invoke();
    }
}