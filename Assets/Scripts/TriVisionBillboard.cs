using System.Collections;
using UnityEngine;

public class TriVisionBillboard : MonoBehaviour
{
    [Header("Poster Strips")]
    [Tooltip("Drag the triangular poster strips here from left to right.")]
    [SerializeField] private Transform[] strips;

    [Header("Game Clock")]
    [Tooltip("How many in-game minutes between poster changes.")]
    [SerializeField] private int changeEveryMinutes = 10;

    [Header("Rotation")]
    [Tooltip("A triangular prism has 3 sides, so each poster is 120 degrees apart.")]
    [SerializeField] private float rotationAmount = 120f;

    [Tooltip("How long each individual strip takes to rotate.")]
    [SerializeField] private float rotationDuration = 0.35f;

    [Tooltip("Delay before the next strip begins rotating.")]
    [SerializeField] private float delayBetweenStrips = 0.04f;

    [Tooltip("Local axis the strips rotate around. Usually Y for vertical strips.")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Options")]
    [SerializeField] private bool reverseRotation = false;
    [SerializeField] private bool reverseSweep = false;

    private int lastChangeMinute;
    private bool clockInitialised;
    private bool isChanging;


    private void OnEnable()
    {
        GameClock.OnTimeChanged += CheckTime;
    }


    private void OnDisable()
    {
        GameClock.OnTimeChanged -= CheckTime;
    }


    private void CheckTime()
    {
        if (GameClock.Instance == null)
            return;

        int currentMinutes = GameClock.Instance.GetCurrentMinutes();


        // The first time the clock talks to us,
        // just remember the current time.
        //
        // This stops the billboard immediately
        // changing when the day begins at 09:00.
        if (!clockInitialised)
        {
            lastChangeMinute = currentMinutes;
            clockInitialised = true;
            return;
        }


        // Has enough game time passed?
        if (currentMinutes - lastChangeMinute >= changeEveryMinutes)
        {
            lastChangeMinute = currentMinutes;

            if (!isChanging)
            {
                StartCoroutine(ChangePoster());
            }
        }
    }


    private IEnumerator ChangePoster()
    {
        isChanging = true;


        // Decide which direction the prisms rotate.
        float direction = reverseRotation ? -1f : 1f;


        // Go through every strip one by one.
        for (int i = 0; i < strips.Length; i++)
        {
            int stripIndex;


            // Normal:
            // 0, 1, 2, 3, 4...
            //
            // Reverse:
            // 8, 7, 6, 5, 4...
            if (reverseSweep)
            {
                stripIndex = strips.Length - 1 - i;
            }
            else
            {
                stripIndex = i;
            }


            Transform strip = strips[stripIndex];


            if (strip != null)
            {
                StartCoroutine(
                    RotateStrip(
                        strip,
                        rotationAmount * direction
                    )
                );
            }


            // Wait briefly before beginning the next strip.
            yield return new WaitForSeconds(delayBetweenStrips);
        }


        // Wait until the final strip has finished.
        yield return new WaitForSeconds(rotationDuration);


        isChanging = false;
    }


    private IEnumerator RotateStrip(Transform strip, float angle)
    {
        Quaternion startRotation = strip.localRotation;


        Quaternion addedRotation =
            Quaternion.AngleAxis(
                angle,
                rotationAxis.normalized
            );


        Quaternion targetRotation =
            startRotation * addedRotation;


        float timer = 0f;


        while (timer < rotationDuration)
        {
            timer += Time.deltaTime;


            float t = timer / rotationDuration;


            // Smooth acceleration/deceleration.
            t = Mathf.SmoothStep(0f, 1f, t);


            strip.localRotation =
                Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );


            yield return null;
        }


        // Make absolutely sure we finish
        // perfectly aligned with the next face.
        strip.localRotation = targetRotation;
    }


    // Handy for testing without waiting 10 game minutes.
    [ContextMenu("Test Poster Change")]
    private void TestPosterChange()
    {
        if (!isChanging)
        {
            StartCoroutine(ChangePoster());
        }
    }
}