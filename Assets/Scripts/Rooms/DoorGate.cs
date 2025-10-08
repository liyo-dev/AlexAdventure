using UnityEngine;

public class DoorGate : MonoBehaviour
{
    [SerializeField] GameObject barrier;   // pared/sistema de árboles/portón
    [SerializeField] bool startsOpen = false;

    void Awake()
    {
        if (!barrier) barrier = this.gameObject;
        if (startsOpen) Open(); else Close();
    }

    public void Open()
    {
        if (barrier) barrier.SetActive(false);
    }

    public void Close()
    {
        if (barrier) barrier.SetActive(true);
    }
}