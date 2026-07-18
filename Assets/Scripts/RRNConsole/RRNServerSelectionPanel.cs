using System.Collections.Generic;
using UnityEngine;

public class RRNServerSelectionPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Scroll View")]
    [SerializeField] private Transform serverEntryContentParent;
    [SerializeField] private RRNServerSelectionEntryUI serverEntryPrefab;

    private readonly List<RRNServerSelectionEntryUI> spawnedEntries = new();

    private RRNServerSelectionEntryUI selectedEntry;
    private ResearchServer selectedServer;

    private void Start()
    {
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }

        RefreshServerList();
    }

    public void RefreshServerList()
    {
        ClearSpawnedEntries();

        if (playerManager == null || serverEntryContentParent == null || serverEntryPrefab == null)
        {
            return;
        }

        IReadOnlyList<ResearchServer> servers = playerManager.RegisteredResearchServers;

        for (int i = 0; i < servers.Count; i++)
        {
            ResearchServer server = servers[i];

            if (server == null || !server.CanAcceptContract())
            {
                continue;
            }

            RRNServerSelectionEntryUI entry =
                Instantiate(serverEntryPrefab, serverEntryContentParent);

            entry.Setup(server, HandleServerEntryClicked);
            entry.SetSelected(false);

            spawnedEntries.Add(entry);
        }
    }

    private void ClearSpawnedEntries()
    {
        selectedEntry = null;
        selectedServer = null;

        for (int i = spawnedEntries.Count - 1; i >= 0; i--)
        {
            if (spawnedEntries[i] != null)
            {
                Destroy(spawnedEntries[i].gameObject);
            }
        }

        spawnedEntries.Clear();
    }

   private void HandleServerEntryClicked(RRNServerSelectionEntryUI clickedEntry)
{
    UISoundManager.Instance?.PlayConfirm();

    selectedEntry = clickedEntry;
    selectedServer = clickedEntry.Server;

    for (int i = 0; i < spawnedEntries.Count; i++)
    {
        spawnedEntries[i].SetSelected(spawnedEntries[i] == selectedEntry);
    }
}

    public ResearchServer GetSelectedServer()
    {
        return selectedServer;
    }
	
	private void OnEnable()
{
    ResearchServer.OnAnyServerStateChanged += RefreshServerList;
}

private void OnDisable()
{
    ResearchServer.OnAnyServerStateChanged -= RefreshServerList;
}
	
	
}