using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RRNContractsPanel : MonoBehaviour
{
	[Header("Details Visibility")]
	[SerializeField] private List<GameObject> detailObjectsToHideWhenEmpty = new();
	
    [Header("References")]
    [SerializeField] private DailyContractBoard dailyContractBoard;
    [SerializeField] private RRNServerSelectionPanel serverSelectionPanel;
	
	[Header("Counters")]
	[SerializeField] private TextMeshProUGUI contractsCounterText;
	
	[Header("Buttons")]
	[SerializeField] private Button refreshContractsButton;
    [Header("Start Button")]
    [SerializeField] private Button startResearchButton;

    [Header("Contract Entry Parent")]
    [SerializeField] private Transform contractEntriesParent;

    [Header("Settings")]
    [SerializeField] private float startupDelaySeconds = 1f;
    [SerializeField] private int maxVisibleContracts = 3;

    [Header("Details Panel Text")]
    [SerializeField] private TextMeshProUGUI detailTitleText;
    [SerializeField] private TextMeshProUGUI detailClientText;
    [SerializeField] private TextMeshProUGUI detailDescriptionText;
    [SerializeField] private TextMeshProUGUI detailDurationText;
    [SerializeField] private TextMeshProUGUI detailMoneyText;
    [SerializeField] private TextMeshProUGUI detailResearchPointsText;

    [Header("Details Panel Logo")]
    [SerializeField] private Image detailClientLogoImage;
	
	[Header("Start Button Feedback")]
	[SerializeField] private Graphic startButtonBackground;
	[SerializeField] private Outline startButtonOutline;
	[SerializeField] private TextMeshProUGUI startButtonText;
	[SerializeField] private TextMeshProUGUI startButtonSymbolText;

	[SerializeField] private Color startNormalBackgroundColour = new Color32(0x16, 0x1C, 0x16, 255);
	[SerializeField] private Color startHighlightedBackgroundColour = new Color32(0x26, 0x22, 0x12, 255);
	[SerializeField] private Color startPressedBackgroundColour = new Color32(0x3A, 0x25, 0x05, 255);
	[SerializeField] private Color startDisabledBackgroundColour = new Color32(0x10, 0x12, 0x10, 255);

	[SerializeField] private Color startNormalTextColour = new Color32(0xD6, 0xD6, 0xD6, 255);
	[SerializeField] private Color startHighlightedTextColour = new Color32(0xFF, 0x84, 0x00, 255);
	[SerializeField] private Color startDisabledTextColour = new Color32(0x70, 0x70, 0x70, 255);

	[SerializeField] private Color startNormalOutlineColour = new Color32(0x60, 0x60, 0x60, 255);
	[SerializeField] private Color startHighlightedOutlineColour = new Color32(0xDA, 0x70, 0x00, 255);
	[SerializeField] private Color startPressedOutlineColour = new Color32(0xFF, 0x84, 0x00, 255);
	[SerializeField] private Color startDisabledOutlineColour = new Color32(0x30, 0x30, 0x30, 255);

	private bool isStartButtonHovered;
	private bool isStartButtonPressed;

    private RRNContractEntryUI[] contractEntries;
    private RRNContractEntryUI selectedEntry;
    private ResearchContract selectedContract;

private IEnumerator Start()
{
    if (dailyContractBoard == null)
    {
        dailyContractBoard = FindAnyObjectByType<DailyContractBoard>();
    }

    if (serverSelectionPanel == null)
    {
        serverSelectionPanel = FindAnyObjectByType<RRNServerSelectionPanel>();
    }

    if (startResearchButton != null)
    {
        startResearchButton.onClick.RemoveListener(StartSelectedResearch);
        startResearchButton.onClick.AddListener(StartSelectedResearch);
    }

    if (refreshContractsButton != null)
    {
        refreshContractsButton.onClick.AddListener(OnRefreshContractsPressed);
    }

    yield return new WaitForSeconds(startupDelaySeconds);

    CacheContractEntries();
    PopulateContractEntries();
	SetupStartButtonFeedbackEvents();
    ClearContractSelection();
    RefreshStartButtonState();

    yield return null;
}

    private void Update()
    {
        RefreshStartButtonState();
    }

private void ClearContractSelection()
{
    selectedEntry = null;
    selectedContract = null;

    if (contractEntries != null)
    {
        foreach (RRNContractEntryUI entry in contractEntries)
        {
            if (entry != null)
            {
                entry.SetSelected(false);
            }
        }
    }

    ClearDetailsPanel();
    RefreshStartButtonState();
}

    private void CacheContractEntries()
    {
        if (contractEntriesParent == null)
        {
            contractEntriesParent = transform;
        }

        RRNContractEntryUI[] foundEntries =
            contractEntriesParent.GetComponentsInChildren<RRNContractEntryUI>(true);

        int entryCount = Mathf.Min(foundEntries.Length, maxVisibleContracts);
        contractEntries = new RRNContractEntryUI[entryCount];

        for (int i = 0; i < entryCount; i++)
        {
            contractEntries[i] = foundEntries[i];
        }
    }

    public void PopulateContractEntries()
    {
        if (dailyContractBoard == null)
        {
            Debug.LogWarning("RRNContractsPanel has no DailyContractBoard assigned.");
            return;
        }

        if (contractEntries == null || contractEntries.Length == 0)
        {
            Debug.LogWarning("RRNContractsPanel found no contract entry buttons.");
            return;
        }

        List<ResearchContract> contracts = dailyContractBoard.CurrentContracts;

        for (int i = 0; i < contractEntries.Length; i++)
        {
            ResearchContract contract = null;

            if (contracts != null && i < contracts.Count)
            {
                contract = contracts[i];
            }

            contractEntries[i].Setup(contract, HandleContractEntryClicked);
            contractEntries[i].SetSelected(false);
			
        }
		RefreshContractsCounter();
    }

    private void SelectFirstAvailableContract()
    {
        if (contractEntries == null)
        {
            ClearDetailsPanel();
            return;
        }

        for (int i = 0; i < contractEntries.Length; i++)
        {
            ResearchContract contract = contractEntries[i].Contract;

            if (contract != null && !contract.isAssigned && !contract.isCompleted)
            {
                SelectContractEntry(contractEntries[i]);
                return;
            }
        }

        ClearDetailsPanel();
    }

   private void HandleContractEntryClicked(RRNContractEntryUI clickedEntry)
{
    UISoundManager.Instance?.PlayConfirm();
    SelectContractEntry(clickedEntry);
}

    private void SelectContractEntry(RRNContractEntryUI entry)
    {
        selectedEntry = entry;
        selectedContract = entry.Contract;

        for (int i = 0; i < contractEntries.Length; i++)
        {
            if (contractEntries[i] != null)
            {
                contractEntries[i].SetSelected(contractEntries[i] == selectedEntry);
            }
        }

        RefreshDetailsPanel();
        RefreshStartButtonState();
		
    }

    private void RefreshDetailsPanel()
	{
    if (selectedContract == null)
    {
        ClearDetailsPanel();
        return;
    }

    // Show all the detail objects now that we have a contract selected.
    SetDetailsObjectsVisible(true);

    if (detailTitleText != null)
    {
        detailTitleText.text = selectedContract.contractName;
    }

    if (detailClientText != null)
    {
        detailClientText.text = selectedContract.clientName;
    }

    if (detailDescriptionText != null)
    {
        detailDescriptionText.text = selectedContract.description;
    }

    if (detailDurationText != null)
    {
        detailDurationText.text = selectedContract.GetDurationText();
    }

    if (detailMoneyText != null)
    {
        detailMoneyText.text = $"£ {selectedContract.rewardMoney:N0}";
    }

    if (detailResearchPointsText != null)
    {
        detailResearchPointsText.text = selectedContract.rewardResearchPoints.ToString("N0");
    }

    if (detailClientLogoImage != null)
    {
        detailClientLogoImage.sprite = selectedContract.clientLogo;
        detailClientLogoImage.enabled = selectedContract.clientLogo != null;
    }
}

	private void ClearDetailsPanel()
	{
    selectedEntry = null;
    selectedContract = null;

    // Hide all the detail objects.
    SetDetailsObjectsVisible(false);

    if (detailTitleText != null) detailTitleText.text = "";
    if (detailClientText != null) detailClientText.text = "";
    if (detailDescriptionText != null) detailDescriptionText.text = "";
    if (detailDurationText != null) detailDurationText.text = "";
    if (detailMoneyText != null) detailMoneyText.text = "";
    if (detailResearchPointsText != null) detailResearchPointsText.text = "";

    if (detailClientLogoImage != null)
    {
        detailClientLogoImage.sprite = null;
        detailClientLogoImage.enabled = false;
    }
}
 
private void RefreshStartButtonState()
{
    if (startResearchButton == null)
    {
        return;
    }

    ResearchServer selectedServer = null;

    if (serverSelectionPanel != null)
    {
        selectedServer = serverSelectionPanel.GetSelectedServer();
    }

    bool canStart =
        selectedContract != null &&
        selectedServer != null &&
        selectedServer.CanAcceptContract();

    startResearchButton.interactable = canStart;

    ApplyStartButtonFeedback();
}

public void StartSelectedResearch()
{
    if (selectedContract == null || serverSelectionPanel == null)
    {
        UISoundManager.Instance?.PlayDenied();
        return;
    }

    ResearchServer selectedServer = serverSelectionPanel.GetSelectedServer();

    if (selectedServer == null || !selectedServer.CanAcceptContract())
    {
        UISoundManager.Instance?.PlayDenied();
        return;
    }

    bool assignedSuccessfully = selectedServer.AssignContract(selectedContract);

    if (!assignedSuccessfully)
    {
        UISoundManager.Instance?.PlayDenied();
        return;
    }

    UISoundManager.Instance?.PlaySuccess();

    if (dailyContractBoard != null)
    {
        dailyContractBoard.RemoveContract(selectedContract);
    }

    PopulateContractEntries();

    if (serverSelectionPanel != null)
    {
        serverSelectionPanel.RefreshServerList();
    }

    RefreshStartButtonState();
    ClearContractSelection();
    RefreshContractsCounter();
}

    public ResearchContract GetSelectedContract()
    {
        return selectedContract;
    }

	private void SetDetailsObjectsVisible(bool visible)
{
    for (int i = 0; i < detailObjectsToHideWhenEmpty.Count; i++)
    {
        if (detailObjectsToHideWhenEmpty[i] != null)
        {
            detailObjectsToHideWhenEmpty[i].SetActive(visible);
        }
    }
}

	public void OnRefreshContractsPressed()
{
    if (dailyContractBoard == null)
        return;

    // Generate a fresh set of contracts.
    dailyContractBoard.RefreshContracts();

    // Clear the current selection.
    ClearContractSelection();

    // Repopulate the buttons.
    PopulateContractEntries();

    // Refresh the available servers.
    if (serverSelectionPanel != null)
    {
        serverSelectionPanel.RefreshServerList();
    }

    // Refresh the start button state.
    RefreshStartButtonState();
	RefreshContractsCounter();
}

	private void RefreshContractsCounter()
{
    if (contractsCounterText == null)
        return;

    int currentContracts = 0;

    if (dailyContractBoard != null)
    {
        currentContracts = dailyContractBoard.GetAvailableContractCount();
    }

    contractsCounterText.text = $"({currentContracts}/{maxVisibleContracts})";
}

private void SetupStartButtonFeedbackEvents()
{
    if (startResearchButton == null)
    {
        return;
    }

    EventTrigger trigger = startResearchButton.GetComponent<EventTrigger>();

    if (trigger == null)
    {
        trigger = startResearchButton.gameObject.AddComponent<EventTrigger>();
    }

    trigger.triggers.Clear();

    AddStartButtonEvent(trigger, EventTriggerType.PointerEnter, OnStartButtonPointerEnter);
    AddStartButtonEvent(trigger, EventTriggerType.PointerExit, OnStartButtonPointerExit);
    AddStartButtonEvent(trigger, EventTriggerType.PointerDown, OnStartButtonPointerDown);
    AddStartButtonEvent(trigger, EventTriggerType.PointerUp, OnStartButtonPointerUp);
}

private void AddStartButtonEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> callback)
{
    EventTrigger.Entry entry = new EventTrigger.Entry();
    entry.eventID = eventType;
    entry.callback.AddListener(callback);
    trigger.triggers.Add(entry);
}

