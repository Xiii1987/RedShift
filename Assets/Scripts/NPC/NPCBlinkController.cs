using System.Collections;
using UnityEngine;

public class NPCBlinkController : MonoBehaviour
{
    [Header("Eye Pivots")]
    [Tooltip("Assign the empty parent object above the left eye quad.")]
    [SerializeField] private Transform leftEye;

    [Tooltip("Assign the empty parent object above the right eye quad.")]
    [SerializeField] private Transform rightEye;

    [Header("Automatic Blinking")]
    [SerializeField] private bool automaticBlinking = true;

    [Tooltip("Minimum time between blinks.")]
    [SerializeField, Min(0f)] private float minimumBlinkInterval = 3f;

    [Tooltip("Maximum time between blinks.")]
    [SerializeField, Min(0f)] private float maximumBlinkInterval = 7f;

    [Header("Blink Shape")]
    [Tooltip("Eye height while fully closed. 0.05 leaves a very thin black line.")]
    [SerializeField, Range(0f, 1f)] private float closedHeight = 0.05f;

    [Tooltip("Time taken to close the eyes.")]
    [SerializeField, Min(0.01f)] private float closingDuration = 0.06f;

    [Tooltip("How long the eyes remain closed.")]
    [SerializeField, Min(0f)] private float closedDuration = 0.04f;

    [Tooltip("Time taken to reopen the eyes.")]
    [SerializeField, Min(0.01f)] private float openingDuration = 0.09f;

    private Vector3 leftOpenScale;
    private Vector3 rightOpenScale;

    private Coroutine blinkingRoutine;
    private bool isBlinking;

    private void Awake()
    {
        if (leftEye == null || rightEye == null)
        {
            Debug.LogError(
                $"{name}: NPCBlinkController is missing one or both eye pivots.",
                this
            );

            enabled = false;
            return;
        }

        // Whatever scales the eye pivots currently have become their open state.
        leftOpenScale = leftEye.localScale;
        rightOpenScale = rightEye.localScale;
    }

    private void OnEnable()
    {
        if (leftEye == null || rightEye == null)
            return;

        if (automaticBlinking)
            blinkingRoutine = StartCoroutine(AutomaticBlinkRoutine());
    }

    private void OnDisable()
    {
        if (blinkingRoutine != null)
        {
            StopCoroutine(blinkingRoutine);
            blinkingRoutine = null;
        }

        isBlinking = false;
        RestoreOpenEyes();
    }

    private IEnumerator AutomaticBlinkRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(
                minimumBlinkInterval,
                maximumBlinkInterval
            );

            yield return new WaitForSeconds(waitTime);
            yield return BlinkRoutine();
        }
    }

    private IEnumerator BlinkRoutine()
    {
        if (isBlinking)
            yield break;

        isBlinking = true;

        Vector3 leftClosedScale = CreateClosedScale(leftOpenScale);
        Vector3 rightClosedScale = CreateClosedScale(rightOpenScale);

        // Close.
        yield return ScaleEyes(
            leftEye.localScale,
            rightEye.localScale,
            leftClosedScale,
            rightClosedScale,
            closingDuration
        );

        // Briefly remain closed.
        if (closedDuration > 0f)
            yield return new WaitForSeconds(closedDuration);

        // Reopen.
        yield return ScaleEyes(
            leftEye.localScale,
            rightEye.localScale,
            leftOpenScale,
            rightOpenScale,
            openingDuration
        );

        RestoreOpenEyes();
        isBlinking = false;
    }

    private IEnumerator ScaleEyes(
        Vector3 leftStart,
        Vector3 rightStart,
        Vector3 leftTarget,
        Vector3 rightTarget,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);

            leftEye.localScale = Vector3.Lerp(
                leftStart,
                leftTarget,
                smoothedProgress
            );

            rightEye.localScale = Vector3.Lerp(
                rightStart,
                rightTarget,
                smoothedProgress
            );

            yield return null;
        }

        leftEye.localScale = leftTarget;
        rightEye.localScale = rightTarget;
    }

    private Vector3 CreateClosedScale(Vector3 openScale)
    {
        // Preserve X and Z. Only squash the local Y axis.
        return new Vector3(
            openScale.x,
            openScale.y * closedHeight,
            openScale.z
        );
    }

    private void RestoreOpenEyes()
    {
        if (leftEye != null)
            leftEye.localScale = leftOpenScale;

        if (rightEye != null)
            rightEye.localScale = rightOpenScale;
    }

    /// <summary>
    /// Allows dialogue or another system to trigger a blink manually.
    /// </summary>
    public void Blink()
    {
        if (!isActiveAndEnabled || isBlinking)
            return;

        StartCoroutine(BlinkRoutine());
    }

    public void SetAutomaticBlinking(bool enabled)
    {
        automaticBlinking = enabled;

        if (!isActiveAndEnabled)
            return;

        if (blinkingRoutine != null)
        {
            StopCoroutine(blinkingRoutine);
            blinkingRoutine = null;
        }

        if (automaticBlinking)
            blinkingRoutine = StartCoroutine(AutomaticBlinkRoutine());
    }
}