using TMPro;
using UnityEngine;

public class RRNClockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clockText;

    private void OnEnable()
    {
        GameClock.OnTimeChanged += RefreshClock;
        RefreshClock();
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= RefreshClock;
    }

    private void RefreshClock()
    {
        if (clockText == null || GameClock.Instance == null)
            return;

        clockText.text = GameClock.Instance.GetCurrentTimeString();
    }
}