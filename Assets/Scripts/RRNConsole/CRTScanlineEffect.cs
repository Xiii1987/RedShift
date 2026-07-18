using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class CRTScanlineEffect : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.015f;

    private RawImage rawImage;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    private void Update()
    {
        Rect uv = rawImage.uvRect;
        uv.y += scrollSpeed * Time.deltaTime;
        rawImage.uvRect = uv;
    }
}