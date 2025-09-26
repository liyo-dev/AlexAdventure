using UnityEngine;

public class SpawnAnchor : MonoBehaviour
{
    public string anchorId;        // único (p.ej. "Bedroom", "City_Gate", "Desert_Camp")
    public Vector3 facing = Vector3.forward; // opcional; si (0), usa forward del transform

    private void OnEnable()
    {
        AnchorRegistry.Register(this);
    }

    private void OnDisable()
    {
        AnchorRegistry.Unregister(this);
    }

    public static SpawnAnchor FindById(string id)
    {
        // Fallback a registro en memoria
        var a = AnchorRegistry.Get(id);
        if (a) return a;
        // Búsqueda lenta de respaldo si no estaba registrado (escena no inicializada aún, etc.)
        foreach(var x in GameObject.FindObjectsByType<SpawnAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (x && x.anchorId == id) return x;
        return null;
    }
}