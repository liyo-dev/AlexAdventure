#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ModularCharacterBaker
{
    [MenuItem("Tools/Modular Characters/Bake Selected As Clean Prefab")]
    public static void BakeSelected()
    {
        var src = Selection.activeGameObject;
        if (!src)
        {
            EditorUtility.DisplayDialog("Baker", "Selecciona el GO raíz del personaje en la escena.", "Ok");
            return;
        }

        // Evita referencias del Inspector a hijos que vamos a destruir
        Selection.activeObject = null;

        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject clone = null;

        try
        {
            clone = Object.Instantiate(src);
            clone.name = src.name + "_BAKED";
            SceneManager.MoveGameObjectToScene(clone, tempScene);

            RemoveInactiveRecursive(clone.transform);
            RemoveEmptyHolders(clone.transform);

            string dir = "Assets/Prefabs/NPCs";
            System.IO.Directory.CreateDirectory(dir);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{clone.name}.prefab");

            PrefabUtility.SaveAsPrefabAsset(clone, path);
            EditorUtility.DisplayDialog("Baked!", $"Prefab creado:\n{path}", "Ok");
        }
        finally
        {
            if (clone) Object.DestroyImmediate(clone);
            EditorSceneManager.CloseScene(tempScene, true);
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.Refresh();
        }
    }

    static void RemoveInactiveRecursive(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            RemoveInactiveRecursive(t.GetChild(i));
        if (!t.gameObject.activeSelf)
            Object.DestroyImmediate(t.gameObject);
    }

    static void RemoveEmptyHolders(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            RemoveEmptyHolders(t.GetChild(i));

        bool hasRenderer = t.GetComponent<Renderer>() || t.GetComponent<SkinnedMeshRenderer>();
        bool hasChildren = t.childCount > 0;
        bool isRoot = t.parent == null;
        if (!isRoot && !hasRenderer && !hasChildren)
            Object.DestroyImmediate(t.gameObject);
    }
}
#endif
