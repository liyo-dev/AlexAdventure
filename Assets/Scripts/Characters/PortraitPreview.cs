using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class PortraitPreview : MonoBehaviour
{
    public Transform target;
    public Vector2Int size = new Vector2Int(512, 512);
    public RenderTexture targetTexture;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        EnsureRT();
    }

    void EnsureRT()
    {
        if (targetTexture == null || targetTexture.width != size.x || targetTexture.height != size.y)
        {
            if (targetTexture != null) targetTexture.Release();
            targetTexture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            targetTexture.name = "PortraitRT";
        }
        cam.targetTexture = targetTexture;
    }

#if UNITY_EDITOR
    [ContextMenu("Save Portrait PNG to Assets/Portrait.png")]
    public void SavePNG()
    {
        EnsureRT();
        var prev = RenderTexture.active;
        RenderTexture.active = targetTexture;
        var tex = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, targetTexture.width, targetTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        var bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes("Assets/Portrait.png", bytes);
        AssetDatabase.Refresh();
        Debug.Log("Portrait guardado en Assets/Portrait.png");
    }
#endif
}