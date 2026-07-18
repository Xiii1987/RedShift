using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RRNServerSelectionEntryUI : MonoBehaviour,
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
    [SerializeField] private GameObject toggleObject;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI serverNameText;

    [Header("Background Colours")]
    [SerializeField] private Color normalBackgroundColour = new Color32(0x16, 0x1C, 0x16, 255);
    [SerializeField] private Color highlightedBackgroundColour = new Color32(0x26, 0x22, 0x12, 255);
    [SerializeField] private Color selectedBackgroundColour = new Color32(0x2C, 0x23, 0x0E, 255);
    [SerializeField] private Color pressedBackgroundColour = new Color32(0x3A, 0x25, 0x05, 255);

    [Header("Text Colours")]
    [SerializeField] private Color normalTextColour = new Color32(0xD6, 0xD6, 0xD6, 255);
    [SerializeField] private Color highlightedTextColour = new Color32(0xFF, 0x84, 0x00, 255);

    [Header("Outline Colours")]
    [SerializeField] private Color normalOutlineColour = new Color32(0x60, 0x60, 0x60, 255);
    [SerializeField] private Color highlightedOutlineColour = new Color32(0xDA, 0x70, 0x00, 255);
    [SerializeField] private Color pressedOutlineColour = new Color32(0xFF, 0x84, 0x00, 255);

    private ResearchServer server;
    private Action<RRNServerSelectionEntryUI> clickedCallback;

    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    public ResearchServer Server => server;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }

        ApplyCurrentState();
    }

    public void Setup(ResearchServer newServer, Action<RRNServerSelectionEntryUI> onClicked)
    {
        server = newServer;
        clickedCallback = onClicked;

        bool hasServer = server != null;
        gameObject.SetActive(hasServer);

        if (!hasServer)
        {
            return;
        }

        if (serverNameText != null)
        {
            serverNameText.text = server.ServerName;
        }

        if (button != null)
        {
            button.interactable = true;
        }

        SetSelected(false);
        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (toggleObject != null)
        {
            toggleObject.SetActive(isSelected);
        }

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
            ApplyPressedState();
            return;
        }

        if (isSelected)
        {
            ApplySelectedState();
            return;
        }

        if (isHovered)
        {
            ApplyHighlightedState();
            return;
        }

        ApplyNormalState();
    }

    private void ApplyNormalState()
    {
        SetBackgroundColour(normalBackgroundColour);
        SetOutlineColour(normalOutlineColour);
        SetTextColour(normalTextColour);
    }

    private void ApplyHighlightedState()
    {
        SetBackgroundColour(highlightedBackgroundColour);
        SetOutlineColour(highlightedOutlineColour);
        SetTextColour(highlightedTextColour);
    }

    private void ApplySelectedState()
    {
        SetBackgroundColour(selectedBackgroundColour);
        SetOutlineColour(highlightedOutlineColour);
        SetTextColour(highlightedTextColour);
    }

    private void ApplyPressedState()
    {
        SetBackgroundColour(pressedBackgroundColour);
        SetOutlineColour(pressedOutlineColour);
        SetTextColour(highlightedTextColour);
    }

    private void SetBackgroundColour(Color colour)
    {
        if (background != null) background.color = colour;
    }

    private void SetOutlineColour(Color colour)
    {
        if (outline != null) outline.effectColor = colour;
    }

    private void SetTextColour(Color colour)
    {
        if (serverNameText != null) serverNameText.color = colour;
    }
}