using UnityEngine;

public class RedshiftInteractableInfo : MonoBehaviour
{
    [Header("Manual Interaction")]
    public string displayName = "Interactable";
    public string actionText = "Use";

    [Header("Inventory Item (Optional)")]
    [SerializeField] private RedshiftInventoryItemData itemData;

    public RedshiftInventoryItemData ItemData => itemData;

    public string DisplayName
    {
        get
        {
            if (itemData != null)
                return itemData.displayName;

            return displayName;
        }
    }

    public string ActionText => actionText;
}