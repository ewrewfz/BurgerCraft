using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 조립된 버거를 드래그하여 Trash로 버릴 수 있는 스택 컨테이너.
/// </summary>
public class UI_BurgerStack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<UI_BurgerStack> OnTrashDropped;

    [SerializeField] private Canvas _canvas;
    [SerializeField] private Image _highlightImage;

    private RectTransform _rectTransform;
    private bool _isDragging;
    private Vector3 _originPos;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
        _originPos = _rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _originPos = _rectTransform.anchoredPosition;
        SetHighlight(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _canvas == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform,
                eventData.position,
                _canvas.worldCamera,
                out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        SetHighlight(false);

        // 드랍 대상이 TrashDropZone이면 해당 쪽에서 콜백 처리
        GameObject dropTarget = eventData.pointerEnter;
        if (dropTarget != null && dropTarget.GetComponent<UI_TrashDropZone>() != null)
        {
            OnTrashDropped?.Invoke(this);
            return;
        }

        // 실패 시 원위치
        _rectTransform.anchoredPosition = _originPos;
    }

    public void ClearStack()
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    private void SetHighlight(bool on)
    {
        if (_highlightImage != null)
            _highlightImage.enabled = on;
    }
}

