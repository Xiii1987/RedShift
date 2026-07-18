using UnityEngine;
using System;
using System.Collections;

public class ResearchServer : MonoBehaviour
{
    [Header("Server Identity")]
    [SerializeField] private string serverName = "RS-80 Server";
    [SerializeField] private int serverID;

    [Header("Server State")]
    [SerializeField] private bool isAvailable = true;
    [SerializeField] private bool isRunningContract = false;

    [Header("Current Contract")]
    [SerializeField] private ResearchContract currentContract;
    [SerializeField] private float remainingTimeSeconds;

    [Header("Visuals")]
	[SerializeField] private Renderer activityRenderer;
	[SerializeField] private float idleEmissionStrength = 0.25f;
	[SerializeField] private float activeMinEmissionStrength = 0f;
	[SerializeField] private float activeMaxEmissionStrength = 2f;
	[SerializeField] private float blinkSpeed = 4f;
		
	[SerializeField] private float completedCardDelaySeconds = 3f;
	
	public static event Action OnAnyServerStateChanged;
	
    private PlayerManager playerManager;
	private Material activityMaterialInstance;
	private Color baseEmissionColour;
	private bool hasRegisteredWithPlayerManager;


    public string ServerName => serverName;
    public int ServerID => serverID;

    public bool IsAvailable => isAvailable;
    public bool IsRunningContract => isRunningContract;
    public bool IsBusy => isRunningContract;

    public ResearchContract CurrentContract => currentContract;
    public float RemainingTimeSeconds => remainingTimeSeconds;
	
	public static event Action OnAnyContractCompleted;

	private static int completedContractCount;

	public static int CompletedContractCount => completedContractCount;
	
	public bool IsCompletingContract => isCompletingContract;
	private bool isCompletingContract;

   private void Start()
{
    playerManager = FindAnyObjectByType<PlayerManager>();

    if (playerManager != null && !hasRegisteredWithPlayerManager)
    {
        playerManager.RegisterResearchServer(this);
        hasRegisteredWithPlayerManager = true;
    }
    else if (playerManager == null)
    {
        Debug.LogWarning($"{serverName} could not find PlayerManager and was not registered.");
    }

    if (activityRenderer != null)
    {
        // This creates a unique material instance for this server only.
        activityMaterialInstance = activityRenderer.material;

        activityMaterialInstance.EnableKeyword("_EMISSION");
        baseEmissionColour = activityMaterialInstance.GetColor("_EmissionColor");
    }

    SetIdleVisuals();
}

    private void OnDestroy()
    {
        if (playerManager != null && hasRegisteredWithPlayerManager)
        {
            playerManager.UnregisterResearchServer(this);
        }
    }

    private void Update()
    {
        if (!isRunningContract || currentContract == null || isCompletingContract)
{
    return;
}

        remainingTimeSeconds -= Time.deltaTime;

        UpdateActiveVisuals();

        if (remainingTimeSeconds <= 0f)
        {
            CompleteCurrentContract();
        }
    }

    public void SetServerIdentity(string newServerName, int newServerID)
    {
        serverName = newServerName;
        serverID = newServerID;

        gameObject.name = serverName;
    }

    public void AssignServerID(int newServerID)
    {
        serverID = newServerID;

        if (string.IsNullOrWhiteSpace(serverName) || serverName == "Research Server" || serverName == "RS-80 Server")
        {
            serverName = $"RS-80 Server {serverID:00}";
        }

        gameObject.name = serverName;
    }

    public bool CanAcceptContract()
    {
        return isAvailable && !isRunningContract;
    }

    public bool AssignContract(ResearchContract contract)
    {
        if (contract == null)
        {
            Debug.LogWarning($"{serverName} cannot accept a null contract.");
            return false;
        }

        if (!CanAcceptContract())
        {
            Debug.LogWarning($"{serverName} is already busy.");
            return false;
        }

        currentContract = contract;
        currentContract.MarkAssigned();

        remainingTimeSeconds = currentContract.durationSeconds;

        isAvailable = false;
        isRunningContract = true;
		OnAnyServerStateChanged?.Invoke();

        Debug.Log($"{serverName} started contract: {currentContract.contractName}");

        return true;
    }

private void CompleteCurrentContract()
{
    if (currentContract == null)
    {
        ResetServer();
        return;
    }

    isCompletingContract = true;
    remainingTimeSeconds = 0f;

    currentContract.MarkCompleted();

    completedContractCount++;
    OnAnyContractCompleted?.Invoke();

    if (playerManager != null)
    {
        playerManager.AddMoney(currentContract.rewardMoney);
        playerManager.AddResearchPoints(currentContract.rewardResearchPoints);
    }

	UISoundManager.Instance?.PlayComplete();
	
    OnAnyServerStateChanged?.Invoke();

    StartCoroutine(ResetServerAfterDelay());
}

private IEnumerator ResetServerAfterDelay()
{
    yield return new WaitForSeconds(completedCardDelaySeconds);

    ResetServer();
}

    private void ResetServer()
{
    currentContract = null;
    remainingTimeSeconds = 0f;

    isAvailable = true;
    isRunningContract = false;
	isCompletingContract = false;

    SetIdleVisuals();
	OnAnyServerStateChanged?.Invoke();
	
}

    private void SetIdleVisuals()
{
    SetEmissionStrength(idleEmissionStrength);
}

   private void UpdateActiveVisuals()
{
    float blinkValue = Mathf.PingPong(Time.time * blinkSpeed, 1f);

    float emissionStrength = Mathf.Lerp(
        activeMinEmissionStrength,
        activeMaxEmissionStrength,
        blinkValue
    );

    SetEmissionStrength(emissionStrength);
}

private void SetEmissionStrength(float strength)
{
    if (activityMaterialInstance == null)
    {
        return;
    }

    activityMaterialInstance.EnableKeyword("_EMISSION");
    activityMaterialInstance.SetColor("_EmissionColor", baseEmissionColour * strength);
}


    public string GetRemainingTimeText()
    {
        int hours = Mathf.FloorToInt(remainingTimeSeconds / 3600f);
        int minutes = Mathf.FloorToInt((remainingTimeSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(remainingTimeSeconds % 60f);

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    public string GetServerStatusText()
    {
        if (isRunningContract && currentContract != null)
        {
            return $"Running: {currentContract.contractName} | {GetRemainingTimeText()}";
        }

        return "Available";
    }
}