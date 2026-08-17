using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RRNEmailEntryUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Graphics")]
    [SerializeField] private Graphic background;
    [SerializeField] private Outline outline;

    [Header("Mail Fields")]
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text messageTitleText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private Image departmentIcon;
    [SerializeField] private GameObject unreadIndicator;

    [Header("Background Colours")]
    [SerializeField] private Color normalBackgroundColour =
        new Color32(0x16, 0x1C, 0x16, 255);

    [SerializeField] private Color highlightedBackgroundColour =
        new Color32(0x26, 0x22, 0x12, 255);

    [SerializeField] private Color selectedBackgroundColour =
        new Color32(0x2C, 0x23, 0x0E, 255);

    [SerializeField] private Color pressedBackgroundColour =
        new Color32(0x3A, 0x25, 0x05, 255);

    [Header("Text Colours")]
    [SerializeField] private Color unreadTextColour =
        new Color32(0xFF, 0x84, 0x00, 255);

    [SerializeField] private Color readTextColour =
        new Color32(0xD6, 0xD6, 0xD6, 255);

    [SerializeField] private Color highlightedTextColour =
        new Color32(0xFF, 0x84, 0x00, 255);

    [Header("Outline Colours")]
    [SerializeField] private Color normalOutlineColour =
        new Color32(0x60, 0x60, 0x60, 255);

    [SerializeField] private Color highlightedOutlineColour =
        new Color32(0xDA, 0x70, 0x00, 255);

    [SerializeField] private Color pressedOutlineColour =
        new Color32(0xFF, 0x84, 0x00, 255);

    private RRNReceivedEmail email;
    private Action<RRNEmailEntryUI> clickedCallback;

    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    public RRNReceivedEmail Email => email;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClicked);
    }

    public void Setup(
        RRNReceivedEmail newEmail,
        Action<RRNEmailEntryUI> onClicked)
    {
        email = newEmail;
        clickedCallback = onClicked;

        RefreshDisplay();
        ApplyCurrentState();
    }

    private void RefreshDisplay()
    {
        if (email == null || email.Definition == null)
            return;

        RRNEmailDefinition data = email.Definition;

        if (senderText != null)
            senderText.text = data.sender;

        if (dateText != null)
            dateText.text = email.Date;

        if (messageTitleText != null)
            messageTitleText.text = data.subject;

        if (timeText != null)
            timeText.text = email.Time;

        if (departmentIcon != null)
        {
            departmentIcon.sprite = data.departmentIcon;
            departmentIcon.enabled = data.departmentIcon != null;
        }

        RefreshReadState();
    }

    public void RefreshReadState()
    {
        if (email == null)
            return;

        if (unreadIndicator != null)
            unreadIndicator.SetActive(!email.IsRead);

        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyCurrentState();
    }

    private void HandleClicked()
    {
        clickedCallback?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UISoundManager.Instance?.PlayHover();
        ApplyCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyCurrentState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ApplyCurrentState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ApplyCurrentState();
    }

    private void ApplyCurrentState()
    {
        if (isPressed)
        {
            SetBackground(pressedBackgroundColour);
            SetOutline(pressedOutlineColour);
            SetMailText(highlightedTextColour);
            return;
        }

        if (isHovered)
        {
            SetBackground(highlightedBackgroundColour);
            SetOutline(highlightedOutlineColour);
            SetMailText(highlightedTextColour);
            return;
        }

        if (isSelected)
        {
            SetBackground(selectedBackgroundColour);
            SetOutline(highlightedOutlineColour);
            SetMailText(GetDefaultMailTextColour());
            return;
        }

        SetBackground(normalBackgroundColour);
        SetOutline(normalOutlineColour);
        SetMailText(GetDefaultMailTextColour());
    }

    private Color GetDefaultMailTextColour()
    {
        if (email != null && !email.IsRead)
            return unreadTextColour;

        return readTextColour;
    }

    private void SetMailText(Color colour)
    {
        if (senderText != null)
            senderText.color = colour;

        if (messageTitleText != null)
            messageTitleText.color = colour;

        if (dateText != null)
            dateText.color = readTextColour;

        if (timeText != null)
            timeText.color = readTextColour;
    }

    private void SetBackground(Color colour)
    {
        if (background != null)
            background.color = colour;
    }

    private void SetOutline(Color colour)
    {
        if (outline != null)
            outline.effectColor = colour;
    }
}
