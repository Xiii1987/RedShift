using UnityEngine;
using UnityEngine.UI;

public class UIButtonGroup : MonoBehaviour
{
    [Header("Buttons In This Group")]
    [SerializeField] private Button[] buttons;

    [Header("Settings")]
    [SerializeField] private int startingSelectedIndex = 0;
    [SerializeField] private bool selectStartingButtonOnStart = true;

    private UIButtonColourFeedback[] buttonFeedbacks;
    private int selectedIndex = -1;

    private void Awake()
    {
        CacheButtonFeedbacks();
        HookButtonEvents();
    }

    private void Start()
    {
        if (selectStartingButtonOnStart)
        {
            SelectButton(startingSelectedIndex);
        }
    }

    private void CacheButtonFeedbacks()
    {
        buttonFeedbacks = new UIButtonColourFeedback[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
            {
                continue;
            }

            buttonFeedbacks[i] = buttons[i].GetComponent<UIButtonColourFeedback>();
        }
    }

    private void HookButtonEvents()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int cachedIndex = i;

            if (buttons[i] != null)
            {
                buttons[i].onClick.AddListener(() => SelectButton(cachedIndex));
            }
        }
    }

    public void SelectButton(int index)
    {
        if (index < 0 || index >= buttons.Length)
        {
            return;
        }

        selectedIndex = index;

        for (int i = 0; i < buttonFeedbacks.Length; i++)
        {
            if (buttonFeedbacks[i] != null)
            {
                buttonFeedbacks[i].SetSelected(i == selectedIndex);
            }
        }
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }
}