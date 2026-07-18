using System.Collections.Generic;
using UnityEngine;

public class RedshiftPlaceable : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField] private Vector3 placementOffset;

    private readonly List<RedshiftPlacementSlot> occupiedSlots = new();

    public Vector2Int Footprint => new(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
    public Vector3 PlacementOffset => placementOffset;
    public IReadOnlyList<RedshiftPlacementSlot> OccupiedSlots => occupiedSlots;

    public void RegisterOccupiedSlots(IEnumerable<RedshiftPlacementSlot> slots)
    {
        occupiedSlots.Clear();

        foreach (RedshiftPlacementSlot slot in slots)
        {
            if (slot != null)
                occupiedSlots.Add(slot);
        }
    }

    public void ReleaseSlots()
    {
        foreach (RedshiftPlacementSlot slot in occupiedSlots)
        {
            if (slot != null)
                slot.Release(this);
        }

        occupiedSlots.Clear();
    }

    private void OnDestroy()
    {
        ReleaseSlots();
    }
}
