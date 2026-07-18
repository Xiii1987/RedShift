using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRaycastUIDebug : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Input")]
    [SerializeField] private Key interactKey = Key.E;

    [Header("UI")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text infoText;

    private RaycastUIInfo currentInfo;
    private IPlayerInteractable currentInteractable;

    private void Start()
    {
        HideInfoPanel();
    }

    private void Update()
    {
        RefreshRaycast();

        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TryStartCurrentObject();
        }
    }

    private void RefreshRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            currentInfo = hit.collider.GetComponentInParent<RaycastUIInfo>();
            currentInteractable = hit.collider.GetComponentInParent<IPlayerInteractable>();

            if (currentInfo != null)
            {
                ShowInfoPanel(currentInfo.uiName);
                return;
            }
        }

        currentInfo = null;
        currentInteractable = null;
        HideInfoPanel();
    }

    private void TryStartCurrentObject()
    {
        currentInteractable?.StartInteraction();
    }

    private void ShowInfoPanel(string displayText)
    {
        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (infoText != null)
            infoText.text = $"[{interactKey}] - {displayText}";
    }

    private void HideInfoPanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}