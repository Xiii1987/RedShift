// RedshiftInventoryContainer.cs
using System;
using UnityEngine;

public class RedshiftInventoryContainer : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private int slotCount = 8;
    [SerializeField] private RedshiftInventorySlot[] slots;

    public event Action OnContentsChanged;

    public int SlotCount => slots != null ? slots.Length : 0;

    protected virtual void Awake()
    {
        EnsureSlots();
    }

    private void OnValidate()
    {
        EnsureSlots();
    }

    private void EnsureSlots()
    {
        if (slotCount < 1)
            slotCount = 1;

        if (slots == null || slots.Length != slotCount)
        {
            RedshiftInventorySlot[] newSlots = new RedshiftInventorySlot[slotCount];

            for (int i = 0; i < newSlots.Length; i++)
            {
                if (slots != null && i < slots.Length && slots[i] != null)
                    newSlots[i] = slots[i];
                else
                    newSlots[i] = new RedshiftInventorySlot();
            }

            slots = newSlots;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new RedshiftInventorySlot();
        }
    }

    public RedshiftInventorySlot GetSlot(int index)
    {
        if (!IsValidIndex(index))
            return null;

        return slots[index];
    }

    public bool AddToFirstFreeSlot(RedshiftInventoryItemData itemData, int amount = 1)
    {
        if (itemData == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].Set(itemData, amount);
                NotifyChanged();
                return true;
            }
        }

        return false;
    }

    public bool RemoveAmount(int index, int amount = 1)
    {
        if (!IsValidIndex(index) || amount <= 0)
            return false;

        RedshiftInventorySlot slot = slots[index];

        if (slot == null || slot.IsEmpty || slot.amount < amount)
            return false;

        slot.amount -= amount;

        if (slot.amount <= 0)
            slot.Clear();

        NotifyChanged();
        return true;
    }

    public void SetSlot(int index, RedshiftInventoryItemData itemData, int amount = 1)
    {
        if (!IsValidIndex(index))
            return;

        slots[index].Set(itemData, amount);
        NotifyChanged();
    }

    public void ClearSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        slots[index].Clear();
        NotifyChanged();
    }

    public void MoveOrSwapSlots(int fromIndex, RedshiftInventoryContainer targetContainer, int targetIndex)
    {
        if (targetContainer == null)
            return;

        if (!IsValidIndex(fromIndex) || !targetContainer.IsValidIndex(targetIndex))
            return;

        RedshiftInventorySlot fromSlot = slots[fromIndex];
        RedshiftInventorySlot targetSlot = targetContainer.GetSlot(targetIndex);

        if (fromSlot == null || targetSlot == null || fromSlot.IsEmpty)
            return;

        RedshiftInventoryItemData fromItem = fromSlot.itemData;
        int fromAmount = fromSlot.amount;

        RedshiftInventoryItemData targetItem = targetSlot.itemData;
        int targetAmount = targetSlot.amount;

        targetSlot.Set(fromItem, fromAmount);

        if (targetItem == null || targetAmount <= 0)
            fromSlot.Clear();
        else
            fromSlot.Set(targetItem, targetAmount);

        NotifyChanged();
        targetContainer.NotifyChanged();
    }
	
	public int FreeSlotCount
	{
		get
		{
			if (slots == null)
				return 0;

			int freeSlots = 0;

			for (int i = 0; i < slots.Length; i++)
			{
				if (slots[i] == null || slots[i].IsEmpty)
					freeSlots++;
			}

			return freeSlots;
		}
	}

	public bool MoveToFirstFreeSlot(
		int fromIndex,
		RedshiftInventoryContainer targetContainer)
	{
		if (targetContainer == null)
			return false;

		if (!IsValidIndex(fromIndex))
			return false;

		RedshiftInventorySlot fromSlot = slots[fromIndex];

		if (fromSlot == null || fromSlot.IsEmpty)
			return false;

		for (int i = 0; i < targetContainer.SlotCount; i++)
		{
			RedshiftInventorySlot targetSlot = targetContainer.GetSlot(i);

			if (targetSlot == null || !targetSlot.IsEmpty)
				continue;

			targetSlot.Set(fromSlot.itemData, fromSlot.amount);
			fromSlot.Clear();

			NotifyChanged();
			targetContainer.NotifyChanged();

			return true;
		}

		return false;
	}

    public bool IsValidIndex(int index)
    {
        return slots != null && index >= 0 && index < slots.Length;
    }

    public void NotifyChanged()
	{
		// If this container is the player's hotbar,
		// make sure the currently selected slot still contains an item.
		if (this is RedshiftHotbarInventory hotbar)
		{
			int selectedIndex = hotbar.SelectedSlotIndex;

			if (selectedIndex >= 0)
			{
				RedshiftInventorySlot selectedSlot =
					hotbar.GetSlot(selectedIndex);

				if (selectedSlot == null || selectedSlot.IsEmpty)
				{
					hotbar.ClearSelection();
				}
			}
		}

		OnContentsChanged?.Invoke();
	}


}