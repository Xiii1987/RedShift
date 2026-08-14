using UnityEngine;

public class RandomWhiteboard : MonoBehaviour
{
    [Header("Whiteboard Material")]
    [SerializeField] private Material whiteboardMaterial;

    [Header("Whiteboard Textures")]
    [SerializeField] private Texture2D[] whiteboardTextures;

    private void Start()
    {
        PickRandomWhiteboard();
    }

    private void PickRandomWhiteboard()
    {
        if (whiteboardMaterial == null)
        {
            Debug.LogWarning("RandomWhiteboard: No material assigned.");
            return;
        }

        if (whiteboardTextures == null || whiteboardTextures.Length == 0)
        {
            Debug.LogWarning("RandomWhiteboard: No textures assigned.");
            return;
        }

        int randomIndex = Random.Range(0, whiteboardTextures.Length);

        whiteboardMaterial.SetTexture(
            "_BaseMap",
            whiteboardTextures[randomIndex]
        );
    }
}