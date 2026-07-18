using TMPro;
using UnityEngine;

public class RedshiftInteractionPromptUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject promptRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text actionText;

    private void Awake()
    {
        Hide();
    }

    public void Show(RedshiftInteractableInfo info)
    {
        if (info == null)
        {
            Hide();
            return;
        }

        promptRoot.SetActive(true);

        titleText.text = info.displayName;
        actionText.text = $"[ {info.actionText} ]";
    }

    public void Hide()
    {
        promptRoot.SetActive(false);
    }
}