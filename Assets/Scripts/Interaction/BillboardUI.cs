using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera cam;
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (cam) transform.forward = cam.transform.forward;
    }
}