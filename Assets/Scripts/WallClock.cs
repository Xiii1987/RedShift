using UnityEngine;

public class WallClock : MonoBehaviour
{
    [Header("References")]
    public GameClock gameClock;

    public Transform hourHand;
    public Transform minuteHand;

    private void Update()
    {
        if (gameClock == null)
            return;

        // Total minutes since midnight (or whatever your GameClock returns)
        int totalMinutes = gameClock.GetCurrentMinutes();

        // Split into hours and minutes
        float hours = totalMinutes / 60f;
        float minutes = totalMinutes % 60f;

        // 360 / 60 = 6 degrees per minute
        float minuteAngle = -(minutes * 6f);

        // 360 / 12 = 30 degrees per hour
        // Using the float means the hour hand smoothly moves between hours.
        float hourAngle = -((hours % 12f) * 30f);

        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(0f, -90f, minuteAngle);

        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(0f, -90f, hourAngle);
    }
}