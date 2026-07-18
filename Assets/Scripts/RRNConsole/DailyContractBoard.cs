using System.Collections.Generic;
using UnityEngine;

public class DailyContractBoard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ResearchContractDatabase contractDatabase;

    [Header("Daily Board Settings")]
    [SerializeField] private int contractsPerDay = 5;
    [SerializeField] private bool generateContractsOnStart = true;

    [Header("Current Daily Contracts")]
    [SerializeField] private List<ResearchContract> currentContracts = new List<ResearchContract>();

    public List<ResearchContract> CurrentContracts => currentContracts;

    private void Start()
    {
        if (generateContractsOnStart)
        {
            RefreshDailyContracts();
        }
    }

public void RefreshContracts()
{
    RefreshDailyContracts();

    Debug.Log("Research contracts refreshed.");
}

    public void RefreshDailyContracts()
    {
        currentContracts.Clear();

        if (contractDatabase == null)
        {
            Debug.LogWarning("DailyContractBoard has no ResearchContractDatabase assigned.");
            return;
        }

        currentContracts = contractDatabase.GenerateRandomContracts(contractsPerDay);

        Debug.Log($"Generated {currentContracts.Count} daily research contracts.");
    }

    public ResearchContract GetContractByID(string contractID)
    {
        for (int i = 0; i < currentContracts.Count; i++)
        {
            if (currentContracts[i].contractID == contractID)
            {
                return currentContracts[i];
            }
        }

        return null;
    }

    public void RemoveContract(ResearchContract contract)
    {
        if (contract == null)
        {
            return;
        }

        if (currentContracts.Contains(contract))
        {
            currentContracts.Remove(contract);
        }
    }

    public void RemoveContractByID(string contractID)
    {
        ResearchContract contract = GetContractByID(contractID);

        if (contract != null)
        {
            currentContracts.Remove(contract);
        }
    }

    public bool HasAvailableContracts()
    {
        return currentContracts != null && currentContracts.Count > 0;
    }

    public int GetAvailableContractCount()
    {
        if (currentContracts == null)
        {
            return 0;
        }

        return currentContracts.Count;
    }

    public void PrintCurrentContractsToConsole()
    {
        if (currentContracts == null || currentContracts.Count == 0)
        {
            Debug.Log("No daily contracts available.");
            return;
        }

        for (int i = 0; i < currentContracts.Count; i++)
        {
            ResearchContract contract = currentContracts[i];

            Debug.Log(
                $"{contract.contractID} | {contract.contractName} | " +
                $"Client: {contract.clientName} | Reward: {contract.GetRewardText()} | " +
                $"Duration: {contract.GetDurationText()}"
            );
        }
    }
}