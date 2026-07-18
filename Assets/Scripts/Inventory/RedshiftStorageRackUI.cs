// RedshiftStorageRackUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RedshiftStorageRackUI : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI titleText;
	
	[Header("Hover Info")]
	[SerializeField] private TextMeshProUGUI hoverInfoText;
	
	[Header("Buttons")]
	[SerializeField] private Button closeButton;


    [Header("Storage Slot Spawning")]
    [SerializeField] private RectTransform storagePanel;
    [SerializeField] private Transform storageSlotsParent;
    [SerializeField] private RedshiftInventorySlotUI slotPrefab;

    [Header("Grid Settings")]
    [SerializeField] private GridLayoutGroup storageGrid;
    [SerializeField] private int maxColumns = 8;

    [Header("Hotbar Slots")]
    [SerializeField] private RedshiftInventorySlotUI[] hotbarSlotUIs;

    [Header("References")]
    [SerializeField] private RedshiftHotbarInventory hotbarInventory;

    private readonly List<RedshiftInventorySlotUI> spawnedStorageSlots = new();

    private RedshiftInventoryContainer currentContainer;
    private string currentStorageName;
    private bool isOpen;
	public static RedshiftInventoryContainer CurrentOpenContainer { get; private set; }

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (storageGrid == null && storageSlotsParent != null)
            storageGrid = storageSlotsParent.GetComponent<GridLayoutGroup>();

		if (closeButton != null)
			closeButton.onClick.AddListener(CloseStorage);


        CloseStorage();
		
		
    }

    private void Update()
{
    if (!isOpen)
        return;

#if ENABLE_INPUT_SYSTEM
    Keyboard keyboard = Keyboard.current;

    if (keyboard == null)
        return;

    if (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
    {
        CloseStorage();
    }
#else
    if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
    {
        CloseStorage();
    }
#endif
}

    public void OpenStorage(RedshiftInventoryContainer container, string storageName)
    {
        if (container == null)
            return;

        currentContainer = container;
        currentStorageName = storageName;
        isOpen = true;

        if (hotbarInventory == null)
            hotbarInventory = RedshiftHotbarInventory.Instance;

        currentContainer.OnContentsChanged += RefreshTitle;

        BuildStorageSlots();
        RefreshTitle();

        LayoutRebuilder.ForceRebuildLayoutImmediate(storageSlotsParent as RectTransform);

        if (storagePanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(storagePanel);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        RedshiftPlayerStateController.Instance?.EnterUI();
		CurrentOpenContainer = currentContainer;
			if (hoverInfoText != null)
			hoverInfoText.text = "";
		
    }

    public void CloseStorage()
    {
        if (currentContainer != null)
            currentContainer.OnContentsChanged -= RefreshTitle;

        currentContainer = null;
        currentStorageName = "";
        isOpen = false;

        ClearSpawnedStorageSlots();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        RedshiftPlayerStateController.Instance?.EnterGameplay();
		CurrentOpenContainer = null;
    }

    private void BuildStorageSlots()
    {
        ClearSpawnedStorageSlots();

        if (currentContainer == null || storageSlotsParent == null || slotPrefab == null)
            return;

        UpdateGridColumns(currentContainer.SlotCount);

        for (int i = 0; i < currentContainer.SlotCount; i++)
        {
            RedshiftInventorySlotUI newSlot = Instantiate(slotPrefab, storageSlotsParent);
            newSlot.Setup(currentContainer, i);
			newSlot.SetHoverInfoText(hoverInfoText);
            spawnedStorageSlots.Add(newSlot);
        }
    }

    private void UpdateGridColumns(int slotCount)
    {
        if (storageGrid == null)
            return;

        int columns = Mathf.Clamp(slotCount, 1, maxColumns);

        storageGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        storageGrid.constraintCount = columns;
    }

    private void ClearSpawnedStorageSlots()
    {
        for (int i = 0; i < spawnedStorageSlots.Count; i++)
        {
            if (spawnedStorageSlots[i] != null)
                Destroy(spawnedStorageSlots[i].gameObject);
        }

        spawnedStorageSlots.Clear();
    }

  

    private void RefreshTitle()
    {
        if (titleText == null || currentContainer == null)
            return;

        titleText.text = $"{currentStorageName} ({currentContainer.FreeSlotCount}/{currentContainer.SlotCount})";
    }
}