private void OnStartButtonPointerEnter(BaseEventData eventData)
{
    isStartButtonHovered = true;

    if (startResearchButton != null && startResearchButton.interactable)
    {
        UISoundManager.Instance?.PlayHover();
    }

    ApplyStartButtonFeedback();
}
private void OnStartButtonPointerExit(BaseEventData eventData)
{
    isStartButtonHovered = false;
    isStartButtonPressed = false;
    ApplyStartButtonFeedback();
}

private void OnStartButtonPointerDown(BaseEventData eventData)
{
    isStartButtonPressed = true;
    ApplyStartButtonFeedback();
}

private void OnStartButtonPointerUp(BaseEventData eventData)
{
    isStartButtonPressed = false;
    ApplyStartButtonFeedback();
}

private void ApplyStartButtonFeedback()
{
    if (startResearchButton == null)
    {
        return;
    }

    if (!startResearchButton.interactable)
    {
        SetStartButtonColours(
            startDisabledBackgroundColour,
            startDisabledOutlineColour,
            startDisabledTextColour
        );

        return;
    }

    if (isStartButtonPressed)
    {
        SetStartButtonColours(
            startPressedBackgroundColour,
            startPressedOutlineColour,
            startHighlightedTextColour
        );

        return;
    }

    if (isStartButtonHovered)
    {
        SetStartButtonColours(
            startHighlightedBackgroundColour,
            startHighlightedOutlineColour,
            startHighlightedTextColour
        );

        return;
    }

    SetStartButtonColours(
        startNormalBackgroundColour,
        startNormalOutlineColour,
        startNormalTextColour
    );
}

private void SetStartButtonColours(Color backgroundColour, Color outlineColour, Color textColour)
{
    if (startButtonBackground != null)
    {
        startButtonBackground.color = backgroundColour;
    }

    if (startButtonOutline != null)
    {
        startButtonOutline.effectColor = outlineColour;
    }

    if (startButtonText != null)
    {
        startButtonText.color = textColour;
    }

    if (startButtonSymbolText != null)
    {
        startButtonSymbolText.color = textColour;
    }
}









}