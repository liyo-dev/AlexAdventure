using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    private readonly Dictionary<string, int> bag = new();

    public void Add(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;
        bag.TryGetValue(item.itemId, out int cur);
        bag[item.itemId] = cur + amount;
        Debug.Log($"[Inventory] +{amount} {item.displayName} (total {bag[item.itemId]})");
    }

    public int Count(string itemId) => bag.TryGetValue(itemId, out var v) ? v : 0;
}