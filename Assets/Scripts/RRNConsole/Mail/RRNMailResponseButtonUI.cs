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
    [SerializeField] private Color normalBackgroundColour =
        new Color32(0x16, 0x1C, 0x16, 255);

    [SerializeField] private Color highlightedBackgroundColour =
        new Color32(0x26, 0x22, 0x12, 255);

    [SerializeField] private Color pressedBackgroundColour =
        new Color32(0x3A, 0x25, 0x05, 255);

    [SerializeField] private Color normalTextColour =
        new Color32(0xFF, 0x84, 0x00, 255);

    [SerializeField] private Color highlightedTextColour =
        new Color32(0xFF, 0xA0, 0x30, 255);

    [SerializeField] private Color normalOutlineColour =
        new Color32(0x60, 0x60, 0x60, 255);

    [SerializeField] private Color highlightedOutlineColour =
        new Color32(0xDA, 0x70, 0x00, 255);

    private RRNEmailResponseDefinition response;
    private Action<RRNEmailResponseDefinition> clickedCallback;

    private bool isHovered;
    private bool isPressed;

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

        bool hasResponse =
            response != null &&
            !string.IsNullOrWhiteSpace(response.buttonText);

        gameObject.SetActive(hasResponse);

        if (!hasResponse)
            return;

        if (responseText != null)
            responseText.text = response.buttonText;

        ApplyCurrentState();
    }

    private void HandleClick()
    {
        if (response == null)
            return;

        UISoundManager.Instance?.PlayConfirm();
        clickedCallback?.Invoke(response);
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
            SetColours(
                pressedBackgroundColour,
                highlightedOutlineColour,
                highlightedTextColour);
            return;
        }

        if (isHovered)
        {
            SetColours(
                highlightedBackgroundColour,
                highlightedOutlineColour,
                highlightedTextColour);
            return;
        }

        SetColours(
            normalBackgroundColour,
            normalOutlineColour,
            normalTextColour);
    }

    private void SetColours(
        Color backgroundColour,
        Color outlineColour,
        Color textColour)
    {
        if (background != null)
            background.color = backgroundColour;

        if (outline != null)
            outline.effectColor = outlineColour;

        if (responseText != null)
            responseText.color = textColour;
    }
}
