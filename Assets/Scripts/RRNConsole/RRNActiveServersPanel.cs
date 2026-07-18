using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class RRNActiveServersPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

	[Header("Counters")]
	[SerializeField] private TextMeshProUGUI completedContractsCounterText;

    [Header("Scroll View")]
    [SerializeField] private Transform activeServerContentParent;
    [SerializeField] private RRNActiveServerEntryUI activeServerEntryPrefab;

    [Header("Settings")]
    [SerializeField] private float startupDelaySeconds = 1f;
    [SerializeField] private float refreshIntervalSeconds = 0.5f;

    private readonly List<RRNActiveServerEntryUI> pooledEntries = new();

    private IEnumerator Start()
    {
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }

        yield return new WaitForSeconds(startupDelaySeconds);

        RefreshActiveServers();

        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshIntervalSeconds);
            RefreshActiveServers();
        }
    }

    public void RefreshActiveServers()
    {
        if (playerManager == null || activeServerContentParent == null || activeServerEntryPrefab == null)
        {
            return;
        }

        IReadOnlyList<ResearchServer> servers = playerManager.RegisteredResearchServers;

        if (servers == null)
        {
            HideAllEntries();
            return;
        }

        int activeServerIndex = 0;

        for (int i = 0; i < servers.Count; i++)
        {
            ResearchServer server = servers[i];

            if (server == null || (!server.IsRunningContract && !server.IsCompletingContract))
            {
                continue;
            }

            RRNActiveServerEntryUI entry = GetOrCreateEntry(activeServerIndex);

            entry.gameObject.SetActive(true);
            entry.Setup(server);

            activeServerIndex++;
        }

        HideUnusedEntries(activeServerIndex);

		RefreshCompletedContractsCounter();
    }

    private RRNActiveServerEntryUI GetOrCreateEntry(int index)
    {
        while (pooledEntries.Count <= index)
        {
            RRNActiveServerEntryUI newEntry =
                Instantiate(activeServerEntryPrefab, activeServerContentParent);

            newEntry.gameObject.SetActive(false);
            pooledEntries.Add(newEntry);
        }

        return pooledEntries[index];
    }

    private void HideUnusedEntries(int usedCount)
    {
        for (int i = usedCount; i < pooledEntries.Count; i++)
        {
            if (pooledEntries[i] != null)
            {
                pooledEntries[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideAllEntries()
    {
        for (int i = 0; i < pooledEntries.Count; i++)
        {
            if (pooledEntries[i] != null)
            {
                pooledEntries[i].gameObject.SetActive(false);
            }
        }
    }

private void OnEnable()
{
    ResearchServer.OnAnyServerStateChanged += RefreshActiveServers;
    ResearchServer.OnAnyContractCompleted += RefreshCompletedContractsCounter;
}

private void OnDisable()
{
    ResearchServer.OnAnyServerStateChanged -= RefreshActiveServers;
    ResearchServer.OnAnyContractCompleted -= RefreshCompletedContractsCounter;
}
private void RefreshCompletedContractsCounter()
{
    if (completedContractsCounterText == null)
        return;

    completedContractsCounterText.text = ResearchServer.CompletedContractCount.ToString("N0");
}



}