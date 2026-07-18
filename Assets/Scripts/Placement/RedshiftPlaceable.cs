using System.Collections.Generic;
using UnityEngine;

public class RedshiftPlaceable : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Vector2Int footprint = Vector2Int.one;
    [SerializeField] private Vector3 placementOffset;
	[SerializeField] private Vector3 placementRotationOffset = new Vector3(-90f, 0f, 0f);

	[Header("Collision Validation")]
	[SerializeField] private Vector3 collisionCheckCenter;
	[SerializeField] private Vector3 collisionCheckSize = Vector3.one;
	
	public Vector3 CollisionCheckCenter => collisionCheckCenter;

	public Vector3 CollisionCheckSize => new Vector3(
    Mathf.Max(0.01f, collisionCheckSize.x),
    Mathf.Max(0.01f, collisionCheckSize.y),
    Mathf.Max(0.01f, collisionCheckSize.z));

	public Vector3 PlacementRotationOffset => placementRotationOffset;

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
