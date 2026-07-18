using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

public class RRNConsoleUI : MonoBehaviour
{
    public enum ConsoleSection
    {
        Contracts,
        ActiveJobs,
        ResearchPrograms,
        Patents,
        Mail
    }

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI researchPointsText;
    [SerializeField] private Button closeButton;
	[SerializeField] private StarterAssetsInputs playerInput;

	[Header("Top Bar Flash")]
	[SerializeField] private Color normalMoneyColour = new Color32(0xD6, 0xD6, 0xD6, 255);
	[SerializeField] private Color moneyFlashColour = new Color32(0x6F, 0xD1, 0x00, 255);
	[SerializeField] private float moneyFlashSeconds = 1f;

	private Coroutine moneyFlashRoutine;


    [Header("Tab Buttons")]
    [SerializeField] private Button contractsButton;
    [SerializeField] private Button activeJobsButton;
    [SerializeField] private Button researchProgramsButton;
    [SerializeField] private Button patentsButton;
    [SerializeField] private Button mailButton;

    [Header("Section Parents")]
    [SerializeField] private GameObject contractsSection;
    [SerializeField] private GameObject activeJobsSection;
    [SerializeField] private GameObject researchProgramsSection;
    [SerializeField] private GameObject patentsSection;
    [SerializeField] private GameObject mailSection;

    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Settings")]
    [SerializeField] private ConsoleSection startingSection = ConsoleSection.Contracts;

