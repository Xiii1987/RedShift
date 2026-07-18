using UnityEngine;
using UnityEngine.InputSystem;

public class RedshiftPlayerInteractionRaycaster : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float rayDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private RedshiftInteractionPromptUI promptUI;

    private RedshiftInteractableInfo currentInfo;
    private IRedshiftInteractable currentInteractable;

    private void Update()
{
    if (RedshiftPlayerStateController.Instance != null &&
        !RedshiftPlayerStateController.Instance.CanInteract)
    {
        promptUI?.Hide();
        return;
    }

    RefreshRaycast();

    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
    {
        TryInteract();
    }
}

    private void RefreshRaycast()
    {
        currentInfo = null;
        currentInteractable = null;

        Ray ray = new Ray(transform.position, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            promptUI?.Hide();
            return;
        }

        currentInfo = hit.collider.GetComponentInParent<RedshiftInteractableInfo>();
        currentInteractable = hit.collider.GetComponentInParent<IRedshiftInteractable>();

        if (currentInfo == null)
        {
            promptUI?.Hide();
            return;
        }

        promptUI?.Show(currentInfo);
    }

    private void TryInteract()
    {
        currentInteractable?.StartInteraction();
    }
}