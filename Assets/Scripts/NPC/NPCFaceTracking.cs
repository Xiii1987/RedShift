using UnityEngine;

public class NPCFaceTracking : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform referenceTransform;
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 4f;
    [SerializeField, Range(0f, 180f)] private float maxViewAngle = 100f;

    [Header("Reference Axis Correction")]
    [SerializeField] private float referenceYawOffset = 180f;

    [Header("Eye Limits")]
    [SerializeField] private float horizontalLimit = 20f;
    [SerializeField] private float verticalLimit = 12f;

    [Header("Eye Movement")]
    [SerializeField] private float eyeSpeed = 120f;
    [SerializeField] private float deadZone = 0.75f;

    [Header("Axis Correction")]
    [SerializeField] private float yawMultiplier = 1f;
    [SerializeField] private float pitchMultiplier = 1f;

    private Quaternion leftEyeNeutralRotation;
    private Quaternion rightEyeNeutralRotation;

    private float currentYaw;
    private float currentPitch;

    private void Awake()
    {
        if (referenceTransform == null)
            referenceTransform = transform;

        if (leftEye != null)
            leftEyeNeutralRotation = leftEye.localRotation;

        if (rightEye != null)
            rightEyeNeutralRotation = rightEye.localRotation;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (leftEye == null || rightEye == null)
            return;

        float desiredYaw = 0f;
        float desiredPitch = 0f;

        if (CanTrackTarget())
        {
            CalculateEyeAngles(
                out desiredYaw,
                out desiredPitch
            );
        }

        currentYaw = Mathf.MoveTowardsAngle(
            currentYaw,
            desiredYaw,
            eyeSpeed * Time.deltaTime
        );

        currentPitch = Mathf.MoveTowardsAngle(
            currentPitch,
            desiredPitch,
            eyeSpeed * Time.deltaTime
        );

        ApplyEyeRotation();
    }

    private bool CanTrackTarget()
    {
        if (target == null || referenceTransform == null)
            return false;

        Vector3 directionToTarget =
            target.position - referenceTransform.position;

        if (directionToTarget.sqrMagnitude >
            detectionDistance * detectionDistance)
            return false;

        Quaternion correctedRotation =
            referenceTransform.rotation *
            Quaternion.Euler(0f, referenceYawOffset, 0f);

        Vector3 correctedForward =
            correctedRotation * Vector3.forward;

        float angle = Vector3.Angle(
            correctedForward,
            directionToTarget
        );

        return angle <= maxViewAngle;
    }

    private void CalculateEyeAngles(
        out float yaw,
        out float pitch)
    {
        Vector3 direction =
            target.position - referenceTransform.position;

        Quaternion correctedRotation =
            referenceTransform.rotation *
            Quaternion.Euler(0f, referenceYawOffset, 0f);

        Vector3 localDirection =
            Quaternion.Inverse(correctedRotation) *
            direction.normalized;

        yaw = Mathf.Atan2(
            localDirection.x,
            localDirection.z
        ) * Mathf.Rad2Deg;

        float horizontalDistance = Mathf.Sqrt(
            localDirection.x * localDirection.x +
            localDirection.z * localDirection.z
        );

        pitch = -Mathf.Atan2(
            localDirection.y,
            horizontalDistance
        ) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(
            yaw,
            -horizontalLimit,
            horizontalLimit
        );

        pitch = Mathf.Clamp(
            pitch,
            -verticalLimit,
            verticalLimit
        );

        if (Mathf.Abs(yaw) < deadZone)
            yaw = 0f;

        if (Mathf.Abs(pitch) < deadZone)
            pitch = 0f;
    }

    private void ApplyEyeRotation()
    {
        Quaternion eyeOffset = Quaternion.Euler(
            currentPitch * pitchMultiplier,
            currentYaw * yawMultiplier,
            0f
        );

        leftEye.localRotation =
            leftEyeNeutralRotation * eyeOffset;

        rightEye.localRotation =
            rightEyeNeutralRotation * eyeOffset;
    }
}