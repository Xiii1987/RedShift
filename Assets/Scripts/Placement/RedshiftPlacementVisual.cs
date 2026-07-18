using UnityEngine;

/// <summary>
/// Defines exactly which renderers from a placeable prefab should be used
/// when building its placement ghost. The real prefab is never instantiated
/// to create the preview.
/// </summary>
[DisallowMultipleComponent]
public class RedshiftPlacementVisual : MonoBehaviour
{
    [Header("Placement Preview")]
    [Tooltip("Only these renderers will be copied into the placement ghost.")]
    [SerializeField] private Renderer[] previewRenderers;

    public Renderer[] PreviewRenderers => previewRenderers;

    public bool HasPreviewRenderers
    {
        get
        {
            if (previewRenderers == null || previewRenderers.Length == 0)
                return false;

            foreach (Renderer previewRenderer in previewRenderers)
            {
                if (previewRenderer != null)
                    return true;
            }

            return false;
        }
    }
}