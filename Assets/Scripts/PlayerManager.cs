using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Player Resources")]
    [SerializeField] private int startingMoney = 10000;
    [SerializeField] private int startingResearchPoints = 0;

    [Header("Registered Machines")]
    [SerializeField] private List<ResearchServer> ownedResearchServers = new();

    private int nextServerID = 1;

    public int Money => startingMoney;
	public int ResearchPoints => startingResearchPoints;
    public int ServerCount => ownedResearchServers.Count;

    public IReadOnlyList<ResearchServer> RegisteredResearchServers => ownedResearchServers;

    public int AvailableServerCount
    {
        get
        {
            int count = 0;

            foreach (ResearchServer server in ownedResearchServers)
            {
                if (server != null && !server.IsBusy)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int BusyServerCount
    {
        get
        {
            int count = 0;

            foreach (ResearchServer server in ownedResearchServers)
            {
                if (server != null && server.IsBusy)
                {
                    count++;
                }
            }

            return count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        
    }

    public void AddMoney(int amount)
{
    if (amount <= 0)
    {
        return;
    }

    startingMoney += amount;
    Debug.Log($"Money added: £{amount}. Total: £{startingMoney}");
}

   public bool SpendMoney(int amount)
{
    if (amount <= 0)
    {
        return false;
    }

    if (startingMoney < amount)
    {
        Debug.Log("Not enough money.");
        return false;
    }

    startingMoney -= amount;
    Debug.Log($"Money spent: £{amount}. Total: £{startingMoney}");
    return true;
}

    public void AddResearchPoints(int amount)
{
    if (amount <= 0)
    {
        return;
    }

    startingResearchPoints += amount;
    Debug.Log($"Research points added: {amount}. Total: {startingResearchPoints}");
}

   public bool SpendResearchPoints(int amount)
{
    if (amount <= 0)
    {
        return false;
    }

    if (startingResearchPoints < amount)
    {
        Debug.Log("Not enough research points.");
        return false;
    }

    startingResearchPoints -= amount;
    Debug.Log($"Research points spent: {amount}. Total: {startingResearchPoints}");
    return true;
}

    public void RegisterResearchServer(ResearchServer server)
    {
        if (server == null)
        {
            return;
        }

        if (ownedResearchServers.Contains(server))
        {
            return;
        }

        server.AssignServerID(nextServerID);
        nextServerID++;

        ownedResearchServers.Add(server);

        Debug.Log($"Registered research server: {server.ServerName}");
        Debug.Log($"Total servers: {ServerCount}, Available: {AvailableServerCount}, Busy: {BusyServerCount}");
    }

    public void UnregisterResearchServer(ResearchServer server)
    {
        if (server == null)
        {
            return;
        }

        if (ownedResearchServers.Contains(server))
        {
            ownedResearchServers.Remove(server);

            Debug.Log($"Unregistered research server: {server.ServerName}");
            Debug.Log($"Total servers: {ServerCount}, Available: {AvailableServerCount}, Busy: {BusyServerCount}");
        }
    }

    public List<ResearchServer> GetOwnedResearchServers()
    {
        return ownedResearchServers;
    }

    public List<ResearchServer> GetAvailableResearchServers()
    {
        List<ResearchServer> availableServers = new();

        foreach (ResearchServer server in ownedResearchServers)
        {
            if (server != null && !server.IsBusy)
            {
                availableServers.Add(server);
            }
        }

        return availableServers;
    }

    public List<ResearchServer> GetBusyResearchServers()
    {
        List<ResearchServer> busyServers = new();

        foreach (ResearchServer server in ownedResearchServers)
        {
            if (server != null && server.IsBusy)
            {
                busyServers.Add(server);
            }
        }

        return busyServers;
    }

    [ContextMenu("Print Registered Servers")]
    private void PrintRegisteredServers()
    {
        foreach (ResearchServer server in ownedResearchServers)
        {
            if (server != null)
            {
                Debug.Log(server.ServerName);
            }
        }
    }
}