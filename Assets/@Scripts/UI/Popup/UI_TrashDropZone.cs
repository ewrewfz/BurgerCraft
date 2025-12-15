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
        UI_BurgerStack stack = null;
        
        // 1. pointerDrag에서 직접 찾기
        if (eventData.pointerDrag != null)
        {
            stack = eventData.pointerDrag.GetComponent<UI_BurgerStack>();
            
            // 2. 재료 오브젝트인 경우 부모에서 찾기
            if (stack == null)
            {
                var ingredientHandler = eventData.pointerDrag.GetComponent<UI_IngredientDragHandler>();
                if (ingredientHandler != null)
                {
                    stack = eventData.pointerDrag.GetComponentInParent<UI_BurgerStack>();
                }
            }
            
            // 3. 부모에서 찾기
            if (stack == null)
            {
                stack = eventData.pointerDrag.GetComponentInParent<UI_BurgerStack>();
            }
        }
        
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

