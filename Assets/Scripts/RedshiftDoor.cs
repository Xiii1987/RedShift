using System.Collections;
using UnityEngine;

public class RedshiftDoor : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Rotation")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;
    [SerializeField] private float closedRotation = 0f;
    [SerializeField] private float forwardOpenRotation = 90f;
    [SerializeField] private float backwardOpenRotation = -90f;

    [Header("Animation")]
	[SerializeField] private float openDelay = 0.15f;
	[SerializeField] private float animationTime = 0.4f;
	[SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
	
	[Header("Auto Close")]
    [SerializeField] private bool autoClose = true;
    [SerializeField] private float autoCloseDelay = 3f;

    [Header("Sound")]
    [SerializeField] private bool playSound = true;
    [SerializeField] private RedshiftUISoundType openSound = RedshiftUISoundType.Confirm;
    [SerializeField] private RedshiftUISoundType closeSound = RedshiftUISoundType.TabChange;
	
	

	private bool isOpen;
    private float currentOpenRotation;
    private Coroutine animationRoutine;
    private Coroutine autoCloseRoutine;

    private void Awake()
    {
        if (target == null)
            target = transform;

        currentOpenRotation = forwardOpenRotation;
        ApplyRotation(closedRotation);
    }

    public void ToggleDoor()
    {
        if (isOpen)
            CloseDoor();
        else
            ToggleForward();
    }

    public void ToggleForward()
{
    currentOpenRotation = forwardOpenRotation;

    if (isOpen)
    {
        SetDoor(false);
    }
    else
    {
        StartCoroutine(OpenAfterDelay());
    }
}
    public void ToggleBackward()
{
    currentOpenRotation = backwardOpenRotation;

    if (isOpen)
    {
        SetDoor(false);
    }
    else
    {
        StartCoroutine(OpenAfterDelay());
    }
}

private IEnumerator OpenAfterDelay()
{
    yield return new WaitForSeconds(openDelay);

    SetDoor(true);
}


    public void OpenForward()
    {
        currentOpenRotation = forwardOpenRotation;
        SetDoor(true);
    }

    public void OpenBackward()
    {
        currentOpenRotation = backwardOpenRotation;
        SetDoor(true);
    }

    public void CloseDoor()
    {
        SetDoor(false);
    }

    public void SetDoor(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (autoCloseRoutine != null)
        {
            StopCoroutine(autoCloseRoutine);
            autoCloseRoutine = null;
        }

        if (playSound)
            UISoundManager.Instance?.PlaySound(open ? openSound : closeSound);

        animationRoutine = StartCoroutine(AnimateDoor(open));

        if (open && autoClose)
            autoCloseRoutine = StartCoroutine(AutoCloseRoutine());
    }

    private IEnumerator AnimateDoor(bool opening)
    {
        float startAngle = GetCurrentRotation();
        float endAngle = opening ? currentOpenRotation : closedRotation;

        float timer = 0f;

        while (timer < animationTime)
        {
            timer += Time.deltaTime;

            float t = animationTime <= 0f ? 1f : Mathf.Clamp01(timer / animationTime);
            float easedT = easeCurve != null ? easeCurve.Evaluate(t) : t;

            float angle = Mathf.LerpAngle(startAngle, endAngle, easedT);
            ApplyRotation(angle);

            yield return null;
        }

        ApplyRotation(endAngle);
        animationRoutine = null;
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (isOpen)
            CloseDoor();

        autoCloseRoutine = null;
    }

    private float GetCurrentRotation()
    {
        Vector3 euler = target.localEulerAngles;

        switch (rotationAxis)
        {
            case RotationAxis.X: return euler.x;
            case RotationAxis.Y: return euler.y;
            case RotationAxis.Z: return euler.z;
            default: return 0f;
        }
    }

    private void ApplyRotation(float angle)
    {
        Vector3 euler = target.localEulerAngles;

        switch (rotationAxis)
        {
            case RotationAxis.X:
                euler.x = angle;
                break;

            case RotationAxis.Y:
                euler.y = angle;
                break;

            case RotationAxis.Z:
                euler.z = angle;
                break;
        }

        target.localEulerAngles = euler;
    }
}