using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHighlight : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Graphic targetGraphic;

    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color highlightedColor = Color.yellow;

    private bool isSelected;

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetGraphic.color = highlightedColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
        {
            targetGraphic.color = normalColor;
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        targetGraphic.color = highlightedColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        targetGraphic.color = normalColor;
    }
}