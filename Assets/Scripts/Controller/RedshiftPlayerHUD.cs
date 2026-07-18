using TMPro;
using UnityEngine;

public class RedshiftPlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI researchPointsText;

    private void Start()
    {
        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }

        RefreshHUD();
    }

    private void OnEnable()
    {
        GameClock.OnTimeChanged += RefreshHUD;
    }

    private void OnDisable()
    {
        GameClock.OnTimeChanged -= RefreshHUD;
    }

    private void RefreshHUD()
    {
        if (playerManager == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = $"£ {playerManager.Money:N0}";
        }

        if (researchPointsText != null)
        {
            researchPointsText.text = $"{playerManager.ResearchPoints:N0} RP";
        }
    }
}