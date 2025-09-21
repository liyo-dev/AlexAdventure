using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item", fileName = "IT_NewItem")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
}