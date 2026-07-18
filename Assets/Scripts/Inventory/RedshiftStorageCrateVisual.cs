// RedshiftStorageCrateVisual.cs
using UnityEngine;

public class RedshiftStorageCrateVisual : MonoBehaviour
{
    [Header("Icon Renderers")]
    [SerializeField] private SpriteRenderer crateIconRenderer;

    public void SetItem(RedshiftInventoryItemData itemData)
    {
        gameObject.SetActive(itemData != null);

        if (itemData == null)
            return;

        if (crateIconRenderer != null)
            crateIconRenderer.sprite = itemData.crateIcon != null ? itemData.crateIcon : itemData.inventoryIcon;
    }

    public void Clear()
    {
        if (crateIconRenderer != null)
            crateIconRenderer.sprite = null;

        gameObject.SetActive(false);
    }
}