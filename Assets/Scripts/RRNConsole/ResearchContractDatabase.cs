using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResearchClientData
{
    public string clientName;
    public Sprite clientLogo;
}

public class ResearchContractDatabase : MonoBehaviour
{
    [Header("Contract ID")]
    [SerializeField] private string contractIDPrefix = "RC-";

    [Header("Clients")]
    [SerializeField] private List<ResearchClientData> clients = new List<ResearchClientData>
    {
        new ResearchClientData { clientName = "Ministry of Applied Sciences" },
        new ResearchClientData { clientName = "Anglian Defence Board" },
        new ResearchClientData { clientName = "Royal Signals Research Office" },
        new ResearchClientData { clientName = "Northbridge Industrial Group" },
        new ResearchClientData { clientName = "Crown Technical Services" },
        new ResearchClientData { clientName = "Department of Experimental Works" },
        new ResearchClientData { clientName = "National Materials Bureau" },
        new ResearchClientData { clientName = "Commonwealth Electronics Authority" }
    };

    [Header("Departments")]
    [SerializeField] private List<string> departmentNames = new List<string>
    {
        "Data Analysis",
        "Materials Testing",
        "Radar Studies",
        "Prototype Review",
        "Signals Processing",
        "Cryogenic Monitoring",
        "Industrial Automation",
        "Solid State Research"
    };

    [Header("Contract Titles")]
    [SerializeField] private List<string> contractTitles = new List<string>
    {
        "Polymer Sample Analysis",
        "Alloy Stress Assessment",
        "Radar Signal Processing",
        "Cryogenic Pressure Survey",
        "Semiconductor Failure Investigation",
        "Telemetry Data Analysis",
        "Automation Timing Audit",
        "Reactor Shielding Assessment",
        "Communications Interference Study",
        "Precision Tooling Evaluation",
        "Materials Integrity Review",
        "Circuit Reliability Testing",
        "Signal Noise Investigation",
        "Prototype Performance Analysis",
        "Industrial Systems Audit",
        "Component Failure Review",
        "Structural Load Assessment",
        "Data Correlation Study",
        "Environmental Monitoring",
        "Calibration Verification"
    };

    [Header("Reward Ranges")]
    [SerializeField] private int minMoneyReward = 1000;
    [SerializeField] private int maxMoneyReward = 5000;

    [SerializeField] private int minResearchPointReward = 5;
    [SerializeField] private int maxResearchPointReward = 50;

    [Header("Duration In Game Hours")]
    [SerializeField] private int minDurationGameHours = 1;
    [SerializeField] private int maxDurationGameHours = 8;

    [Header("Game Clock Conversion")]
    [Tooltip("If 1 in-game minute takes 1 real second, leave this as 1.")]
    [SerializeField] private float realSecondsPerGameMinute = 1f;

    private readonly List<int> usedContractNumbers = new List<int>();

    public ResearchContract GenerateRandomContract()
    {
        string contractID = GenerateRandomContractID();

        ResearchClientData client = GetRandomClient();
        string department = GetRandomFromList(departmentNames);
        string contractName = GetRandomFromList(contractTitles);

        string description =
            $"{client.clientName} requires additional research server capacity to complete the project \"{contractName}\". " +
            $"Assign this contract to an available research server to begin processing.";

        int rewardMoney = GenerateRoundedMoneyReward();
        int rewardResearchPoints = GenerateRoundedResearchPointReward();

        int durationGameHours = UnityEngine.Random.Range(minDurationGameHours, maxDurationGameHours + 1);
        float durationSeconds = ConvertGameHoursToRealSeconds(durationGameHours);

        return new ResearchContract(
            contractID,
            contractName,
            description,
            client.clientName,
            client.clientLogo,
            department,
            rewardMoney,
            rewardResearchPoints,
            durationGameHours,
            durationSeconds
        );
    }

    public List<ResearchContract> GenerateRandomContracts(int amount)
    {
        List<ResearchContract> generatedContracts = new List<ResearchContract>();

        for (int i = 0; i < amount; i++)
        {
            generatedContracts.Add(GenerateRandomContract());
        }

        return generatedContracts;
    }

    private ResearchClientData GetRandomClient()
    {
        if (clients == null || clients.Count == 0)
        {
            return new ResearchClientData
            {
                clientName = "Unknown Client",
                clientLogo = null
            };
        }

        return clients[UnityEngine.Random.Range(0, clients.Count)];
    }

    private string GenerateRandomContractID()
    {
        int randomNumber = UnityEngine.Random.Range(1000, 10000);
        int safetyCounter = 0;

        while (usedContractNumbers.Contains(randomNumber) && safetyCounter < 1000)
        {
            randomNumber = UnityEngine.Random.Range(1000, 10000);
            safetyCounter++;
        }

        usedContractNumbers.Add(randomNumber);

        return $"{contractIDPrefix}{randomNumber}";
    }

    private int GenerateRoundedMoneyReward()
    {
        int rawReward = UnityEngine.Random.Range(minMoneyReward, maxMoneyReward + 1);
        return Mathf.RoundToInt(rawReward / 100f) * 100;
    }

    private int GenerateRoundedResearchPointReward()
    {
        int rawReward = UnityEngine.Random.Range(minResearchPointReward, maxResearchPointReward + 1);
        return Mathf.RoundToInt(rawReward / 5f) * 5;
    }

    private float ConvertGameHoursToRealSeconds(int gameHours)
    {
        int gameMinutes = gameHours * 60;
        return gameMinutes * realSecondsPerGameMinute;
    }

    private string GetRandomFromList(List<string> list)
    {
        if (list == null || list.Count == 0)
        {
            return "Unknown";
        }

        return list[UnityEngine.Random.Range(0, list.Count)];
    }
}