    [Header("Transition Settings")]
    [SerializeField] private bool useTransitions = true;
    [SerializeField] private float transitionDuration = 0.12f;
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.98f, 0.98f, 1f);
    [SerializeField] private Vector3 visibleScale = Vector3.one;

	[Header("Server Refresh")]
	[SerializeField] private RRNServerSelectionPanel serverSelectionPanel;


    private ConsoleSection currentSection;
    private Coroutine transitionRoutine;
    private bool isTransitioning;

    private void Awake()
    {
        EnsureCanvasGroups();
        SetupButtons();
    }

    private void Start()
    {
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }

        RefreshTopBar();

        SetupStartingSection();
        SelectStartingButton();
    }

    private void Update()
    {
        RefreshTopBar();
    }

    private void EnsureCanvasGroups()
    {
        EnsureCanvasGroup(contractsSection);
        EnsureCanvasGroup(activeJobsSection);
        EnsureCanvasGroup(researchProgramsSection);
        EnsureCanvasGroup(patentsSection);
        EnsureCanvasGroup(mailSection);
    }

    private CanvasGroup EnsureCanvasGroup(GameObject sectionObject)
    {
        if (sectionObject == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = sectionObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = sectionObject.AddComponent<CanvasGroup>();
        }

        sectionObject.SetActive(true);

        return canvasGroup;
    }

    private void SetupButtons()
    {
        if (contractsButton != null) contractsButton.onClick.AddListener(ShowContracts);
        if (activeJobsButton != null) activeJobsButton.onClick.AddListener(ShowActiveJobs);
        if (researchProgramsButton != null) researchProgramsButton.onClick.AddListener(ShowResearchPrograms);
        if (patentsButton != null) patentsButton.onClick.AddListener(ShowPatents);
        if (mailButton != null) mailButton.onClick.AddListener(ShowMail);
        if (closeButton != null) closeButton.onClick.AddListener(CloseConsole);
    }

    private void RefreshTopBar()
    {
        if (playerManager == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = $"£ {playerManager.Money:N0}";
        }

        if (researchPointsText != null)
        {
            researchPointsText.text = $"RP {playerManager.ResearchPoints:N0}";
        }
    }

    private void SetupStartingSection()
    {
        currentSection = startingSection;

        SetSectionInstant(contractsSection, startingSection == ConsoleSection.Contracts);
        SetSectionInstant(activeJobsSection, startingSection == ConsoleSection.ActiveJobs);
        SetSectionInstant(researchProgramsSection, startingSection == ConsoleSection.ResearchPrograms);
        SetSectionInstant(patentsSection, startingSection == ConsoleSection.Patents);
        SetSectionInstant(mailSection, startingSection == ConsoleSection.Mail);
    }

    private void SetSectionInstant(GameObject sectionObject, bool visible)
    {
        if (sectionObject == null)
        {
            return;
        }

        sectionObject.SetActive(true);

        CanvasGroup canvasGroup = EnsureCanvasGroup(sectionObject);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        sectionObject.transform.localScale = visible ? visibleScale : hiddenScale;
    }

    private void SelectStartingButton()
    {
        Button buttonToSelect = GetButtonForSection(startingSection);

        if (buttonToSelect != null)
        {
            buttonToSelect.Select();
        }
    }

    private Button GetButtonForSection(ConsoleSection section)
    {
        switch (section)
        {
            case ConsoleSection.Contracts:
                return contractsButton;
            case ConsoleSection.ActiveJobs:
                return activeJobsButton;
            case ConsoleSection.ResearchPrograms:
                return researchProgramsButton;
            case ConsoleSection.Patents:
                return patentsButton;
            case ConsoleSection.Mail:
                return mailButton;
            default:
                return null;
        }
    }

    private GameObject GetSectionObject(ConsoleSection section)
    {
        switch (section)
        {
            case ConsoleSection.Contracts:
                return contractsSection;
            case ConsoleSection.ActiveJobs:
                return activeJobsSection;
            case ConsoleSection.ResearchPrograms:
                return researchProgramsSection;
            case ConsoleSection.Patents:
                return patentsSection;
            case ConsoleSection.Mail:
                return mailSection;
            default:
                return null;
        }
    }

   public void ShowSection(ConsoleSection newSection)
{
    if (newSection == currentSection || isTransitioning)
    {
        return;
    }

    UISoundManager.Instance?.PlayTabChange();

    if (!useTransitions)
    {
        ShowSectionInstant(newSection);
        return;
    }

    if (transitionRoutine != null)
    {
        StopCoroutine(transitionRoutine);
    }

    transitionRoutine = StartCoroutine(SwitchSectionRoutine(newSection));
}
    private void ShowSectionInstant(ConsoleSection newSection)
    {
        currentSection = newSection;

        SetSectionInstant(contractsSection, newSection == ConsoleSection.Contracts);
        SetSectionInstant(activeJobsSection, newSection == ConsoleSection.ActiveJobs);
        SetSectionInstant(researchProgramsSection, newSection == ConsoleSection.ResearchPrograms);
        SetSectionInstant(patentsSection, newSection == ConsoleSection.Patents);
        SetSectionInstant(mailSection, newSection == ConsoleSection.Mail);
    }

    private IEnumerator SwitchSectionRoutine(ConsoleSection newSection)
    {
        isTransitioning = true;

        GameObject oldSectionObject = GetSectionObject(currentSection);
        GameObject newSectionObject = GetSectionObject(newSection);

        CanvasGroup oldCanvasGroup = EnsureCanvasGroup(oldSectionObject);
        CanvasGroup newCanvasGroup = EnsureCanvasGroup(newSectionObject);

        if (newSectionObject != null)
        {
            newSectionObject.SetActive(true);
            newSectionObject.transform.localScale = hiddenScale;
        }

        if (newCanvasGroup != null)
        {
            newCanvasGroup.alpha = 0f;
            newCanvasGroup.interactable = false;
            newCanvasGroup.blocksRaycasts = false;
        }

        if (oldCanvasGroup != null)
        {
            oldCanvasGroup.interactable = false;
            oldCanvasGroup.blocksRaycasts = false;
        }

        yield return FadeAndScaleSectionsRoutine(
            oldSectionObject,
            oldCanvasGroup,
            newSectionObject,
            newCanvasGroup,
            transitionDuration
        );

        if (oldCanvasGroup != null)
        {
            oldCanvasGroup.alpha = 0f;
            oldCanvasGroup.interactable = false;
            oldCanvasGroup.blocksRaycasts = false;
        }

        if (oldSectionObject != null)
        {
            oldSectionObject.transform.localScale = hiddenScale;
        }

        if (newCanvasGroup != null)
        {
            newCanvasGroup.alpha = 1f;
            newCanvasGroup.interactable = true;
            newCanvasGroup.blocksRaycasts = true;
        }

        if (newSectionObject != null)
        {
            newSectionObject.transform.localScale = visibleScale;
        }

        currentSection = newSection;
        isTransitioning = false;
    }

    private IEnumerator FadeAndScaleSectionsRoutine(
        GameObject oldSectionObject,
        CanvasGroup oldCanvasGroup,
        GameObject newSectionObject,
        CanvasGroup newCanvasGroup,
        float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsedTime / duration);
            float smoothedProgress = SmoothStep(progress);

            if (oldCanvasGroup != null)
            {
                oldCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothedProgress);
            }

            if (newCanvasGroup != null)
            {
                newCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothedProgress);
            }

            if (oldSectionObject != null)
            {
                oldSectionObject.transform.localScale =
                    Vector3.Lerp(visibleScale, hiddenScale, smoothedProgress);
            }

            if (newSectionObject != null)
            {
                newSectionObject.transform.localScale =
                    Vector3.Lerp(hiddenScale, visibleScale, smoothedProgress);
            }

            yield return null;
        }
    }

    private float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    public void ShowContracts()
    {
        ShowSection(ConsoleSection.Contracts);
    }

    public void ShowActiveJobs()
    {
        ShowSection(ConsoleSection.ActiveJobs);
    }

    public void ShowResearchPrograms()
    {
        ShowSection(ConsoleSection.ResearchPrograms);
    }

    public void ShowPatents()
    {
        ShowSection(ConsoleSection.Patents);
    }

    public void ShowMail()
    {
        ShowSection(ConsoleSection.Mail);
    }

public void OpenConsole()
{
    CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

    if (canvasGroup == null)
    {
        return;
    }

    canvasGroup.alpha = 1f;
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;

    if (serverSelectionPanel == null)
    {
        serverSelectionPanel = GetComponentInChildren<RRNServerSelectionPanel>(true);
    }

    if (serverSelectionPanel != null)
    {
        serverSelectionPanel.RefreshServerList();
    }

    RedshiftPlayerStateController.Instance?.EnterUI();
}
public void CloseConsole()
{
    CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

    if (canvasGroup == null)
        return;

    canvasGroup.alpha = 0f;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;

    RedshiftPlayerStateController.Instance?.EnterGameplay();
}

private void OnEnable()
{
    ResearchServer.OnAnyContractCompleted += FlashMoneyText;
}

private void OnDisable()
{
    ResearchServer.OnAnyContractCompleted -= FlashMoneyText;
}

private void FlashMoneyText()
{
    if (moneyText == null)
        return;

    if (moneyFlashRoutine != null)
    {
        StopCoroutine(moneyFlashRoutine);
    }

    moneyFlashRoutine = StartCoroutine(MoneyFlashRoutine());
}

private IEnumerator MoneyFlashRoutine()
{
    moneyText.color = moneyFlashColour;

    yield return new WaitForSeconds(moneyFlashSeconds);

    moneyText.color = normalMoneyColour;
}







}