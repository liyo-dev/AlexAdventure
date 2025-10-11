#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class CharacterCreatorPersistence
{
    static Dictionary<PartCat, string> _savedSelection;
    static string _savedBuilderPath;

    static CharacterCreatorPersistence()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Cuando está saliendo del Play Mode, guardar la configuración
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SaveCurrentSelection();
        }
        // Cuando entra en Edit Mode, restaurar la configuración
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RestoreConfiguration;
        }
    }

    static void SaveCurrentSelection()
    {
        // Buscar el builder en la escena
        var builder = Object.FindFirstObjectByType<ModularAutoBuilder>();
        if (builder)
        {
            _savedSelection = builder.GetSelection();
            _savedBuilderPath = GetGameObjectPath(builder.gameObject);
            
            Debug.Log($"[Persistence] Configuración guardada: {_savedSelection.Count} partes activas");
            foreach (var part in _savedSelection)
            {
                Debug.Log($"  - {part.Key}: {part.Value}");
            }
        }
    }

    static void RestoreConfiguration()
    {
        if (_savedSelection == null || string.IsNullOrEmpty(_savedBuilderPath))
        {
            Debug.Log("[Persistence] No hay configuración para restaurar");
            return;
        }

        var builderGo = GameObject.Find(_savedBuilderPath);
        if (!builderGo)
        {
            Debug.LogWarning($"[Persistence] No se encontró el builder: {_savedBuilderPath}");
            _savedSelection = null;
            return;
        }

        var builderComp = builderGo.GetComponent<ModularAutoBuilder>();
        if (!builderComp)
        {
            Debug.LogWarning("[Persistence] No se encontró ModularAutoBuilder");
            _savedSelection = null;
            return;
        }

        Debug.Log("[Persistence] Restaurando configuración...");

        // Desactivar ensureDefaultsAtStart temporalmente
        var field = typeof(ModularAutoBuilder).GetField("ensureDefaultsAtStart", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        bool oldValue = field != null && (bool)field.GetValue(builderComp);
        if (field != null) field.SetValue(builderComp, false);

        // Reconstruir caché y limpiar
        var cacheMethod = typeof(ModularAutoBuilder).GetMethod("CacheAll", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var deactivateMethod = typeof(ModularAutoBuilder).GetMethod("DeactivateAll", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (cacheMethod != null && deactivateMethod != null)
        {
            cacheMethod.Invoke(builderComp, null);
            deactivateMethod.Invoke(builderComp, null);
        }

        // Aplicar selección
        builderComp.ApplySelection(_savedSelection);
        
        Debug.Log($"[Persistence] ✓ Configuración restaurada: {_savedSelection.Count} partes");
        Debug.Log("[Persistence] Ahora puedes eliminar manualmente los GameObjects inactivos que no necesites");
        
        foreach (var part in _savedSelection)
        {
            Debug.Log($"  ✓ {part.Key}: {part.Value}");
        }

        // Restaurar flag
        if (field != null) field.SetValue(builderComp, oldValue);

        EditorUtility.SetDirty(builderGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(builderGo.scene);

        // Limpiar
        _savedSelection = null;
        _savedBuilderPath = null;
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
#endif
