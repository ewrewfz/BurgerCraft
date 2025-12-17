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
        // TrashDropZone 감지 (재료를 드래그할 때도 처리)
        UI_TrashDropZone dropZone = null;
        
        if (eventData.pointerEnter != null)
        {
            dropZone = eventData.pointerEnter.GetComponent<UI_TrashDropZone>();
            if (dropZone == null)
            {
                dropZone = eventData.pointerEnter.GetComponentInParent<UI_TrashDropZone>();
            }
        }
        
        if (dropZone == null && EventSystem.current != null)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                dropZone = result.gameObject.GetComponent<UI_TrashDropZone>();
                if (dropZone == null)
                {
                    dropZone = result.gameObject.GetComponentInParent<UI_TrashDropZone>();
                }
                if (dropZone != null)
                    break;
            }
        }
        
        // TrashDropZone에 드롭된 경우 부모 스택 삭제
        if (dropZone != null && _parentStack != null)
        {
            UI_CookingPopup popup = FindObjectOfType<UI_CookingPopup>();
            if (popup != null)
            {
                popup.OnBurgerTrashed(_parentStack);
            }
            return;
        }
        
        // 일반 드래그 종료 처리
        if (_parentStack != null)
        {
            _parentStack.OnEndDrag(eventData);
        }
    }
}

