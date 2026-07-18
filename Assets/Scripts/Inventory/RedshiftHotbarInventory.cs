// RedshiftHotbarInventory.cs
using System;
using UnityEngine;

public class RedshiftHotbarInventory : RedshiftInventoryContainer
{
    public event Action<int> OnSelectedSlotChanged;

    [Header("Selection")]
    [SerializeField] private int selectedSlotIndex = -1;

    public int SelectedSlotIndex => selectedSlotIndex;
    public static RedshiftHotbarInventory Instance { get; private set; }

    public void SelectSlot(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (selectedSlotIndex == index)
            return;

        selectedSlotIndex = index;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
    }

    public void ClearSelection()
    {
        if (selectedSlotIndex == -1)
            return;

        selectedSlotIndex = -1;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
    }

		protected override void Awake()
	{
		base.Awake();

		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		// Always start the game with no hotbar slot selected.
		// This prevents an item picked up into slot 1 from
		// automatically entering placement mode.
		ClearSelection();
	}
}