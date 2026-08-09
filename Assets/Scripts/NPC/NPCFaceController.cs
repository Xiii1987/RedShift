using System.Collections;
using UnityEngine;

public class NPCFaceController : MonoBehaviour
{
    public enum Expression
    {
        Neutral,
        Squint,
        Surprised,
        Angry,
        Sad
    }

    [System.Serializable]
    public struct FacePose
    {
        [Header("Eyelids")]
        public float topLidX;
        public float bottomLidX;

        [Header("Eyebrows")]
        public float browY;
        public float leftBrowZ;
        public float rightBrowZ;
    }

    [Header("Face Objects")]
    [SerializeField] private Transform topLid;
    [SerializeField] private Transform bottomLid;
    [SerializeField] private Transform leftEyebrow;
    [SerializeField] private Transform rightEyebrow;
	private Expression appliedExpression;

    [Header("Expression Poses")]
    [SerializeField] private FacePose neutralPose = new FacePose
    {
        topLidX = 180f,
        bottomLidX = 75f,
        browY = 0f,
        leftBrowZ = 0f,
        rightBrowZ = 0f
    };

    [SerializeField] private FacePose squintPose = new FacePose
    {
        topLidX = 102f,
        bottomLidX = 85f,
        browY = -0.003f,
        leftBrowZ = -3f,
        rightBrowZ = 3f
    };

    [SerializeField] private FacePose surprisedPose = new FacePose
    {
        topLidX = 130f,
        bottomLidX = 55f,
        browY = 0.012f,
        leftBrowZ = 4f,
        rightBrowZ = -4f
    };

    [SerializeField] private FacePose angryPose = new FacePose
    {
        topLidX = 105f,
        bottomLidX = 84f,
        browY = -0.003f,
        leftBrowZ = -12f,
        rightBrowZ = 12f
    };

    [SerializeField] private FacePose sadPose = new FacePose
    {
        topLidX = 180f,
        bottomLidX = 75f,
        browY = 0.003f,
        leftBrowZ = 10f,
        rightBrowZ = -10f
    };

    [Header("Closed Eyelid Angle")]
    [Tooltip("The X angle used by both eyelids when fully closed.")]
    [SerializeField] private float closedX = 90f;

    [Header("Expression Timing")]
    [SerializeField, Min(0f)]
    private float expressionTransitionDuration = 0.18f;

    [Header("Automatic Blinking")]
    [SerializeField] private bool automaticBlinking = true;

    [SerializeField] private Vector2 timeBetweenBlinks =
        new Vector2(2.5f, 6f);

    [SerializeField, Min(0.01f)]
    private float blinkCloseDuration = 0.06f;

    [SerializeField, Min(0f)]
    private float blinkHoldDuration = 0.025f;

    [SerializeField, Min(0.01f)]
    private float blinkOpenDuration = 0.10f;

    [Header("Current State")]
    [SerializeField] private Expression currentExpression =
        Expression.Neutral;

    private Vector3 leftBrowNeutralPosition;
    private Vector3 rightBrowNeutralPosition;



    private float currentTopLidX;
    private float currentBottomLidX;
    private float currentBrowY;
    private float currentLeftBrowZ;
    private float currentRightBrowZ;

    private float blinkAmount;

    private bool isBlinking;
    private bool isInitialised;

    private Coroutine blinkLoop;
    private Coroutine blinkRoutine;
    private Coroutine expressionRoutine;
	
	private Quaternion topLidRestRotation;
	private Quaternion bottomLidRestRotation;

	private Quaternion leftBrowRestRotation;
	private Quaternion rightBrowRestRotation;
	

   private void Awake()
{
    if (!ValidateReferences())
    {
        enabled = false;
        return;
    }

    leftBrowNeutralPosition = leftEyebrow.localPosition;
    rightBrowNeutralPosition = rightEyebrow.localPosition;

    topLidRestRotation = topLid.localRotation;
    bottomLidRestRotation = bottomLid.localRotation;

    leftBrowRestRotation = leftEyebrow.localRotation;
    rightBrowRestRotation = rightEyebrow.localRotation;

    ApplyPoseInstant(GetPose(currentExpression));

    appliedExpression = currentExpression;

    blinkAmount = 0f;
    isInitialised = true;

    ApplyFaceTransforms();
}

