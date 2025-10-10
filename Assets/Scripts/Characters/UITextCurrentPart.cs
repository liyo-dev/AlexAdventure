using UnityEngine;
using UnityEngine.UI;
using System;

public class UITextCurrentPart : MonoBehaviour
{
    public ModularAutoBuilder builder;
    public string category; // "Head", "Body", etc.

    Text t; PartCat cat; bool ok;

    void Awake()
    {
        t = GetComponent<Text>();
        ok = Enum.TryParse(category, out cat);
    }

    void Update()
    {
        if (!ok || builder == null) return;
        var sel = builder.GetSelection();
        if (sel.TryGetValue(cat, out var part))
            t.text = part;
        else
            t.text = "-";
    }
}