using UnityEngine;

public class RedshiftObjectToggle : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("Settings")]
    [SerializeField] private bool startEnabled = true;

    private bool isEnabled;

    private void Awake()
    {
        isEnabled = startEnabled;
        ApplyState();
    }

    public void Toggle()
{
    Debug.Log("RedshiftObjectToggle Toggle called on: " + gameObject.name);

    isEnabled = !isEnabled;
    ApplyState();
}
    public void EnableObjects()
    {
        isEnabled = true;
        ApplyState();
    }

    public void DisableObjects()
    {
        isEnabled = false;
        ApplyState();
    }

    public void SetState(bool enabled)
    {
        isEnabled = enabled;
        ApplyState();
    }

    private void ApplyState()
{
    Debug.Log("Applying toggle state: " + isEnabled);

    foreach (GameObject target in targetObjects)
    {
        if (target != null)
        {
            Debug.Log("Setting " + target.name + " active: " + isEnabled);
            target.SetActive(isEnabled);
        }
    }
}
}