    private void OnEnable()
    {
        if (isInitialised && automaticBlinking)
        {
            StartBlinkLoop();
        }
    }

    private void Update()
{
   
    // Allows Automatic Blinking to be enabled during Play mode.
    if (automaticBlinking &&
        blinkLoop == null &&
        isActiveAndEnabled)
    {
        StartBlinkLoop();
    }
}

    private void LateUpdate()
    {
        ApplyFaceTransforms();
    }

    private void OnDisable()
    {
        if (blinkLoop != null)
        {
            StopCoroutine(blinkLoop);
            blinkLoop = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        if (expressionRoutine != null)
        {
            StopCoroutine(expressionRoutine);
            expressionRoutine = null;
        }

        blinkAmount = 0f;
        isBlinking = false;
    }

    private bool ValidateReferences()
    {
        if (topLid == null ||
            bottomLid == null ||
            leftEyebrow == null ||
            rightEyebrow == null)
        {
            Debug.LogError(
                $"{name}: NPCFaceController is missing a face-object reference.",
                this
            );

            return false;
        }

        return true;
    }

    private void StartBlinkLoop()
    {
        if (blinkLoop == null)
        {
            blinkLoop = StartCoroutine(RandomBlinkLoop());
        }
    }

    private IEnumerator RandomBlinkLoop()
    {
        while (automaticBlinking)
        {
            float minimum = Mathf.Max(
                0.5f,
                Mathf.Min(timeBetweenBlinks.x, timeBetweenBlinks.y)
            );

            float maximum = Mathf.Max(
                minimum,
                Mathf.Max(timeBetweenBlinks.x, timeBetweenBlinks.y)
            );

            yield return new WaitForSeconds(
                Random.Range(minimum, maximum)
            );

            if (!isBlinking)
            {
                yield return BlinkOnce();
            }
        }

        blinkLoop = null;
    }

    public void TriggerBlink()
    {
        if (!isActiveAndEnabled || isBlinking)
        {
            return;
        }

        blinkRoutine = StartCoroutine(BlinkOnce());
    }

    private IEnumerator BlinkOnce()
    {
        if (isBlinking)
        {
            yield break;
        }

        isBlinking = true;

        yield return AnimateBlinkAmount(
            blinkAmount,
            1f,
            blinkCloseDuration
        );

        if (blinkHoldDuration > 0f)
        {
            yield return new WaitForSeconds(blinkHoldDuration);
        }

        yield return AnimateBlinkAmount(
            blinkAmount,
            0f,
            blinkOpenDuration
        );

        blinkAmount = 0f;
        isBlinking = false;
        blinkRoutine = null;
    }

    private IEnumerator AnimateBlinkAmount(
        float start,
        float target,
        float duration
    )
    {
        if (duration <= 0f)
        {
            blinkAmount = target;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            progress = SmoothStep(progress);

            blinkAmount = Mathf.Lerp(start, target, progress);

            yield return null;
        }

        blinkAmount = target;
    }

    public void SetExpression(Expression expression)
{
    currentExpression = expression;
    appliedExpression = expression;

    if (expressionRoutine != null)
    {
        StopCoroutine(expressionRoutine);
    }

    expressionRoutine = StartCoroutine(
        TransitionToPose(GetPose(expression))
    );
}

    public void SetNeutral()
    {
        SetExpression(Expression.Neutral);
    }

    public void SetSquint()
    {
        SetExpression(Expression.Squint);
    }

    public void SetSurprised()
    {
        SetExpression(Expression.Surprised);
    }

    public void SetAngry()
    {
        SetExpression(Expression.Angry);
    }

    public void SetSad()
    {
        SetExpression(Expression.Sad);
    }

    private IEnumerator TransitionToPose(FacePose target)
    {
        if (expressionTransitionDuration <= 0f)
        {
            ApplyPoseInstant(target);
            expressionRoutine = null;
            yield break;
        }

        float startTopX = currentTopLidX;
        float startBottomX = currentBottomLidX;

        float startBrowY = currentBrowY;
        float startLeftBrowZ = currentLeftBrowZ;
        float startRightBrowZ = currentRightBrowZ;

        float elapsed = 0f;

        while (elapsed < expressionTransitionDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / expressionTransitionDuration
            );

            progress = SmoothStep(progress);

            currentTopLidX = Mathf.LerpAngle(
                startTopX,
                target.topLidX,
                progress
            );

            currentBottomLidX = Mathf.LerpAngle(
                startBottomX,
                target.bottomLidX,
                progress
            );

            currentBrowY = Mathf.Lerp(
                startBrowY,
                target.browY,
                progress
            );

            currentLeftBrowZ = Mathf.LerpAngle(
                startLeftBrowZ,
                target.leftBrowZ,
                progress
            );

            currentRightBrowZ = Mathf.LerpAngle(
                startRightBrowZ,
                target.rightBrowZ,
                progress
            );

            yield return null;
        }

        ApplyPoseInstant(target);
        expressionRoutine = null;
    }

