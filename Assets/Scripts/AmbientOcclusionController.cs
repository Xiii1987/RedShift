using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AmbientOcclusionController : MonoBehaviour
{
    [Header("URP Renderer")]
    [SerializeField] private UniversalRendererData rendererData;

    [Header("Settings")]
    [SerializeField] private bool enableAmbientOcclusion = true;

    [Tooltip("Name of the renderer feature in your URP Renderer Data.")]
    [SerializeField] private string ambientOcclusionFeatureName = "Screen Space Ambient Occlusion";

    private ScriptableRendererFeature ambientOcclusionFeature;

    private void Awake()
    {
        FindAmbientOcclusionFeature();
        ApplyAmbientOcclusionState();
    }

    public void SetAmbientOcclusionEnabled(bool enabled)
    {
        enableAmbientOcclusion = enabled;
        ApplyAmbientOcclusionState();
    }

    public void ToggleAmbientOcclusion()
    {
        SetAmbientOcclusionEnabled(!enableAmbientOcclusion);
    }

    private void FindAmbientOcclusionFeature()
    {
        if (rendererData == null)
        {
            Debug.LogWarning("AmbientOcclusionController has no UniversalRendererData assigned.");
            return;
        }

        foreach (ScriptableRendererFeature feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.name == ambientOcclusionFeatureName)
            {
                ambientOcclusionFeature = feature;
                return;
            }
        }

        Debug.LogWarning($"Could not find renderer feature named: {ambientOcclusionFeatureName}");
    }

    private void ApplyAmbientOcclusionState()
    {
        if (ambientOcclusionFeature == null)
        {
            return;
        }

        ambientOcclusionFeature.SetActive(enableAmbientOcclusion);
    }
}