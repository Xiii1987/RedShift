using UnityEngine;
using UnityEngine.InputSystem;

public class RedshiftPlayerPickupRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private LayerMask pickupLayer;

    [Header("UI")]
    [SerializeField] private RedshiftInteractionPromptUI promptUI;

    private RedshiftWorldCratePickup currentPickup;
    private RedshiftInteractableInfo currentInfo;

    private void Update()
    {
        if (RedshiftPlayerStateController.Instance != null &&
            !RedshiftPlayerStateController.Instance.CanInteract)
        {
            ClearCurrentTarget();
            return;
        }

        RefreshRaycast();

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickup();
        }
    }

    private void RefreshRaycast()
    {
        currentPickup = null;
        currentInfo = null;

        Ray ray = new Ray(transform.position, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, pickupLayer))
        {
            promptUI?.Hide();
            return;
        }

        currentPickup = hit.collider.GetComponentInParent<RedshiftWorldCratePickup>();
        currentInfo = hit.collider.GetComponentInParent<RedshiftInteractableInfo>();

        if (currentPickup == null || currentInfo == null)
        {
            promptUI?.Hide();
            return;
        }

        promptUI?.Show(currentInfo);
    }

    private void TryPickup()
    {
        if (currentPickup == null)
            return;

        currentPickup.PickUp();
        ClearCurrentTarget();
    }

    private void ClearCurrentTarget()
    {
        currentPickup = null;
        currentInfo = null;
        promptUI?.Hide();
    }
}