// RedshiftInventorySlot.cs
using System;
using UnityEngine;

[Serializable]
public class RedshiftInventorySlot
{
    public RedshiftInventoryItemData itemData;
    public int amount;

    public bool IsEmpty => itemData == null || amount <= 0;

    public void Set(RedshiftInventoryItemData item, int newAmount = 1)
    {
        itemData = item;
        amount = Mathf.Max(0, newAmount);
    }

    public void Clear()
    {
        itemData = null;
        amount = 0;
    }
}