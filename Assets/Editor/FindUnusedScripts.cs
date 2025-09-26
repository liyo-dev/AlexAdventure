#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FindUnusedScripts
{
    [MenuItem("Tools/Cleanup/Find Unused Scripts")] 
    public static void FindUnused()
    {
        var unused = ScanUnusedScripts();
        if (unused.Count == 0)
        {
            Debug.Log("[FindUnusedScripts] No se encontraron scripts no usados (por referencia en assets o código).\nNota: Puede haber scripts usados solo en runtime por reflexión.");
            return;
        }

        // Mostrar resultado y seleccionar en Project
        var msg = $"[FindUnusedScripts] Candidatos a eliminar: {unused.Count}\n" + string.Join("\n", unused.Select(u => $"- {u.path}"));
        Debug.Log(msg);

        var objs = unused.Select(u => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(u.path)).Where(o => o).ToArray();
        Selection.objects = objs;
        EditorUtility.DisplayDialog("Find Unused Scripts", $"Encontrados {unused.Count} candidatos. Revisa la consola y la selección en Project.", "OK");
    }

    [MenuItem("Tools/Cleanup/Find and Delete Unused Scripts (Dangerous)")]
    public static void FindAndDeleteUnused()
    {
        var unused = ScanUnusedScripts();
        if (unused.Count == 0)
        {
            EditorUtility.DisplayDialog("Delete Unused Scripts", "No se encontraron scripts no usados.", "OK");
            return;
        }

        var msg = $"Se van a eliminar {unused.Count} scripts no referenciados.\n\n" + string.Join("\n", unused.Take(30).Select(u => u.path)) + (unused.Count > 30 ? "\n..." : string.Empty);
        bool confirm = EditorUtility.DisplayDialog("Confirmar borrado", msg, "Eliminar", "Cancelar");
        if (!confirm) return;

        int deleted = 0; int failed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var u in unused)
            {
                if (AssetDatabase.DeleteAsset(u.path)) deleted++;
                else failed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Delete Unused Scripts", $"Eliminados: {deleted}\nFallidos: {failed}", "OK");
    }

    private static List<(string guid, string path, string name, Type type)> ScanUnusedScripts()
    {
        // 1) Recoger todos los MonoScript del proyecto
        var monoScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
        var scripts = new List<(string guid, string path, string name, Type type)>();
        foreach (var guid in monoScriptGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (!ms) continue;
            var t = ms.GetClass();
            // Solo clases válidas (pueden ser scripts de editor también)
            scripts.Add((guid, path, ms.name, t));
        }

        // 2) Construir set de GUIDs usados en assets YAML (.prefab, .unity, .asset, .controller, etc.)
        var usedGuidsInAssets = new HashSet<string>();
        var assetGuids = AssetDatabase.FindAssets(string.Empty)
            .Where(g => {
                var p = AssetDatabase.GUIDToAssetPath(g);
                // Filtrar solo YAML-text assets
                var ext = Path.GetExtension(p).ToLowerInvariant();
                return ext == ".prefab" || ext == ".unity" || ext == ".asset" || ext == ".controller" || ext == ".overrideController" || ext == ".playable" || ext == ".anim" || ext == ".mat";
            })
            .ToArray();

        int scanned = 0;
        foreach (var guid in assetGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            scanned++;
            if (scanned % 250 == 0)
                EditorUtility.DisplayProgressBar("Scanning assets for script references", path, (float)scanned / assetGuids.Length);

            try
            {
                var text = File.ReadAllText(path);
                // Buscar "m_Script: {fileID: 11500000, guid: <guid>, type: 3}"
                foreach (var s in scripts)
                {
                    if (text.Contains(s.guid))
                    {
                        usedGuidsInAssets.Add(s.guid);
                    }
                }
            }
            catch { /* ignorar binarios */ }
        }
        EditorUtility.ClearProgressBar();

        // 3) Construir set de tipos usados en código (.cs) buscando por nombre de clase
        var codeFiles = AssetDatabase.FindAssets("t:TextAsset")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => Path.GetExtension(p).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var usedTypeNamesInCode = new HashSet<string>(StringComparer.Ordinal);
        scanned = 0;
        foreach (var path in codeFiles)
        {
            scanned++;
            if (scanned % 500 == 0)
                EditorUtility.DisplayProgressBar("Scanning code for type references", path, (float)scanned / codeFiles.Length);
            try
            {
                var text = File.ReadAllText(path);
                foreach (var s in scripts)
                {
                    // Búsqueda simple por nombre de clase (puede tener falsos positivos, pero es útil para evitar falsos negativos)
                    if (text.Contains(s.name))
                    {
                        usedTypeNamesInCode.Add(s.name);
                    }
                }
            }
            catch { }
        }
        EditorUtility.ClearProgressBar();

        // 4) Candidatos a no usados: sin referencia en assets (GUID) ni en código (por nombre)
        var unused = scripts
            .Where(s => !usedGuidsInAssets.Contains(s.guid) && !usedTypeNamesInCode.Contains(s.name))
            .OrderBy(s => s.path)
            .ToList();

        return unused;
    }
}
#endif
