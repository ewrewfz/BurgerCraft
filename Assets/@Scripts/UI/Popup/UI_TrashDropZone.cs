using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버거 스택을 버릴 수 있는 쓰레기 존.
/// </summary>
public class UI_TrashDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _highlightImage;
    [SerializeField] private UI_CookingPopup _cookingPopup;

    public void OnDrop(PointerEventData eventData)
    {
        UI_BurgerStack stack = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UI_BurgerStack>() : null;
        if (stack != null)
        {
            _cookingPopup?.OnBurgerTrashed(stack);
        }
        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    private void SetHighlight(bool on)
    {
        if (_highlightImage != null)
            _highlightImage.enabled = on;
    }
}

