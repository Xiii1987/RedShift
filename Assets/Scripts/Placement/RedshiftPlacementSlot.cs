using UnityEngine;

public class RedshiftPlacementSlot : MonoBehaviour
{
    [Header("Grid Position")]
    [SerializeField] private Vector2Int gridCoordinate;

    [Header("Snap")]
    [SerializeField] private Transform snapPoint;

    [Header("Runtime")]
    [SerializeField] private RedshiftPlaceable occupyingObject;

    public Vector2Int GridCoordinate => gridCoordinate;
    public Transform SnapPoint => snapPoint != null ? snapPoint : transform;
    public bool IsOccupied => occupyingObject != null;
    public RedshiftPlaceable OccupyingObject => occupyingObject;

    public void Configure(Vector2Int coordinate)
    {
        gridCoordinate = coordinate;
    }

    public bool TryOccupy(RedshiftPlaceable placeable)
    {
        if (placeable == null || IsOccupied)
            return false;

        occupyingObject = placeable;
        return true;
    }

    public void Release(RedshiftPlaceable placeable)
    {
        if (occupyingObject == placeable)
            occupyingObject = null;
    }
}
