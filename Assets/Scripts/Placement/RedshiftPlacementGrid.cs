using System.Collections.Generic;
using UnityEngine;

public class RedshiftPlacementGrid : MonoBehaviour
{
    [Header("Grid Slots")]
    [SerializeField] private RedshiftPlacementSlot[] slots;
	
	[Header("Grid Visual")]
	[SerializeField] private GameObject gridVisual;

    private readonly Dictionary<Vector2Int, RedshiftPlacementSlot> slotLookup = new();

    private void Awake()
    {
        RebuildLookup();
		SetVisualsVisible(false);
    }

	public void SetVisualsVisible(bool visible)
{
    if (gridVisual != null)
        gridVisual.SetActive(visible);
}
	
    private void OnValidate()
    {
        if (!Application.isPlaying)
            RebuildLookup();
    }

    [ContextMenu("Collect Child Slots")]
    public void CollectChildSlots()
    {
        slots = GetComponentsInChildren<RedshiftPlacementSlot>(true);
        RebuildLookup();
    }

    public void RebuildLookup()
    {
        slotLookup.Clear();

        if (slots == null)
            return;

        foreach (RedshiftPlacementSlot slot in slots)
        {
            if (slot == null)
                continue;

            if (!slotLookup.TryAdd(slot.GridCoordinate, slot))
            {
                Debug.LogWarning($"Duplicate placement coordinate {slot.GridCoordinate} in {name}.", slot);
            }
        }
    }

    public RedshiftPlacementSlot GetSlot(Vector2Int coordinate)
    {
        slotLookup.TryGetValue(coordinate, out RedshiftPlacementSlot slot);
        return slot;
    }

    public bool TryGetFootprintSlots(
        RedshiftPlacementSlot origin,
        Vector2Int footprint,
        int rotationQuarterTurns,
        List<RedshiftPlacementSlot> results)
    {
        results.Clear();

        if (origin == null)
            return false;

        Vector2Int rotatedFootprint = GetRotatedFootprint(footprint, rotationQuarterTurns);

        for (int x = 0; x < rotatedFootprint.x; x++)
        {
            for (int y = 0; y < rotatedFootprint.y; y++)
            {
                Vector2Int coordinate = origin.GridCoordinate + new Vector2Int(x, y);
                RedshiftPlacementSlot slot = GetSlot(coordinate);

                if (slot == null)
                {
                    results.Clear();
                    return false;
                }

                results.Add(slot);
            }
        }

        return true;
    }

    public static Vector2Int GetRotatedFootprint(Vector2Int footprint, int rotationQuarterTurns)
    {
        int normalisedRotation = ((rotationQuarterTurns % 4) + 4) % 4;
        return normalisedRotation % 2 == 0
            ? footprint
            : new Vector2Int(footprint.y, footprint.x);
    }





}
