using UnityEngine;

public class RedshiftMaterialEmission : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private Color emissionColor = Color.white;

    private Material materialInstance;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
            materialInstance = targetRenderer.material; // Creates an instance
    }

    public void EnableEmission()
    {
        if (materialInstance == null)
            return;

        materialInstance.EnableKeyword("_EMISSION");
        materialInstance.SetColor("_EmissionColor", emissionColor);
    }

    public void DisableEmission()
    {
        if (materialInstance == null)
            return;

        materialInstance.SetColor("_EmissionColor", Color.black);
        materialInstance.DisableKeyword("_EMISSION");
    }

    public void ToggleEmission()
    {
        if (materialInstance == null)
            return;

        bool enabled = materialInstance.IsKeywordEnabled("_EMISSION");

        if (enabled)
            DisableEmission();
        else
            EnableEmission();
    }
}