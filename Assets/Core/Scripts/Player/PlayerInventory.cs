using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }
    public List<ItemData> items = new List<ItemData>();
    
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(ItemData item)
    {
        return items.Contains(item);
    }

    public bool RemoveItem(ItemData item)
    {
        bool removed = items.Remove(item);
        if (removed)
        {
            OnInventoryChanged?.Invoke();
        }
        return removed;
    }
}