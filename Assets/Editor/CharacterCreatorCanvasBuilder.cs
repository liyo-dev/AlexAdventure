#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class CharacterCreatorCanvasBuilder
{
    static readonly string[] Cats = {
        "Body","Cloak","Head","Hair","Eyes","Mouth","Hat","Eyebrow","Accessory",
        "OHS","Shield","Bow"
    };

    [MenuItem("Tools/Modular Characters/Create Character Creator Canvas (Designer Only)")]
    public static void CreateCanvas()
    {
        var builder = Object.FindFirstObjectByType<ModularAutoBuilder>();
        if (!builder)
        {
            EditorUtility.DisplayDialog("Character Creator",
                "Coloca en la escena un personaje con 'ModularAutoBuilder' antes de crear la UI.",
                "Ok");
            return;
        }

        if (!Object.FindFirstObjectByType<EventSystem>())
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGO = new GameObject("UI_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Character Creator Canvas");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Panel contenedor (columna izquierda)
        var panel = New("Panel", canvasGO.transform, typeof(Image), typeof(VerticalLayoutGroup));
        panel.GetComponent<Image>().color = new Color(0.07f, 0.13f, 0.23f, 0.95f);

        var v = panel.GetComponent<VerticalLayoutGroup>();
        v.spacing = 8; v.padding = new RectOffset(16, 16, 16, 16);
        v.childForceExpandHeight = false; v.childForceExpandWidth = false;

        var ui = panel.AddComponent<CharacterCreatorUI>();
        ui.builder = builder;

        foreach (var c in Cats)
        {
            var row = New($"Row_{c}", panel.transform, typeof(HorizontalLayoutGroup));
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 6; h.childForceExpandWidth = false; h.childForceExpandHeight = false;

            MakeText(row.transform, c, 22, TextAnchor.MiddleLeft, new Vector2(160, 48));

            var prev = MakeButton(row.transform, "◄", new Vector2(60, 48));
            var prevStep = prev.AddComponent<UIButtonStep>();
            prevStep.ui = ui; prevStep.category = c; prevStep.step = -1;

            var next = MakeButton(row.transform, "►", new Vector2(60, 48));
            var nextStep = next.AddComponent<UIButtonStep>();
            nextStep.ui = ui; nextStep.category = c; nextStep.step = +1;

            var none = MakeButton(row.transform, "None", new Vector2(90, 48));
            var noneComp = none.AddComponent<UIButtonSetNone>();
            noneComp.ui = ui; noneComp.category = c;

            var active = MakeText(row.transform, "-", 18, TextAnchor.MiddleLeft, new Vector2(320, 48));
            var watch = active.gameObject.AddComponent<UITextCurrentPart>();
            watch.builder = builder; watch.category = c;
        }

        // Acciones
        var actions = New("Row_Actions", panel.transform, typeof(HorizontalLayoutGroup));
        actions.GetComponent<HorizontalLayoutGroup>().spacing = 10;

        var random = MakeButton(actions.transform, "Random", new Vector2(140, 54));
        random.GetComponent<Button>().onClick.AddListener(ui.RandomizeAll);

        var bake = MakeButton(actions.transform, "Bake NPC (Editor)", new Vector2(220, 54));
        bake.GetComponent<Button>().onClick.AddListener(() =>
        {
            Selection.activeGameObject = builder.gameObject;
            ModularCharacterBaker.BakeSelected();
        });

        // ===== Alineación y tamaño del Panel =====
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);   // esquina superior izquierda
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(600f, 1200f); // ancho 600, alto 1200
        rt.anchoredPosition = new Vector2(20f, -20f); // margen 20px a la derecha y abajo

        Selection.activeGameObject = canvasGO;
    }

    // ---------- helpers ----------
    static Font BuiltinUIFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static GameObject New(string n, Transform p, params System.Type[] t)
    {
        var go = new GameObject(n, t);
        go.transform.SetParent(p, false);
        return go;
    }

    static Text MakeText(Transform p, string txt, int size, TextAnchor a, Vector2 dim)
    {
        var go = New("Text", p, typeof(Text));
        var t = go.GetComponent<Text>();
        t.text = txt; t.font = BuiltinUIFont();
        t.alignment = a; t.fontSize = size; t.color = Color.white;
        go.GetComponent<RectTransform>().sizeDelta = dim;
        return t;
    }

    static GameObject MakeButton(Transform p, string label, Vector2 dim)
    {
        var go = New("Button", p, typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.15f);
        go.GetComponent<RectTransform>().sizeDelta = dim;

        var txt = New("Text", go.transform, typeof(Text)).GetComponent<Text>();
        txt.text = label; txt.font = BuiltinUIFont();
        txt.alignment = TextAnchor.MiddleCenter; txt.fontSize = 22; txt.color = Color.white;

        var tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;

        return go;
    }
}
#endif
