using UnityEngine;
using UnityEngine.Events;

public class RedshiftUnityEventInteractable : MonoBehaviour, IRedshiftInteractable
{
    [Header("Interaction Event")]
    [SerializeField] private UnityEvent onStartInteraction;

    public void StartInteraction()
    {
        onStartInteraction?.Invoke();
    }
}