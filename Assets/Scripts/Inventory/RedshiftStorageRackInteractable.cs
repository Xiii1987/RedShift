// RedshiftStorageRackInteractable.cs
using UnityEngine;

public class RedshiftStorageRackInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RedshiftInventoryContainer inventoryContainer;
    [SerializeField] private RedshiftStorageRackUI storageUI;
    [SerializeField] private RedshiftInteractableInfo interactableInfo;

    private void Awake()
    {
        if (inventoryContainer == null)
            inventoryContainer = GetComponent<RedshiftInventoryContainer>();

        if (interactableInfo == null)
            interactableInfo = GetComponent<RedshiftInteractableInfo>();
    }

    public void OpenStorageRack()
    {
        if (storageUI == null)
            storageUI = FindAnyObjectByType<RedshiftStorageRackUI>(FindObjectsInactive.Include);

        if (storageUI == null || inventoryContainer == null)
        {
            Debug.LogWarning("Storage object missing UI or inventory reference.");
            return;
        }

        string storageName = interactableInfo != null
            ? interactableInfo.DisplayName
            : gameObject.name;

        storageUI.OpenStorage(inventoryContainer, storageName);
    }
}