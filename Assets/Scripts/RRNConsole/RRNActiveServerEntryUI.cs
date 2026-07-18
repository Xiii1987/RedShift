using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RRNActiveServerEntryUI : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI statusSymbolText;
    [SerializeField] private TextMeshProUGUI contractNameText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Graphics")]
    [SerializeField] private Graphic background;

    [Header("Progress")]
    [SerializeField] private Slider progressBar;

    [Header("Colours")]
    [SerializeField] private Color activeColour = new Color32(0x6F, 0xD1, 0x00, 255);
    [SerializeField] private Color redProgressColour = new Color32(0xD9, 0x3B, 0x30, 255);
    [SerializeField] private Color orangeProgressColour = new Color32(0xFF, 0x84, 0x00, 255);
    [SerializeField] private Color greenProgressColour = new Color32(0x6F, 0xD1, 0x00, 255);

    [Header("Completion Flash")]
    [SerializeField] private Color normalBackgroundColour = new Color32(0x16, 0x1C, 0x16, 255);
    [SerializeField] private Color completedFlashColour = new Color32(0x77, 0xFF, 0x77, 255);
    

private float completionFlashStartTime = -1f;
private const float flashDuration = 1f;

    private ResearchServer server;

    public ResearchServer Server => server;

    public void Setup(ResearchServer newServer)
    {
        server = newServer;

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 100f;
        }

        RefreshDisplay();
    }

    private void Update()
    {
        RefreshDisplay();
    }

private void RefreshDisplay()
{
    if (server == null)
        return;

    ResearchContract contract = server.CurrentContract;
    float progressPercent = GetServerProgressPercent();
    Color progressColour = GetProgressColour(progressPercent);

    if (titleText != null)
        titleText.text = server.ServerName;

    if (statusText != null)
    {
        statusText.text = server.IsCompletingContract ? "COMPLETED" : "ACTIVE";
        statusText.color = activeColour;
    }

    if (statusSymbolText != null)
    {
        statusSymbolText.text = server.IsCompletingContract ? "■" : "▲";
        statusSymbolText.color = activeColour;
    }

    if (contractNameText != null)
    {
        contractNameText.text = contract != null ? contract.contractName : "No Active Contract";
    }

    if (timeRemainingText != null)
    {
        timeRemainingText.text = server.IsCompletingContract ? "00:00:00" : server.GetRemainingTimeText();
    }

    if (percentageText != null)
    {
        percentageText.text = $"{Mathf.RoundToInt(progressPercent)}%";
        percentageText.color = progressColour;
    }

    if (progressBar != null)
    {
        progressBar.value = progressPercent;
    }

    RefreshBackgroundFlash();
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
    // 0-20% : Red -> Orange
    if (progressPercent <= 20f)
    {
        return Color.Lerp(redProgressColour, orangeProgressColour, progressPercent / 20f);
    }

    // 20-80% : Orange
    if (progressPercent <= 80f)
    {
        return orangeProgressColour;
    }

    // 80-100% : Orange -> Green
    return Color.Lerp(
        orangeProgressColour,
        greenProgressColour,
        (progressPercent - 80f) / 20f);
}

private void RefreshBackgroundFlash()
{
    if (background == null)
        return;

    if (!server.IsCompletingContract)
    {
        completionFlashStartTime = -1f;
        background.color = normalBackgroundColour;
        return;
    }

    if (completionFlashStartTime < 0f)
    {
        completionFlashStartTime = Time.time;
    }

    float elapsed = Time.time - completionFlashStartTime;

    // Flash for the first second only.
    if (elapsed < flashDuration)
    {
        // Two complete flashes in one second.
        float t = Mathf.PingPong(elapsed * 4f, 1f);
        background.color = Color.Lerp(normalBackgroundColour, completedFlashColour, t);
    }
   else
{
    // Return to the normal background after the flashes.
    background.color = normalBackgroundColour;
}
}

}



