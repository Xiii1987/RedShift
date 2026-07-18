// RedshiftStorageRackInventory.cs
using UnityEngine;

public class RedshiftStorageRackInventory : RedshiftInventoryContainer
{
    [Header("World Crate Visuals")]
    [SerializeField] private RedshiftStorageCrateVisual[] crateVisuals;

    protected override void Awake()
    {
        base.Awake();

        OnContentsChanged += RefreshWorldCrates;
        RefreshWorldCrates();
    }

    private void OnDestroy()
    {
        OnContentsChanged -= RefreshWorldCrates;
    }

    public void RefreshWorldCrates()
    {
        if (crateVisuals == null)
            return;

        for (int i = 0; i < crateVisuals.Length; i++)
        {
            if (crateVisuals[i] == null)
                continue;

            RedshiftInventorySlot slot = GetSlot(i);

            if (slot == null || slot.IsEmpty)
                crateVisuals[i].Clear();
            else
                crateVisuals[i].SetItem(slot.itemData);
        }
    }
}