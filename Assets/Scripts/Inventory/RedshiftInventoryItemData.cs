// RedshiftInventoryItemData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Redshift/Inventory/Item Data")]
public class RedshiftInventoryItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemID = "ITM_RS80_SERVER";
    public string displayName = "RS-80 Research Server";

    [Header("Icons")]
    public Sprite inventoryIcon;
    public Sprite crateIcon;

    [Header("Prefabs")]
    public GameObject placeablePrefab;

    [Header("Rules")]
    public bool canBePlaced = true;
    public int maxStackSize = 1;
}