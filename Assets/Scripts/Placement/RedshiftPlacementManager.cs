using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class RedshiftPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private RedshiftHotbarInventory hotbarInventory;

    [Header("Raycast")]
    [SerializeField] private LayerMask placementSlotLayer;
    [SerializeField] private float placementDistance = 6f;

    [Header("Ghost Materials")]
    [SerializeField] private Material validGhostMaterial;
    [SerializeField] private Material invalidGhostMaterial;

    [Header("Controls")]
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;

    private readonly List<RedshiftPlacementSlot> footprintSlots = new();

    private RedshiftInventoryItemData selectedItem;
    private int selectedHotbarIndex = -1;
    private GameObject ghostObject;
    private RedshiftPlacementGhost ghost;
    private RedshiftPlaceable ghostPlaceable;
    private RedshiftPlacementSlot currentOriginSlot;
    private RedshiftPlacementGrid currentGrid;
    private int rotationQuarterTurns;
    private bool hasValidPlacement;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (hotbarInventory == null)
            hotbarInventory = RedshiftHotbarInventory.Instance;
    }

    private void OnEnable()
    {
        ResolveHotbar();

        if (hotbarInventory != null)
        {
            hotbarInventory.OnSelectedSlotChanged += HandleSelectedSlotChanged;
            hotbarInventory.OnContentsChanged += RefreshFromSelectedSlot;
            HandleSelectedSlotChanged(hotbarInventory.SelectedSlotIndex);
        }
    }

    private void OnDisable()
    {
        if (hotbarInventory != null)
        {
            hotbarInventory.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
            hotbarInventory.OnContentsChanged -= RefreshFromSelectedSlot;
        }

        CancelPlacement(false);
    }

    private void Update()
    {
        ResolveHotbar();
        HandleHotbarNumberKeys();

        if (ghostObject == null)
            return;

        if (CancelPressed())
        {
            CancelPlacement(true);
            return;
        }

        if (RotateLeftPressed())
            Rotate(-1);

        if (RotateRightPressed())
            Rotate(1);

        UpdateGhostPosition();

        if (ConfirmPressed())
            TryPlaceCurrentItem();
    }

    private void ResolveHotbar()
    {
        if (hotbarInventory != null)
            return;

        hotbarInventory = RedshiftHotbarInventory.Instance;
    }

    private void HandleHotbarNumberKeys()
    {
        if (hotbarInventory == null)
            return;

        for (int i = 0; i < Mathf.Min(10, hotbarInventory.SlotCount); i++)
        {
            if (NumberKeyPressed(i))
            {
                hotbarInventory.SelectSlot(i);
                return;
            }
        }
    }

    private void HandleSelectedSlotChanged(int slotIndex)
    {
        selectedHotbarIndex = slotIndex;
        RefreshFromSelectedSlot();
    }

    private void RefreshFromSelectedSlot()
    {
        if (hotbarInventory == null || !hotbarInventory.IsValidIndex(selectedHotbarIndex))
        {
            CancelPlacement(false);
            return;
        }

        RedshiftInventorySlot slot = hotbarInventory.GetSlot(selectedHotbarIndex);

        if (slot == null || slot.IsEmpty || slot.itemData == null ||
            !slot.itemData.canBePlaced || slot.itemData.placeablePrefab == null)
        {
            CancelPlacement(false);
            return;
        }

        if (selectedItem == slot.itemData && ghostObject != null)
            return;

        BeginPlacement(slot.itemData);
    }

    private void BeginPlacement(RedshiftInventoryItemData itemData)
    {
        CancelPlacement(false);

        selectedItem = itemData;
        rotationQuarterTurns = 0;

        ghostObject = Instantiate(itemData.placeablePrefab);
        ghostObject.name = $"{itemData.displayName}_Ghost";

        ghostPlaceable = ghostObject.GetComponent<RedshiftPlaceable>();
        if (ghostPlaceable == null)
            ghostPlaceable = ghostObject.AddComponent<RedshiftPlaceable>();

        ghost = ghostObject.GetComponent<RedshiftPlacementGhost>();
        if (ghost == null)
            ghost = ghostObject.AddComponent<RedshiftPlacementGhost>();

        ghost.Initialise(validGhostMaterial, invalidGhostMaterial);
        ghostObject.SetActive(false);
    }

    public void CancelPlacement(bool clearSelection)
    {
        if (ghostObject != null)
            Destroy(ghostObject);

        ghostObject = null;
        ghost = null;
        ghostPlaceable = null;
        currentOriginSlot = null;
        currentGrid = null;
        selectedItem = null;
        hasValidPlacement = false;
        footprintSlots.Clear();

        if (clearSelection && hotbarInventory != null)
            hotbarInventory.ClearSelection();
    }

    private void Rotate(int direction)
    {
        rotationQuarterTurns = (rotationQuarterTurns + direction) % 4;
        if (rotationQuarterTurns < 0)
            rotationQuarterTurns += 4;
    }

    private void UpdateGhostPosition()
    {
        hasValidPlacement = false;
        currentOriginSlot = null;
        currentGrid = null;
        footprintSlots.Clear();

        if (playerCamera == null || ghostObject == null)
            return;

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, placementDistance, placementSlotLayer, QueryTriggerInteraction.Collide))
        {
            ghostObject.SetActive(false);
            return;
        }

        RedshiftPlacementSlot originSlot = hit.collider.GetComponentInParent<RedshiftPlacementSlot>();
        RedshiftPlacementGrid grid = hit.collider.GetComponentInParent<RedshiftPlacementGrid>();

        if (originSlot == null || grid == null)
        {
            ghostObject.SetActive(false);
            return;
        }

        ghostObject.SetActive(true);
        currentOriginSlot = originSlot;
        currentGrid = grid;

        Quaternion rotation = Quaternion.Euler(0f, rotationQuarterTurns * 90f, 0f);
        ghostObject.transform.SetPositionAndRotation(
            originSlot.SnapPoint.position + rotation * ghostPlaceable.PlacementOffset,
            rotation);

        bool footprintExists = grid.TryGetFootprintSlots(
            originSlot,
            ghostPlaceable.Footprint,
            rotationQuarterTurns,
            footprintSlots);

        hasValidPlacement = footprintExists;

        if (hasValidPlacement)
        {
            foreach (RedshiftPlacementSlot slot in footprintSlots)
            {
                if (slot.IsOccupied)
                {
                    hasValidPlacement = false;
                    break;
                }
            }
        }

        ghost.SetValid(hasValidPlacement);
    }

    private void TryPlaceCurrentItem()
    {
        if (!hasValidPlacement || selectedItem == null || hotbarInventory == null ||
            !hotbarInventory.IsValidIndex(selectedHotbarIndex))
        {
            UISoundManager.Instance?.PlayDenied();
            return;
        }

        RedshiftInventorySlot inventorySlot = hotbarInventory.GetSlot(selectedHotbarIndex);

        if (inventorySlot == null || inventorySlot.IsEmpty || inventorySlot.itemData != selectedItem)
        {
            RefreshFromSelectedSlot();
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, rotationQuarterTurns * 90f, 0f);
        Vector3 position = currentOriginSlot.SnapPoint.position + rotation * ghostPlaceable.PlacementOffset;

        GameObject placedObject = Instantiate(selectedItem.placeablePrefab, position, rotation);
        RedshiftPlaceable placeable = placedObject.GetComponent<RedshiftPlaceable>();

        if (placeable == null)
            placeable = placedObject.AddComponent<RedshiftPlaceable>();

        List<RedshiftPlacementSlot> successfullyOccupiedSlots = new();

        foreach (RedshiftPlacementSlot slot in footprintSlots)
        {
            if (!slot.TryOccupy(placeable))
            {
                foreach (RedshiftPlacementSlot occupiedSlot in successfullyOccupiedSlots)
                    occupiedSlot.Release(placeable);

                Destroy(placedObject);
                UISoundManager.Instance?.PlayDenied();
                return;
            }

            successfullyOccupiedSlots.Add(slot);
        }

        placeable.RegisterOccupiedSlots(successfullyOccupiedSlots);
        hotbarInventory.RemoveAmount(selectedHotbarIndex, 1);
        UISoundManager.Instance?.PlaySuccess();

        // Inventory change refreshes automatically. If another copy remains,
        // the ghost stays active; if the stack is empty it disappears.
        RefreshFromSelectedSlot();
    }

    private bool NumberKeyPressed(int slotIndex)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return false;

        Key[] keys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };

        return Keyboard.current[keys[slotIndex]].wasPressedThisFrame;
#else
        KeyCode[] keys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };

        return Input.GetKeyDown(keys[slotIndex]);
#endif
    }

    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        bool rightClick = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        return rightClick || escape;
#else
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private bool RotateLeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(rotateLeftKey);
#endif
    }

    private bool RotateRightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(rotateRightKey);
#endif
    }
}
