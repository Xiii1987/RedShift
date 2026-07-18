using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonColourFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Target Graphics")]
    [SerializeField] private Graphic targetPanel;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private Outline targetOutline;

    [Header("Panel Colours")]
    [SerializeField] private Color normalPanelColour = new Color32(0x16, 0x1C, 0x16, 255);
    [SerializeField] private Color highlightedPanelColour = new Color32(0xDA, 0x70, 0x00, 255);
    [SerializeField] private Color pressedPanelColour = new Color32(0xFF, 0x84, 0x00, 255);

    [Header("Text Colours")]
    [SerializeField] private Color normalTextColour = new Color32(0xD6, 0xD6, 0xD6, 255);
    [SerializeField] private Color highlightedTextColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color pressedTextColour = new Color32(0xFF, 0x84, 0x00, 255);

    [Header("Outline Colours")]
    [SerializeField] private Color normalOutlineColour = new Color32(0x60, 0x60, 0x60, 255);
    [SerializeField] private Color highlightedOutlineColour = new Color32(0xDA, 0x70, 0x00, 255);
    [SerializeField] private Color pressedOutlineColour = new Color32(0xFF, 0x84, 0x00, 255);

    private bool isHovered;
    private bool isPressed;
    private bool isSelected;

    private void Awake()
    {
        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyCurrentState();
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
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

        if (isSelected || isHovered)
        {
            ApplyHighlightedState();
            return;
        }

        ApplyNormalState();
    }

    private void ApplyNormalState()
    {
        if (targetPanel != null)
        {
            targetPanel.color = normalPanelColour;
        }

        if (targetText != null)
        {
            targetText.color = normalTextColour;
        }

        if (targetOutline != null)
        {
            targetOutline.effectColor = normalOutlineColour;
        }
    }

    private void ApplyHighlightedState()
    {
        if (targetPanel != null)
        {
            targetPanel.color = highlightedPanelColour;
        }

        if (targetText != null)
        {
            targetText.color = highlightedTextColour;
        }

        if (targetOutline != null)
        {
            targetOutline.effectColor = highlightedOutlineColour;
        }
    }

    private void ApplyPressedState()
    {
        if (targetPanel != null)
        {
            targetPanel.color = pressedPanelColour;
        }

        if (targetText != null)
        {
            targetText.color = pressedTextColour;
        }

        if (targetOutline != null)
        {
            targetOutline.effectColor = pressedOutlineColour;
        }
    }
}