using System.Collections;
using UnityEngine;

public class ElevatorDoorOpener : MonoBehaviour
{
    public enum MoveAxis
    {
        X,
        Y,
        Z
    }

    [Header("Doors")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Movement")]
    [SerializeField] private MoveAxis moveAxis = MoveAxis.X;

    [SerializeField] private float leftDoorOffset = -0.8f;
    [SerializeField] private float rightDoorOffset = 0.8f;

    [SerializeField] private float moveTime = 1.0f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private float stayOpenTime = 5f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private void Start()
    {
        if (leftDoor == null || rightDoor == null)
            return;

        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        leftOpenPos = leftClosedPos + AxisVector(leftDoorOffset);
        rightOpenPos = rightClosedPos + AxisVector(rightDoorOffset);

        StartCoroutine(DoorSequence());
    }

    private IEnumerator DoorSequence()
    {
        // Wait before opening
        yield return new WaitForSeconds(startDelay);

        yield return MoveDoors(leftClosedPos, leftOpenPos, rightClosedPos, rightOpenPos);

        // Stay open
        yield return new WaitForSeconds(stayOpenTime);

        yield return MoveDoors(leftOpenPos, leftClosedPos, rightOpenPos, rightClosedPos);
    }

    private IEnumerator MoveDoors(
        Vector3 leftStart,
        Vector3 leftEnd,
        Vector3 rightStart,
        Vector3 rightEnd)
    {
        float timer = 0f;

        while (timer < moveTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / moveTime);

            // Ease In / Ease Out
            t = Mathf.SmoothStep(0f, 1f, t);

            leftDoor.localPosition = Vector3.Lerp(leftStart, leftEnd, t);
            rightDoor.localPosition = Vector3.Lerp(rightStart, rightEnd, t);

            yield return null;
        }

        leftDoor.localPosition = leftEnd;
        rightDoor.localPosition = rightEnd;
    }

    private Vector3 AxisVector(float amount)
    {
        switch (moveAxis)
        {
            case MoveAxis.X:
                return Vector3.right * amount;

            case MoveAxis.Y:
                return Vector3.up * amount;

            case MoveAxis.Z:
                return Vector3.forward * amount;

            default:
                return Vector3.zero;
        }
    }
}