using UnityEngine;

public class ServerRackBlink : MonoBehaviour
{
    [Header("Renderer")]
    public Renderer targetRenderer;

    [Header("Emission Maps")]
    public Texture emission1;
    public Texture emission2;
    public Texture emission3;

    [Header("Timing")]
    public float switchInterval = 1f;

    private Material materialInstance;
    private Texture[] emissionMaps;
    private int currentIndex;

    private void Start()
    {
        materialInstance = targetRenderer.material;

        emissionMaps = new Texture[]
        {
            emission1,
            emission2,
            emission3
        };

        InvokeRepeating(nameof(SwapEmission), 0f, switchInterval);
    }

    private void SwapEmission()
    {
        materialInstance.SetTexture("_EmissionMap", emissionMaps[currentIndex]);

        currentIndex++;

        if (currentIndex >= emissionMaps.Length)
            currentIndex = 0;
    }
}