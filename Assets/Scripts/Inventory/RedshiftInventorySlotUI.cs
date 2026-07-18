// RedshiftInventorySlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RedshiftInventorySlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private static RedshiftInventorySlotUI draggedSlot;
    private static Image draggedIcon;
    private static RectTransform draggedIconRect;

    [Header("Slot")]
    [SerializeField] private RedshiftInventoryContainer container;
    [SerializeField] private int slotIndex;
	
	[Header("Hover Info")]
	[SerializeField] private bool showItemNameOnHover = true;
	[SerializeField] private TextMeshProUGUI hoverInfoText;

    [Header("UI")]
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Image slotBackgroundImage;
    [SerializeField] private TextMeshProUGUI slotNumberText;

    [Header("Hover")]
    [SerializeField] private bool useHoverHighlight = false;
    [SerializeField] private Color normalColour = new Color32(18, 26, 14, 255);
    [SerializeField] private Color hoverColour = new Color32(55, 68, 38, 255);

    [Header("Quick Move")]
    [SerializeField] private bool allowShiftClickMove = true;
	
	[Header("Hover Info")]
	[SerializeField] private Image hoverInfoBackgroundImage;

    [Header("Drag")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Vector2 draggedIconSize = new Vector2(64f, 64f);

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (slotBackgroundImage == null)
            slotBackgroundImage = GetComponent<Image>();

        if (itemIconImage == null)
        {
            Transform iconChild = transform.Find("ItemIcon");
            if (iconChild != null)
                itemIconImage = iconChild.GetComponent<Image>();
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (container != null)
            container.OnContentsChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (container != null)
            container.OnContentsChanged -= Refresh;
    }

    public void Setup(RedshiftInventoryContainer newContainer, int newSlotIndex)
    {
        if (container != null)
            container.OnContentsChanged -= Refresh;

        container = newContainer;
        slotIndex = newSlotIndex;

        if (container != null)
            container.OnContentsChanged += Refresh;

        Refresh();
    }

    public void Refresh()
    {
        if (itemIconImage == null)
            return;

        RedshiftInventorySlot slot = container != null ? container.GetSlot(slotIndex) : null;

        if (slot == null || slot.IsEmpty || slot.itemData == null)
        {
            itemIconImage.enabled = false;
            itemIconImage.sprite = null;
            return;
        }

        Sprite icon = slot.itemData.inventoryIcon != null
            ? slot.itemData.inventoryIcon
            : slot.itemData.crateIcon;

        itemIconImage.sprite = icon;
        itemIconImage.enabled = icon != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!useHoverHighlight)
            return;

        if (slotBackgroundImage != null)
            slotBackgroundImage.color = hoverColour;

        UISoundManager.Instance?.PlayHover();
		RefreshHoverInfoText();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!useHoverHighlight)
            return;

        if (slotBackgroundImage != null)
            slotBackgroundImage.color = normalColour;
		ClearHoverInfoText();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!allowShiftClickMove || container == null)
            return;

        if (!IsShiftHeld())
            return;

        RedshiftInventoryContainer targetContainer = GetShiftClickTargetContainer();

        if (targetContainer == null || targetContainer == container)
            return;

        container.MoveToFirstFreeSlot(slotIndex, targetContainer);
    }

    private bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null &&
               UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed;
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    private RedshiftInventoryContainer GetShiftClickTargetContainer()
    {
        if (container == RedshiftHotbarInventory.Instance)
            return RedshiftStorageRackUI.CurrentOpenContainer;

        return RedshiftHotbarInventory.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (container == null)
            return;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            return;

        RedshiftInventorySlot slot = container.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty || slot.itemData == null)
            return;

        Sprite icon = slot.itemData.inventoryIcon != null
            ? slot.itemData.inventoryIcon
            : slot.itemData.crateIcon;

        if (icon == null)
            return;

        draggedSlot = this;

        GameObject dragObject = new GameObject("Dragged Inventory Icon", typeof(RectTransform));
        dragObject.transform.SetParent(rootCanvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        draggedIcon = dragObject.AddComponent<Image>();
        draggedIcon.sprite = icon;
        draggedIcon.color = Color.white;
        draggedIcon.raycastTarget = false;
        draggedIcon.preserveAspect = true;

        draggedIconRect = draggedIcon.GetComponent<RectTransform>();
        draggedIconRect.sizeDelta = draggedIconSize;
        draggedIconRect.pivot = new Vector2(0.5f, 0.5f);
        draggedIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        draggedIconRect.anchorMax = new Vector2(0.5f, 0.5f);

        UpdateDraggedIconPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDraggedIconPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggedSlot = null;

        if (draggedIcon != null)
            Destroy(draggedIcon.gameObject);

        draggedIcon = null;
        draggedIconRect = null;

        Refresh();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null || draggedSlot == this)
            return;

        if (draggedSlot.container == null || container == null)
            return;

        draggedSlot.container.MoveOrSwapSlots(draggedSlot.slotIndex, container, slotIndex);

        draggedSlot.Refresh();
        Refresh();
    }

    private void UpdateDraggedIconPosition(PointerEventData eventData)
    {
        if (draggedIconRect == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        draggedIconRect.anchoredPosition = localPoint;
    }
	
	public void SetHoverInfoText(TextMeshProUGUI newHoverInfoText)
	{
		hoverInfoText = newHoverInfoText;
	}
	
	
private void RefreshHoverInfoText()
{
    if (!showItemNameOnHover || hoverInfoText == null || container == null)
        return;

    RedshiftInventorySlot slot = container.GetSlot(slotIndex);

    if (slot == null || slot.IsEmpty || slot.itemData == null)
    {
        hoverInfoText.text = "";
        hoverInfoText.transform.parent.gameObject.SetActive(false);
        return;
    }

    hoverInfoText.text = slot.itemData.displayName;
    hoverInfoText.transform.parent.gameObject.SetActive(true);
}

private void ClearHoverInfoText()
{
    if (!showItemNameOnHover || hoverInfoText == null)
        return;

    hoverInfoText.text = "";
    hoverInfoText.transform.parent.gameObject.SetActive(false);
}
}