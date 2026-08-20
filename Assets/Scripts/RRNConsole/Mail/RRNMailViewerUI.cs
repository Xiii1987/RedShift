using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RRNMailViewerUI : MonoBehaviour
{
    [Header("Title Panel")]
    [SerializeField] private TMP_Text fromText;
    [SerializeField] private TMP_Text toText;
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private Image departmentIcon;

    [Header("Content Panel")]
    [SerializeField] private TMP_Text contentPanelText;

    [Header("Response Panel")]
    [SerializeField] private GameObject responsePanel;
    [SerializeField] private RRNMailResponseButtonUI[] responseButtons;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button archiveButton;

    private RRNReceivedEmail currentEmail;
    private RRNEmailResponseDefinition pendingResponse;
    private Action<RRNEmailResponseDefinition> confirmedResponseCallback;
    private Action archiveCallback;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmResponse);

        if (archiveButton != null)
            archiveButton.onClick.AddListener(ArchiveCurrentEmail);

        ClearDisplay();
    }

    public void DisplayEmail(
        RRNReceivedEmail email,
        Action<RRNEmailResponseDefinition> responseCallback,
        Action onArchive)
    {
        if (email == null || email.Definition == null)
            return;

        currentEmail = email;
        confirmedResponseCallback = responseCallback;
        archiveCallback = onArchive;
        pendingResponse = null;

        RRNEmailDefinition data = email.Definition;

        string playerName = PlayerManager.Instance != null
            ? PlayerManager.Instance.PlayerName
            : "Playername";

        if (fromText != null)
            fromText.text = data.sender;

        if (toText != null)
            toText.text = playerName;

        if (subjectText != null)
            subjectText.text = data.subject;

        if (typeText != null)
            typeText.text = data.departmentName;

        if (dateText != null)
            dateText.text = $"{email.Date} {email.Time}";

        if (departmentIcon != null)
        {
            departmentIcon.sprite = data.departmentIcon;
            departmentIcon.enabled = data.departmentIcon != null;
        }

        if (contentPanelText != null)
        {
            string body = data.body ?? string.Empty;
            body = body.Replace("{PLAYER}", playerName);
            contentPanelText.text = body;
        }

        SetupResponses(email);
        RefreshActionButtons();
    }

    private void SetupResponses(RRNReceivedEmail email)
    {
        bool hasResponses =
            email.Definition.responses != null &&
            email.Definition.responses.Count > 0;

        if (responsePanel != null)
            responsePanel.SetActive(true);

        if (responseButtons == null)
            return;

        for (int i = 0; i < responseButtons.Length; i++)
        {
            RRNMailResponseButtonUI button = responseButtons[i];

            if (button == null)
                continue;

            if (!hasResponses || i >= email.Definition.responses.Count)
            {
                button.gameObject.SetActive(false);
                continue;
            }

            RRNEmailResponseDefinition response = email.Definition.responses[i];
            button.Setup(response, HandleResponseSelected);

            if (email.HasResponded)
            {
                bool chosen = response == email.SelectedResponse;

                if (chosen)
                {
                    button.gameObject.SetActive(true);
                    button.SetLocked(true, true);
                }
                else
                {
                    button.gameObject.SetActive(false);
                }
            }
            else
            {
                button.gameObject.SetActive(true);
                button.SetSelected(false);
            }
        }
    }

    private void HandleResponseSelected(RRNEmailResponseDefinition response)
    {
        if (currentEmail == null || currentEmail.HasResponded || response == null)
            return;

        pendingResponse = response;

        foreach (RRNMailResponseButtonUI button in responseButtons)
        {
            if (button == null || !button.gameObject.activeSelf)
                continue;

            button.SetSelected(button.Response == pendingResponse);
        }

        RefreshActionButtons();
    }

    private void ConfirmResponse()
    {
        if (currentEmail == null || currentEmail.HasResponded || pendingResponse == null)
            return;

        UISoundManager.Instance?.PlayConfirm();
        confirmedResponseCallback?.Invoke(pendingResponse);
    }

    private void ArchiveCurrentEmail()
    {
        if (currentEmail == null || currentEmail.IsArchived || !CanArchiveCurrentEmail())
            return;

        UISoundManager.Instance?.PlayConfirm();
        archiveCallback?.Invoke();
        ClearDisplay();
    }

    public void LockResponses(RRNReceivedEmail email)
    {
        if (email == null || responseButtons == null)
            return;

        pendingResponse = null;

        foreach (RRNMailResponseButtonUI button in responseButtons)
        {
            if (button == null)
                continue;

            bool chosen = button.Response == email.SelectedResponse;

            if (chosen)
            {
                button.gameObject.SetActive(true);
                button.SetLocked(true, true);
            }
            else
            {
                button.gameObject.SetActive(false);
            }
        }

        RefreshActionButtons();
    }

    public void ClearDisplay()
    {
        currentEmail = null;
        pendingResponse = null;
        confirmedResponseCallback = null;
        archiveCallback = null;

        if (fromText != null)
            fromText.text = string.Empty;

        if (toText != null)
            toText.text = string.Empty;

        if (subjectText != null)
            subjectText.text = string.Empty;

        if (typeText != null)
            typeText.text = string.Empty;

        if (dateText != null)
            dateText.text = string.Empty;

        if (contentPanelText != null)
            contentPanelText.text = string.Empty;

        if (departmentIcon != null)
        {
            departmentIcon.sprite = null;
            departmentIcon.enabled = false;
        }

        if (responseButtons != null)
        {
            foreach (RRNMailResponseButtonUI button in responseButtons)
            {
                if (button != null)
                    button.gameObject.SetActive(false);
            }
        }

        RefreshActionButtons();
    }

    private bool CanArchiveCurrentEmail()
    {
        if (currentEmail == null || currentEmail.Definition == null)
            return false;

        bool hasResponses =
            currentEmail.Definition.responses != null &&
            currentEmail.Definition.responses.Count > 0;

        return !hasResponses || currentEmail.HasResponded;
    }

    private void RefreshActionButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable =
                currentEmail != null &&
                !currentEmail.HasResponded &&
                pendingResponse != null;
        }

        if (archiveButton != null)
        {
            archiveButton.interactable =
                currentEmail != null &&
                !currentEmail.IsArchived &&
                CanArchiveCurrentEmail();
        }
    }
}
