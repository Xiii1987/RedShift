using System.Collections.Generic;
using UnityEngine;

public class RedshiftPlacementGhost : MonoBehaviour
{
    private readonly List<Renderer> renderers = new();
    private readonly List<Collider> colliders = new();
    private readonly List<Behaviour> disabledBehaviours = new();

    private Material validMaterial;
    private Material invalidMaterial;
    private bool isValid;

    public bool IsValid => isValid;

    public void Initialise(Material validGhostMaterial, Material invalidGhostMaterial)
    {
        validMaterial = validGhostMaterial;
        invalidMaterial = invalidGhostMaterial;

        renderers.AddRange(GetComponentsInChildren<Renderer>(true));
        colliders.AddRange(GetComponentsInChildren<Collider>(true));

        foreach (Collider ghostCollider in colliders)
            ghostCollider.enabled = false;

        foreach (Behaviour behaviour in GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour == this || behaviour is Renderer)
                continue;

            if (behaviour.enabled)
            {
                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);
            }
        }

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
            if (ghostRenderer != null)
                ghostRenderer.sharedMaterial = targetMaterial;
        }
    }
}
