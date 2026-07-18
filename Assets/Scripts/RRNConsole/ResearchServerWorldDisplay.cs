using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResearchServerWorldDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResearchServer server;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI contractNameText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Progress")]
    [SerializeField] private Slider progressBar;

    [Header("Status Colours")]
    [SerializeField] private Color busyColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color idleColour = new Color32(0x6F, 0xD1, 0x00, 255);
    [SerializeField] private Color completedColour = new Color32(0x6F, 0xD1, 0x00, 255);

    [Header("Percentage Colours")]
    [SerializeField] private Color redProgressColour = new Color32(0xD9, 0x3B, 0x30, 255);
    [SerializeField] private Color orangeProgressColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color greenProgressColour = new Color32(0x6F, 0xD1, 0x00, 255);

    private void Awake()
    {
        if (server == null)
        {
            server = GetComponentInParent<ResearchServer>();
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 100f;
        }

        RefreshDisplay();
    }

    private void OnEnable()
    {
        GameClock.OnTimeChanged += RefreshDisplay;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= RefreshDisplay;
    }

    private void RefreshDisplay()
    {
        if (server == null)
        {
            ClearDisplay();
            return;
        }

        if (titleText != null)
            titleText.text = server.ServerName;

        if (server.IsCompletingContract)
        {
            SetCompletedDisplay();
            return;
        }

        if (server.IsRunningContract && server.CurrentContract != null)
        {
            SetBusyDisplay();
            return;
        }

        SetIdleDisplay();
    }

    private void SetBusyDisplay()
    {
        float progressPercent = GetServerProgressPercent();
        Color progressColour = GetProgressColour(progressPercent);

        if (statusText != null)
        {
            statusText.text = "BUSY";
            statusText.color = busyColour;
        }

        if (contractNameText != null)
            contractNameText.text = server.CurrentContract.contractName;

        if (timeRemainingText != null)
            timeRemainingText.text = server.GetRemainingTimeText();

        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(progressPercent)}%";
            percentageText.color = progressColour;
        }

        if (progressBar != null)
            progressBar.value = progressPercent;
    }

    private void SetIdleDisplay()
    {
        if (statusText != null)
        {
            statusText.text = "IDLE";
            statusText.color = idleColour;
        }

        if (contractNameText != null)
            contractNameText.text = "Server Offline";

        if (timeRemainingText != null)
            timeRemainingText.text = "";

        if (percentageText != null)
            percentageText.text = "";

        if (progressBar != null)
            progressBar.value = 0f;
    }

    private void SetCompletedDisplay()
    {
        if (statusText != null)
        {
            statusText.text = "COMPLETED";
            statusText.color = completedColour;
        }

        if (contractNameText != null)
            contractNameText.text = server.CurrentContract != null ? server.CurrentContract.contractName : "Server Offline";

        if (timeRemainingText != null)
            timeRemainingText.text = "00:00:00";

        if (percentageText != null)
        {
            percentageText.text = "100%";
            percentageText.color = greenProgressColour;
        }

        if (progressBar != null)
            progressBar.value = 100f;
    }

    private void ClearDisplay()
    {
        if (titleText != null) titleText.text = "";
        if (statusText != null) statusText.text = "";
        if (contractNameText != null) contractNameText.text = "";
        if (timeRemainingText != null) timeRemainingText.text = "";
        if (percentageText != null) percentageText.text = "";

        if (progressBar != null)
            progressBar.value = 0f;
    }

    private float GetServerProgressPercent()
    {
        if (server == null || server.CurrentContract == null)
            return 0f;

        float totalDuration = server.CurrentContract.durationSeconds;

        if (totalDuration <= 0f)
            return 0f;

        if (server.IsCompletingContract)
            return 100f;

        float remaining = server.RemainingTimeSeconds;
        float progress = 1f - (remaining / totalDuration);

        return Mathf.Clamp(progress * 100f, 0f, 100f);
    }

    private Color GetProgressColour(float progressPercent)
    {
        if (progressPercent <= 20f)
        {
            return Color.Lerp(redProgressColour, orangeProgressColour, progressPercent / 20f);
        }

        if (progressPercent <= 80f)
        {
            return orangeProgressColour;
        }

        return Color.Lerp(
            orangeProgressColour,
            greenProgressColour,
            (progressPercent - 80f) / 20f
        );
    }
}