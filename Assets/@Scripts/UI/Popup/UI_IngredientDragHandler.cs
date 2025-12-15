using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 재료 오브젝트에 추가하여 부모 버거 스택의 드래그를 전달하는 핸들러
/// </summary>
public class UI_IngredientDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UI_BurgerStack _parentStack;

    public void SetParentStack(UI_BurgerStack parentStack)
    {
        _parentStack = parentStack;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_parentStack != null)
        {
            _parentStack.OnBeginDrag(eventData);
        }
        else
        {
            Debug.LogError($"[IngredientDragHandler] 부모 스택이 null입니다: {gameObject.name}");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_parentStack != null)
        {
            _parentStack.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_parentStack != null)
        {
            _parentStack.OnEndDrag(eventData);
        }
    }
}

