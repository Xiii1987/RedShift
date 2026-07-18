using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RRNContractEntryUI : MonoBehaviour,
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

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI companyText;
    [SerializeField] private TextMeshProUGUI arrowText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI rpTitleText;
    [SerializeField] private TextMeshProUGUI rpPointsText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Background Colours")]
    [SerializeField] private Color normalBackgroundColour = new Color32(0x16, 0x1C, 0x16, 255);
    [SerializeField] private Color highlightedBackgroundColour = new Color32(0x26, 0x22, 0x12, 255);
    [SerializeField] private Color selectedBackgroundColour = new Color32(0x2C, 0x23, 0x0E, 255);
    [SerializeField] private Color pressedBackgroundColour = new Color32(0x3A, 0x25, 0x05, 255);
    [SerializeField] private Color disabledBackgroundColour = new Color32(0x10, 0x12, 0x10, 255);

    [Header("Text Colours")]
    [SerializeField] private Color normalTextColour = new Color32(0xD6, 0xD6, 0xD6, 255);
    [SerializeField] private Color highlightedTextColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color disabledTextColour = new Color32(0x70, 0x70, 0x70, 255);

    [Header("Outline Colours")]
    [SerializeField] private Color normalOutlineColour = new Color32(0x60, 0x60, 0x60, 255);
    [SerializeField] private Color highlightedOutlineColour = new Color32(0xDA, 0x70, 0x00, 255);
    [SerializeField] private Color pressedOutlineColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color disabledOutlineColour = new Color32(0x30, 0x30, 0x30, 255);

    private ResearchContract contract;
    private Action<RRNContractEntryUI> clickedCallback;

    private bool isHovered;
    private bool isPressed;
    private bool isSelected;
    private bool isDisabled;

    public ResearchContract Contract => contract;

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

    public void Setup(ResearchContract newContract, Action<RRNContractEntryUI> onClicked)
    {
        contract = newContract;
        clickedCallback = onClicked;

        bool hasContract = contract != null;
        gameObject.SetActive(hasContract);

        if (!hasContract)
        {
            return;
        }

        RefreshDisplay();

        isDisabled = contract.isAssigned || contract.isCompleted;

        if (button != null)
        {
            button.interactable = !isDisabled;
        }

        ApplyCurrentState();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyCurrentState();
    }

    private void RefreshDisplay()
    {
        if (contract == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = contract.contractName;
        }

        if (companyText != null)
        {
            companyText.text = contract.clientName;
        }

        if (arrowText != null)
        {
            arrowText.text = ">";
        }

        if (moneyText != null)
        {
            moneyText.text = $"£ {contract.rewardMoney:N0}";
        }

        if (rpTitleText != null)
        {
            rpTitleText.text = "RP";
        }

        if (rpPointsText != null)
        {
            rpPointsText.text = contract.rewardResearchPoints.ToString();
        }

        if (timeText != null)
        {
            timeText.text = contract.GetDurationText();
        }
    }

    private void HandleClicked()
    {
        if (isDisabled)
        {
            return;
        }

        clickedCallback?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
		if (!isDisabled)
    {
        UISoundManager.Instance?.PlayHover();
    }
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
        if (isDisabled)
        {
            ApplyDisabledState();
            return;
        }

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
        SetAllTextColour(normalTextColour);
        SetMainHighlightTextColour(normalTextColour);
    }

    private void ApplyHighlightedState()
    {
        SetBackgroundColour(highlightedBackgroundColour);
        SetOutlineColour(highlightedOutlineColour);
        SetAllTextColour(normalTextColour);
        SetMainHighlightTextColour(highlightedTextColour);
    }

    private void ApplySelectedState()
    {
        SetBackgroundColour(selectedBackgroundColour);
        SetOutlineColour(highlightedOutlineColour);
        SetAllTextColour(normalTextColour);
        SetMainHighlightTextColour(highlightedTextColour);
    }

    private void ApplyPressedState()
    {
        SetBackgroundColour(pressedBackgroundColour);
        SetOutlineColour(pressedOutlineColour);
        SetAllTextColour(normalTextColour);
        SetMainHighlightTextColour(highlightedTextColour);
    }

    private void ApplyDisabledState()
    {
        SetBackgroundColour(disabledBackgroundColour);
        SetOutlineColour(disabledOutlineColour);
        SetAllTextColour(disabledTextColour);
    }

    private void SetBackgroundColour(Color colour)
    {
        if (background != null)
        {
            background.color = colour;
        }
    }

    private void SetOutlineColour(Color colour)
    {
        if (outline != null)
        {
            outline.effectColor = colour;
        }
    }

    private void SetAllTextColour(Color colour)
    {
        if (titleText != null) titleText.color = colour;
        if (companyText != null) companyText.color = colour;
        if (arrowText != null) arrowText.color = colour;
        if (moneyText != null) moneyText.color = colour;
        if (rpTitleText != null) rpTitleText.color = colour;
        if (rpPointsText != null) rpPointsText.color = colour;
        if (timeText != null) timeText.color = colour;
    }

    private void SetMainHighlightTextColour(Color colour)
    {
        if (titleText != null)
        {
            titleText.color = colour;
        }

        if (arrowText != null)
        {
            arrowText.color = colour;
        }
    }
}