using UnityEngine;

public class CCTVPatrol : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation Settings")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;

    [Tooltip("How far the camera rotates from its starting rotation in each direction.")]
    [SerializeField] private float turnAmount = 90f;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 20f;

    [Header("Pause Settings")]
    [Tooltip("How long the camera pauses when it reaches each end.")]
    [SerializeField] private float pauseDuration = 2f;

    private Quaternion startRotation;
    private Quaternion leftRotation;
    private Quaternion rightRotation;

    private Quaternion targetRotation;

    private bool movingRight = true;
    private float pauseTimer;
    private bool isPaused;

    private void Start()
    {
        startRotation = transform.localRotation;

        Vector3 axis = GetRotationAxis();

        // turnAmount is the TOTAL sweep.
        // Example: 90 = 45 degrees left and 45 degrees right.
        float halfTurn = turnAmount * 0.5f;

        leftRotation =
            startRotation * Quaternion.AngleAxis(-halfTurn, axis);

        rightRotation =
            startRotation * Quaternion.AngleAxis(halfTurn, axis);

        targetRotation = rightRotation;
    }

    private void Update()
    {
        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;

            if (pauseTimer <= 0f)
            {
                isPaused = false;

                movingRight = !movingRight;
                targetRotation = movingRight
                    ? rightRotation
                    : leftRotation;
            }

            return;
        }

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.01f)
        {
            transform.localRotation = targetRotation;

            isPaused = true;
            pauseTimer = pauseDuration;
        }
    }

    private Vector3 GetRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;

            case RotationAxis.Z:
                return Vector3.forward;

            case RotationAxis.Y:
            default:
                return Vector3.up;
        }
    }
}