using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation")]
    [SerializeField] private RotationAxis axis = RotationAxis.Y;

    [SerializeField]
    private float speed = 20f; // Degrees per second

    private void Update()
    {
        Vector3 rotation = Vector3.zero;

        switch (axis)
        {
            case RotationAxis.X:
                rotation.x = speed * Time.deltaTime;
                break;

            case RotationAxis.Y:
                rotation.y = speed * Time.deltaTime;
                break;

            case RotationAxis.Z:
                rotation.z = speed * Time.deltaTime;
                break;
        }

        transform.Rotate(rotation, Space.Self);
    }
}