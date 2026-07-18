using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CRTFlicker : MonoBehaviour
{
    [SerializeField] private float flickerSpeed = 4f;
    [SerializeField] private float minimumBrightness = 0.98f;
    [SerializeField] private float maximumBrightness = 1f;

    private CanvasGroup canvasGroup;
    private float noiseOffset;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        noiseOffset = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);

        canvasGroup.alpha = Mathf.Lerp(
            minimumBrightness,
            maximumBrightness,
            noise
        );
    }
}