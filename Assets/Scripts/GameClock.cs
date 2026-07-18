using System;
using TMPro;
using UnityEngine;

public class GameClock : MonoBehaviour
{
    public static GameClock Instance { get; private set; }

    /// <summary>
    /// Fired whenever the in-game minute changes.
    /// </summary>
    public static event Action OnTimeChanged;

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    [Header("Work Day")]
    [SerializeField] private string startTime = "09:00";
    [SerializeField] private string endTime = "18:00";

    [Header("Time Settings")]
    [SerializeField] private float secondsPerMinute = 1f;

    private int currentMinutes;
    private int endMinutes;
    private float timer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        currentMinutes = TimeToMinutes(startTime);
        endMinutes = TimeToMinutes(endTime);

        UpdateClockUI();
        OnTimeChanged?.Invoke();
    }

    private void Update()
    {
        if (currentMinutes >= endMinutes)
            return;

        timer += Time.deltaTime;

        if (timer >= secondsPerMinute)
        {
            timer -= secondsPerMinute;
            currentMinutes++;

            UpdateClockUI();

            OnTimeChanged?.Invoke();
        }
    }

    private void UpdateClockUI()
    {
        int hours = currentMinutes / 60;
        int minutes = currentMinutes % 60;

        if (timeText != null)
        {
            timeText.text = $"{hours:00}:{minutes:00}";
        }
    }

    private int TimeToMinutes(string time)
    {
        string[] parts = time.Split(':');

        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);

        return (hours * 60) + minutes;
    }

    public int GetCurrentMinutes()
    {
        return currentMinutes;
    }

    public string GetCurrentTimeString()
    {
        int hours = currentMinutes / 60;
        int minutes = currentMinutes % 60;

        return $"{hours:00}:{minutes:00}";
    }
}