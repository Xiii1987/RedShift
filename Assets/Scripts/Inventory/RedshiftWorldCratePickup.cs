using UnityEngine;

public class RedshiftWorldCratePickup : MonoBehaviour
{
    [Header("Contained Item")]
    [SerializeField] private RedshiftInventoryItemData itemData;

    [Header("Pickup")]
    [SerializeField] private bool destroyCrateOnPickup = true;
    [SerializeField] private RedshiftUISoundType pickupSound = RedshiftUISoundType.Success;
    [SerializeField] private RedshiftUISoundType failedPickupSound = RedshiftUISoundType.Denied;

    public RedshiftInventoryItemData ItemData => itemData;

    public void PickUp()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name} has no item data assigned.");
            return;
        }

        if (RedshiftHotbarInventory.Instance == null)
        {
            Debug.LogWarning("No RedshiftHotbarInventory found.");
            return;
        }

        bool added = RedshiftHotbarInventory.Instance.AddToFirstFreeSlot(itemData, 1);

        if (!added)
        {
            UISoundManager.Instance?.PlaySound(failedPickupSound);
            Debug.Log("Hotbar is full.");
            return;
        }

        UISoundManager.Instance?.PlaySound(pickupSound);

        if (destroyCrateOnPickup)
            Destroy(gameObject);
    }
}