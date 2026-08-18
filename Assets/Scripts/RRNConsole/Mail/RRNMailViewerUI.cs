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

    public void DisplayEmail(
        RRNReceivedEmail email,
        Action<RRNEmailResponseDefinition> responseCallback)
    {
        if (email == null || email.Definition == null)
            return;

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

        SetupResponses(email, responseCallback);
    }

    private void SetupResponses(
        RRNReceivedEmail email,
        Action<RRNEmailResponseDefinition> responseCallback)
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
            if (responseButtons[i] == null)
                continue;

            if (hasResponses && i < email.Definition.responses.Count)
            {
                RRNEmailResponseDefinition response = email.Definition.responses[i];

                responseButtons[i].Setup(response, responseCallback);
                responseButtons[i].SetLocked(
                    email.HasResponded,
                    email.HasResponded && response == email.SelectedResponse);
            }
            else
            {
                responseButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void LockResponses(RRNReceivedEmail email)
    {
        if (email == null || responseButtons == null)
            return;

        for (int i = 0; i < responseButtons.Length; i++)
        {
            if (responseButtons[i] == null || !responseButtons[i].gameObject.activeSelf)
                continue;

            bool chosen =
                email.Definition.responses != null &&
                i < email.Definition.responses.Count &&
                email.Definition.responses[i] == email.SelectedResponse;

            responseButtons[i].SetLocked(true, chosen);
        }
    }
}
