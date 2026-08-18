using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RRNMailResponseButtonUI : MonoBehaviour,
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

    [Header("Text")]
    [SerializeField] private TMP_Text responseText;

    [Header("Colours")]
    [SerializeField] private Color normalBackgroundColour = new Color32(0x16, 0x1C, 0x16, 255);
    [SerializeField] private Color highlightedBackgroundColour = new Color32(0x26, 0x22, 0x12, 255);
    [SerializeField] private Color pressedBackgroundColour = new Color32(0x3A, 0x25, 0x05, 255);
    [SerializeField] private Color normalTextColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color highlightedTextColour = new Color32(0xFF, 0xA0, 0x30, 255);
    [SerializeField] private Color normalOutlineColour = new Color32(0x60, 0x60, 0x60, 255);
    [SerializeField] private Color highlightedOutlineColour = new Color32(0xDA, 0x70, 0x00, 255);

    [Header("Locked Colours")]
    [SerializeField] private Color lockedTextColour = new Color32(0xD6, 0xD6, 0xD6, 255);
    [SerializeField] private Color lockedOutlineColour = new Color32(0x60, 0x60, 0x60, 255);

    private RRNEmailResponseDefinition response;
    private Action<RRNEmailResponseDefinition> clickedCallback;

    private bool isHovered;
    private bool isPressed;
    private bool isLocked;
    private bool isChosen;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    public void Setup(
        RRNEmailResponseDefinition newResponse,
        Action<RRNEmailResponseDefinition> callback)
    {
        response = newResponse;
        clickedCallback = callback;
        isHovered = false;
        isPressed = false;
        isLocked = false;
        isChosen = false;

        bool hasResponse = response != null && !string.IsNullOrWhiteSpace(response.buttonText);
        gameObject.SetActive(hasResponse);

        if (!hasResponse)
            return;

        if (responseText != null)
            responseText.text = response.buttonText;

        if (button != null)
            button.interactable = true;

        ApplyCurrentState();
    }

    public void SetLocked(bool locked, bool chosen)
    {
        isLocked = locked;
        isChosen = chosen;
        isHovered = false;
        isPressed = false;

        if (button != null)
            button.interactable = !locked;

        ApplyCurrentState();
    }

    private void HandleClick()
    {
        if (response == null || isLocked)
            return;

        UISoundManager.Instance?.PlayConfirm();
        clickedCallback?.Invoke(response);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isHovered = true;
        UISoundManager.Instance?.PlayHover();
        ApplyCurrentState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isHovered = false;
        ApplyCurrentState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isPressed = true;
        ApplyCurrentState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isLocked)
            return;

        isPressed = false;
        ApplyCurrentState();
    }

    private void ApplyCurrentState()
    {
        if (isLocked)
        {
            if (isChosen)
            {
                SetColours(highlightedBackgroundColour, highlightedOutlineColour, highlightedTextColour);
            }
            else
            {
                SetColours(normalBackgroundColour, lockedOutlineColour, lockedTextColour);
            }
            return;
        }

        if (isPressed)
        {
            SetColours(pressedBackgroundColour, highlightedOutlineColour, highlightedTextColour);
            return;
        }

        if (isHovered)
        {
            SetColours(highlightedBackgroundColour, highlightedOutlineColour, highlightedTextColour);
            return;
        }

        SetColours(normalBackgroundColour, normalOutlineColour, normalTextColour);
    }

    private void SetColours(Color backgroundColour, Color outlineColour, Color textColour)
    {
        if (background != null)
            background.color = backgroundColour;

        if (outline != null)
            outline.effectColor = outlineColour;

        if (responseText != null)
            responseText.color = textColour;
    }
}
