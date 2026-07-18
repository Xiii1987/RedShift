using UnityEngine;
using UnityEngine.Events;

public class InteractableUnityEvent : MonoBehaviour, IPlayerInteractable
{
    [Header("Interaction Event")]
    [SerializeField] private UnityEvent onStartInteraction;

    public void StartInteraction()
    {
        onStartInteraction?.Invoke();
    }
}