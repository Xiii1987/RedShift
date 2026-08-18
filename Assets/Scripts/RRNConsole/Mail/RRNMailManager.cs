using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RRNMailManager : MonoBehaviour
{
    public static event Action<string> OnProgrammeAccepted;

    [Header("Database")]
    [SerializeField] private RRNEmailDatabase emailDatabase;

    [Header("Inbox")]
    [SerializeField] private Transform unreadContent;
    [SerializeField] private RectTransform archivedContent;
    [SerializeField] private RRNEmailEntryUI emailEntryPrefab;

    [Header("Archive")]
    [SerializeField] private Button archivedMailButton;
    [SerializeField] private TMP_Text archivedMailButtonText;
    [SerializeField] private float archiveFadeDuration = 0.2f;

    [Header("Counters")]
    [SerializeField] private TMP_Text unreadTitle;
    [SerializeField] private TMP_Text totalTitle;

    [Header("Message Viewer")]
    [SerializeField] private RRNMailViewerUI mailViewer;

    [Header("Starting / Testing Emails")]
    [Tooltip("Email IDs sent immediately when the game begins.")]
    [SerializeField] private List<string> startingEmailIDs = new();

    [Header("Random Daily Fluff")]
    [SerializeField] private bool scheduleFluffOnStart = true;
    [SerializeField] private int minimumFluffPerDay = 1;
    [SerializeField] private int maximumFluffPerDay = 3;

    [Tooltip("09:15 = 555")]
    [SerializeField] private int earliestFluffMinute = 555;

    [Tooltip("17:30 = 1050")]
    [SerializeField] private int latestFluffMinute = 1050;

    [Header("Temporary Date System")]
    [SerializeField] private int placeholderYear = 1964;

    private readonly List<RRNReceivedEmail> inbox = new();
    private readonly List<RRNEmailEntryUI> entryUIs = new();
    private readonly HashSet<string> sentEmailIDs = new();
    private readonly List<ScheduledEmail> scheduledEmails = new();

    private RRNEmailEntryUI selectedEntry;
    private CanvasGroup archivedCanvasGroup;
    private bool archivedVisible;
    private Coroutine archiveAnimation;

    private class ScheduledEmail
    {
        public RRNEmailDefinition email;
        public int deliveryMinute;
    }

    private void Awake()
    {
        SetupArchiveContainer();

        if (archivedMailButton != null)
            archivedMailButton.onClick.AddListener(ToggleArchivedMail);
    }

    private void OnEnable()
    {
        GameClock.OnTimeChanged += HandleTimeChanged;
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= HandleTimeChanged;
    }

    private void Start()
    {
        SendStartingEmails();

        if (scheduleFluffOnStart)
            ScheduleNewDayFluff();

        UpdateCounters();
        RefreshArchivedButton();
    }

    private void SetupArchiveContainer()
    {
        if (archivedContent == null)
            return;

        archivedCanvasGroup = archivedContent.GetComponent<CanvasGroup>();

        if (archivedCanvasGroup == null)
            archivedCanvasGroup = archivedContent.gameObject.AddComponent<CanvasGroup>();

        archivedVisible = false;

        Vector3 scale = archivedContent.localScale;
        scale.y = 0f;
        archivedContent.localScale = scale;

        archivedCanvasGroup.alpha = 0f;
        archivedContent.gameObject.SetActive(false);
    }

    public void SendEmailByID(string emailID)
    {
        if (emailDatabase == null)
        {
            Debug.LogWarning("RRNMailManager: No Email Database assigned.");
            return;
        }

        RRNEmailDefinition email = emailDatabase.GetEmailByID(emailID);

        if (email == null)
        {
            Debug.LogWarning($"RRNMailManager: Email ID '{emailID}' not found.");
            return;
        }

        SendEmail(email);
    }

    public void SendEmail(RRNEmailDefinition email)
    {
        if (email == null)
            return;

        if (!email.allowRepeat && sentEmailIDs.Contains(email.emailID))
        {
            Debug.Log($"Mail '{email.emailID}' has already been sent.");
            return;
        }

        RRNReceivedEmail receivedEmail = new RRNReceivedEmail(
            email,
            GeneratePlaceholderDate(),
            GetCurrentMailTime());

        inbox.Insert(0, receivedEmail);
        sentEmailIDs.Add(email.emailID);

        SpawnInboxEntry(receivedEmail);
        UpdateCounters();

        Debug.Log($"Mail received: {email.sender} - {email.subject}");
    }

    private string GetCurrentMailTime()
    {
        if (GameClock.Instance == null)
            return "09:00";

        if (GameClock.Instance.GetCurrentMinutes() <= 0)
            return "09:00";

        return GameClock.Instance.GetCurrentTimeString();
    }

    private void SpawnInboxEntry(RRNReceivedEmail email)
    {
        if (emailEntryPrefab == null || unreadContent == null)
            return;

        RRNEmailEntryUI entry = Instantiate(emailEntryPrefab, unreadContent);
        entry.transform.SetAsFirstSibling();
        entry.Setup(email, HandleEntryClicked);

        entryUIs.Add(entry);
        RebuildMailLayout();
    }

    private void HandleEntryClicked(RRNEmailEntryUI entry)
    {
        if (entry == null || entry.Email == null)
            return;

        if (selectedEntry != null && selectedEntry != entry)
            selectedEntry.SetSelected(false);

        selectedEntry = entry;
        selectedEntry.SetSelected(true);

        if (!entry.Email.IsRead)
        {
            entry.Email.MarkRead();
            entry.RefreshReadState();
            UpdateCounters();
        }

        if (mailViewer != null)
        {
            mailViewer.DisplayEmail(
                entry.Email,
                response => HandleResponseConfirmed(entry.Email, response),
                () => ArchiveEmail(entry));
        }
    }

    private void HandleResponseConfirmed(
        RRNReceivedEmail email,
        RRNEmailResponseDefinition response)
    {
        if (email == null || response == null || email.HasResponded)
            return;

        email.MarkResponded(response);

        if (mailViewer != null)
            mailViewer.LockResponses(email);

        ExecuteResponse(response);
    }

    private void ArchiveEmail(RRNEmailEntryUI entry)
    {
        if (entry == null ||
            entry.Email == null ||
            entry.Email.IsArchived ||
            archivedContent == null)
        {
            return;
        }

        entry.Email.MarkArchived();
        entry.transform.SetParent(archivedContent, false);
        entry.transform.SetAsFirstSibling();
        entry.SetSelected(false);
        selectedEntry = null;

        RefreshArchivedButton();
        RebuildMailLayout();

        Debug.Log($"Mail archived: {entry.Email.Definition.subject}");
    }

    public void ToggleArchivedMail()
    {
        if (archivedContent == null)
            return;

        if (!archivedVisible && archivedContent.childCount == 0)
            return;

        archivedVisible = !archivedVisible;

        if (archiveAnimation != null)
            StopCoroutine(archiveAnimation);

        archiveAnimation = StartCoroutine(AnimateArchivedMail(archivedVisible));
        RefreshArchivedButton();
    }

    private IEnumerator AnimateArchivedMail(bool show)
    {
        if (archivedCanvasGroup == null || archivedContent == null)
            yield break;

        if (show)
        {
            archivedContent.gameObject.SetActive(true);

            Vector3 startScale = archivedContent.localScale;
            startScale.y = 0f;
            archivedContent.localScale = startScale;

            archivedCanvasGroup.alpha = 0f;
        }

        float elapsed = 0f;
        float startAlpha = archivedCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;
        float startScaleY = archivedContent.localScale.y;
        float targetScaleY = show ? 1f : 0f;

        while (elapsed < archiveFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / archiveFadeDuration);
            t = t * t * (3f - 2f * t);

            archivedCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            Vector3 scale = archivedContent.localScale;
            scale.y = Mathf.Lerp(startScaleY, targetScaleY, t);
            archivedContent.localScale = scale;

            RebuildMailLayout();
            yield return null;
        }

        archivedCanvasGroup.alpha = targetAlpha;

        Vector3 finalScale = archivedContent.localScale;
        finalScale.y = targetScaleY;
        archivedContent.localScale = finalScale;

        if (!show)
            archivedContent.gameObject.SetActive(false);

        RebuildMailLayout();
        archiveAnimation = null;
    }

    private void RefreshArchivedButton()
    {
        int archivedCount = archivedContent != null
            ? archivedContent.childCount
            : 0;

        if (archivedMailButton != null)
            archivedMailButton.interactable = archivedCount > 0;

        if (archivedMailButtonText != null)
        {
            archivedMailButtonText.text = archivedVisible
                ? "HIDE ARCHIVED MAIL"
                : "SHOW ARCHIVED MAIL";
        }
    }

    private void RebuildMailLayout()
    {
        if (unreadContent == null)
            return;

        RectTransform root = unreadContent.parent as RectTransform;

        if (root != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private void ExecuteResponse(RRNEmailResponseDefinition response)
    {
        switch (response.action)
        {
            case RRNEmailResponseAction.None:
                break;

            case RRNEmailResponseAction.SendFollowUpEmail:
                SendEmailByID(response.targetID);
                break;

            case RRNEmailResponseAction.StartProgramme:
                Debug.Log($"Programme accepted from mail: {response.targetID}");
                OnProgrammeAccepted?.Invoke(response.targetID);
                break;

            case RRNEmailResponseAction.AddMoney:
                PlayerManager.Instance?.AddMoney(response.amount);
                break;

            case RRNEmailResponseAction.AddResearchPoints:
                PlayerManager.Instance?.AddResearchPoints(response.amount);
                break;
        }
    }

    public void ScheduleNewDayFluff()
    {
        scheduledEmails.Clear();

        if (emailDatabase == null)
            return;

        List<RRNEmailDefinition> candidates = new();

        foreach (RRNEmailDefinition email in emailDatabase.Emails)
        {
            if (email == null)
                continue;

            if (email.deliveryType != RRNEmailDeliveryType.RandomFluff)
                continue;

            if (!email.allowRepeat && sentEmailIDs.Contains(email.emailID))
                continue;

            candidates.Add(email);
        }

        if (candidates.Count == 0)
            return;

        Shuffle(candidates);

        int amount = UnityEngine.Random.Range(
            minimumFluffPerDay,
            maximumFluffPerDay + 1);

        amount = Mathf.Clamp(amount, 0, candidates.Count);

        HashSet<int> usedTimes = new();

        for (int i = 0; i < amount; i++)
        {
            int deliveryMinute = UnityEngine.Random.Range(
                earliestFluffMinute,
                latestFluffMinute + 1);

            int safety = 0;

            while (usedTimes.Contains(deliveryMinute) && safety < 100)
            {
                deliveryMinute = UnityEngine.Random.Range(
                    earliestFluffMinute,
                    latestFluffMinute + 1);
                safety++;
            }

            usedTimes.Add(deliveryMinute);

            scheduledEmails.Add(new ScheduledEmail
            {
                email = candidates[i],
                deliveryMinute = deliveryMinute
            });
        }

        scheduledEmails.Sort(
            (a, b) => a.deliveryMinute.CompareTo(b.deliveryMinute));

        foreach (ScheduledEmail scheduled in scheduledEmails)
        {
            Debug.Log(
                $"Mail scheduled: {scheduled.email.subject} at " +
                MinutesToTime(scheduled.deliveryMinute));
        }
    }

    private void HandleTimeChanged()
    {
        if (GameClock.Instance == null)
            return;

        int currentMinute = GameClock.Instance.GetCurrentMinutes();

        for (int i = scheduledEmails.Count - 1; i >= 0; i--)
        {
            if (currentMinute >= scheduledEmails[i].deliveryMinute)
            {
                SendEmail(scheduledEmails[i].email);
                scheduledEmails.RemoveAt(i);
            }
        }
    }

    private void UpdateCounters()
    {
        int unreadCount = 0;

        foreach (RRNReceivedEmail email in inbox)
        {
            if (!email.IsRead)
                unreadCount++;
        }

        if (unreadTitle != null)
            unreadTitle.text = $"{unreadCount} Unread";

        if (totalTitle != null)
            totalTitle.text = $"{inbox.Count} Total";
    }

    private void SendStartingEmails()
    {
        foreach (string emailID in startingEmailIDs)
            SendEmailByID(emailID);
    }

    private string GeneratePlaceholderDate()
    {
        string[] months =
        {
            "JAN", "FEB", "MAR", "APR",
            "MAY", "JUN", "JUL", "AUG",
            "SEP", "OCT", "NOV", "DEC"
        };

        int day = UnityEngine.Random.Range(1, 29);
        string month = months[UnityEngine.Random.Range(0, months.Length)];

        return $"{day:00} {month} {placeholderYear}";
    }

    private string MinutesToTime(int minutes)
    {
        int hour = minutes / 60;
        int minute = minutes % 60;

        return $"{hour:00}:{minute:00}";
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
