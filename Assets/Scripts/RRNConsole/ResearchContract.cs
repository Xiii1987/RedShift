using System;
using UnityEngine;

[Serializable]
public class ResearchContract
{
    [Header("Contract Identity")]
    public string contractID;
    public string contractName;

    [TextArea(2, 5)]
    public string description;

    [Header("Client Info")]
    public string clientName;
    public Sprite clientLogo;
    public string departmentName;

    [Header("Contract Values")]
    public int rewardMoney;
    public int rewardResearchPoints;

    [Header("Duration")]
    public int durationGameHours;
    public float durationSeconds;

    [Header("Runtime State")]
    public bool isAssigned;
    public bool isCompleted;

    public ResearchContract(
        string contractID,
        string contractName,
        string description,
        string clientName,
        Sprite clientLogo,
        string departmentName,
        int rewardMoney,
        int rewardResearchPoints,
        int durationGameHours,
        float durationSeconds)
    {
        this.contractID = contractID;
        this.contractName = contractName;
        this.description = description;
        this.clientName = clientName;
        this.clientLogo = clientLogo;
        this.departmentName = departmentName;
        this.rewardMoney = rewardMoney;
        this.rewardResearchPoints = rewardResearchPoints;
        this.durationGameHours = durationGameHours;
        this.durationSeconds = durationSeconds;

        isAssigned = false;
        isCompleted = false;
    }

    public ResearchContract Clone()
    {
        return new ResearchContract(
            contractID,
            contractName,
            description,
            clientName,
            clientLogo,
            departmentName,
            rewardMoney,
            rewardResearchPoints,
            durationGameHours,
            durationSeconds
        );
    }

    public void MarkAssigned()
    {
        isAssigned = true;
    }

    public void MarkCompleted()
    {
        isCompleted = true;
    }

    public string GetRewardText()
    {
        return $"£{rewardMoney} / {rewardResearchPoints} RP";
    }

    public string GetDurationText()
    {
        return durationGameHours == 1 ? "1 hour" : $"{durationGameHours} hours";
    }
}