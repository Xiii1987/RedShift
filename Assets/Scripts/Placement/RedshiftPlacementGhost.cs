using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RedshiftPlacementGhost : MonoBehaviour
{
    private readonly List<Renderer> renderers = new();

    private Material validMaterial;
    private Material invalidMaterial;
    private bool isValid;

    public bool IsValid => isValid;

    /// <summary>
    /// Builds a visual-only placement ghost from the explicitly assigned renderers
    /// on a RedshiftPlacementVisual component. The real prefab is never instantiated.
    /// </summary>
    public static RedshiftPlacementGhost CreateFromPrefab(
        GameObject sourcePrefab,
        Material validGhostMaterial,
        Material invalidGhostMaterial)
    {
        if (sourcePrefab == null)
        {
            Debug.LogError("Cannot create placement ghost because the source prefab is null.");
            return null;
        }

        RedshiftPlacementVisual placementVisual = sourcePrefab.GetComponent<RedshiftPlacementVisual>();
        if (placementVisual == null)
        {
            Debug.LogError($"Placeable prefab '{sourcePrefab.name}' is missing RedshiftPlacementVisual.");
            return null;
        }

        if (!placementVisual.HasPreviewRenderers)
        {
            Debug.LogError($"Placeable prefab '{sourcePrefab.name}' has no preview renderers assigned.");
            return null;
        }

        GameObject ghostRoot = new GameObject($"{sourcePrefab.name}_Ghost");
        RedshiftPlacementGhost ghost = ghostRoot.AddComponent<RedshiftPlacementGhost>();

        foreach (Renderer sourceRenderer in placementVisual.PreviewRenderers)
        {
            if (sourceRenderer == null)
                continue;

            Mesh sourceMesh = GetSourceMesh(sourceRenderer);
            if (sourceMesh == null)
            {
                Debug.LogWarning(
                    $"Placement preview renderer '{sourceRenderer.name}' on '{sourcePrefab.name}' " +
                    "does not have a supported mesh and was skipped.");
                continue;
            }

            GameObject visualObject = new GameObject(sourceRenderer.gameObject.name);
            visualObject.transform.SetParent(ghostRoot.transform, false);
            CopyRelativeTransform(sourcePrefab.transform, sourceRenderer.transform, visualObject.transform);

            MeshFilter meshFilter = visualObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = sourceMesh;

            MeshRenderer meshRenderer = visualObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            int materialSlotCount = Mathf.Max(1, sourceRenderer.sharedMaterials.Length);
            meshRenderer.sharedMaterials = CreateMaterialArray(validGhostMaterial, materialSlotCount);

            ghost.renderers.Add(meshRenderer);
        }

        if (ghost.renderers.Count == 0)
        {
            Debug.LogError($"Placement ghost for '{sourcePrefab.name}' could not create any renderers.");
            Destroy(ghostRoot);
            return null;
        }

        ghost.Initialise(validGhostMaterial, invalidGhostMaterial);
        return ghost;
    }

    public void Initialise(Material validGhostMaterial, Material invalidGhostMaterial)
    {
        validMaterial = validGhostMaterial;
        invalidMaterial = invalidGhostMaterial;

        if (renderers.Count == 0)
            renderers.AddRange(GetComponentsInChildren<Renderer>(true));

        SetValid(false);
    }

    public void SetValid(bool valid)
    {
        isValid = valid;
        Material targetMaterial = valid ? validMaterial : invalidMaterial;

        if (targetMaterial == null)
            return;

        foreach (Renderer ghostRenderer in renderers)
        {
            if (ghostRenderer == null)
                continue;

            int materialSlotCount = Mathf.Max(1, ghostRenderer.sharedMaterials.Length);
            ghostRenderer.sharedMaterials = CreateMaterialArray(targetMaterial, materialSlotCount);
        }
    }

    private static Mesh GetSourceMesh(Renderer sourceRenderer)
    {
        if (sourceRenderer is MeshRenderer)
        {
            MeshFilter meshFilter = sourceRenderer.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        if (sourceRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
            return skinnedMeshRenderer.sharedMesh;

        return null;
    }

    private static void CopyRelativeTransform(
        Transform sourceRoot,
        Transform sourceTransform,
        Transform destinationTransform)
    {
        destinationTransform.localPosition = sourceRoot.InverseTransformPoint(sourceTransform.position);
        destinationTransform.localRotation = Quaternion.Inverse(sourceRoot.rotation) * sourceTransform.rotation;

        Vector3 rootScale = sourceRoot.lossyScale;
        Vector3 sourceScale = sourceTransform.lossyScale;

        destinationTransform.localScale = new Vector3(
            SafeDivide(sourceScale.x, rootScale.x),
            SafeDivide(sourceScale.y, rootScale.y),
            SafeDivide(sourceScale.z, rootScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }

    private static Material[] CreateMaterialArray(Material material, int count)
    {
        Material[] materials = new Material[count];

        for (int i = 0; i < materials.Length; i++)
            materials[i] = material;

        return materials;
    }
}