    private FacePose GetPose(Expression expression)
    {
        switch (expression)
        {
            case Expression.Squint:
                return squintPose;

            case Expression.Surprised:
                return surprisedPose;

            case Expression.Angry:
                return angryPose;

            case Expression.Sad:
                return sadPose;

            default:
                return neutralPose;
        }
    }

    private void ApplyPoseInstant(FacePose pose)
    {
        currentTopLidX = pose.topLidX;
        currentBottomLidX = pose.bottomLidX;

        currentBrowY = pose.browY;
        currentLeftBrowZ = pose.leftBrowZ;
        currentRightBrowZ = pose.rightBrowZ;
    }

    private void ApplyFaceTransforms()
    {
        // Expressions define the open position.
        // Blink Amount blends that position toward Closed X.
        float finalTopX = Mathf.LerpAngle(
            currentTopLidX,
            closedX,
            blinkAmount
        );

        float finalBottomX = Mathf.LerpAngle(
            currentBottomLidX,
            closedX,
            blinkAmount
        );

        SetLidRotation(
    topLid,
    topLidRestRotation,
    finalTopX,
    neutralPose.topLidX
);

SetLidRotation(
    bottomLid,
    bottomLidRestRotation,
    finalBottomX,
    neutralPose.bottomLidX
);
        leftEyebrow.localPosition =
            leftBrowNeutralPosition +
            Vector3.up * currentBrowY;

        rightEyebrow.localPosition =
            rightBrowNeutralPosition +
            Vector3.up * currentBrowY;

   leftEyebrow.localRotation =
    leftBrowRestRotation *
    Quaternion.AngleAxis(
        currentLeftBrowZ,
        Vector3.forward
    );

rightEyebrow.localRotation =
    rightBrowRestRotation *
    Quaternion.AngleAxis(
        currentRightBrowZ,
        Vector3.forward
    );
	}


  private void SetLidRotation(
    Transform lid,
    Quaternion restRotation,
    float targetX,
    float neutralX)
{
    float offset = Mathf.DeltaAngle(
        neutralX,
        targetX
    );

    lid.localRotation =
        restRotation *
        Quaternion.AngleAxis(
            offset,
            Vector3.right
        );
}
    private float WrapAngle(float angle)
    {
        return Mathf.Repeat(angle, 360f);
    }

    private float SmoothStep(float value)
    {
        return value * value * (3f - (2f * value));
    }

    [ContextMenu("Test/Blink")]
    private void TestBlink()
    {
        if (Application.isPlaying)
        {
            TriggerBlink();
        }
    }

    [ContextMenu("Test/Neutral")]
    private void TestNeutral()
    {
        if (Application.isPlaying)
        {
            SetNeutral();
        }
    }

    [ContextMenu("Test/Squint")]
    private void TestSquint()
    {
        if (Application.isPlaying)
        {
            SetSquint();
        }
    }

    [ContextMenu("Test/Surprised")]
    private void TestSurprised()
    {
        if (Application.isPlaying)
        {
            SetSurprised();
        }
    }

    [ContextMenu("Test/Angry")]
    private void TestAngry()
    {
        if (Application.isPlaying)
        {
            SetAngry();
        }
    }

    [ContextMenu("Test/Sad")]
    private void TestSad()
    {
        if (Application.isPlaying)
        {
            SetSad();
        }
    }
}