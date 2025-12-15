using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 조립된 버거를 드래그하여 Trash로 버릴 수 있는 스택 컨테이너.
/// </summary>
public class UI_BurgerStack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<UI_BurgerStack> OnTrashDropped;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private RectTransform _canvasRectTransform;
    private bool _isDragging;
    private Vector3 _originPos;
    private Vector2 _dragOffset; // 드래그 시작 시 마우스와 오브젝트 위치의 오프셋

    private void Awake()
    {
        // RectTransform 캐싱
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UI_BurgerStack: RectTransform이 없습니다.", this);
            return;
        }

        _originPos = _rectTransform.anchoredPosition;
    }

    private void EnsureCanvas()
    {
        // Canvas를 매번 찾거나 캐싱된 것이 없으면 찾기
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                _canvas = FindObjectOfType<Canvas>();
            }
        }

        if (_canvas != null && _canvasRectTransform == null)
        {
            _canvasRectTransform = _canvas.transform as RectTransform;
        }
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragInternal(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        OnDragInternal(eventData);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        OnEndDragInternal(eventData);
    }

    // 외부에서 호출 가능하도록 public 메서드로 노출
    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragInternal(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragInternal(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragInternal(eventData);
    }

    private void OnBeginDragInternal(PointerEventData eventData)
    {
        if (_rectTransform == null)
            return;

        // Canvas를 미리 찾아두기
        EnsureCanvas();

        if (_canvas == null || _canvasRectTransform == null)
        {
            return;
        }

        // 드래그 시작 시 마우스 위치와 오브젝트 위치의 오프셋 계산
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                eventData.position,
                _canvas.worldCamera,
                out Vector2 localPoint))
        {
            _dragOffset = _rectTransform.anchoredPosition - localPoint;
        }
        else
        {
            _dragOffset = Vector2.zero;
        }

        _isDragging = true;
        _originPos = _rectTransform.anchoredPosition;
    }

    private void OnDragInternal(PointerEventData eventData)
    {
        if (!_isDragging || _rectTransform == null)
        {
            return;
        }

        // Canvas를 매번 확인하고 필요시 다시 찾기
        EnsureCanvas();

        if (_canvas == null || _canvasRectTransform == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                eventData.position,
                _canvas.worldCamera,
                out Vector2 localPoint))
        {
            // 마우스 위치에 오프셋을 더해서 오브젝트 위치 설정
            _rectTransform.anchoredPosition = localPoint + _dragOffset;
        }
    }

    private void OnEndDragInternal(PointerEventData eventData)
    {
        if (_rectTransform == null)
            return;

        _isDragging = false;

        // 드롭 위치 확인 - 여러 방법으로 시도
        UI_TrashDropZone trashZone = null;

        // 1. pointerEnter 확인
        if (eventData.pointerEnter != null)
        {
            trashZone = eventData.pointerEnter.GetComponent<UI_TrashDropZone>();
            if (trashZone == null)
            {
                trashZone = eventData.pointerEnter.GetComponentInParent<UI_TrashDropZone>();
            }
        }

        // 2. pointerEnter가 없으면 현재 위치에서 Raycast로 확인
        if (trashZone == null && _canvas != null)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            foreach (var result in results)
            {
                trashZone = result.gameObject.GetComponent<UI_TrashDropZone>();
                if (trashZone != null)
                    break;
                    
                if (trashZone == null)
                {
                    trashZone = result.gameObject.GetComponentInParent<UI_TrashDropZone>();
                    if (trashZone != null)
                        break;
                }
            }
        }

        // 3. RectTransform 위치로 확인 (TrashRoot 영역 내에 있는지)
        if (trashZone == null && _canvasRectTransform != null)
        {
            // 현재 오브젝트 위치를 월드 좌표로 변환
            Vector3[] worldCorners = new Vector3[4];
            _rectTransform.GetWorldCorners(worldCorners);
            Vector2 centerWorldPos = (worldCorners[0] + worldCorners[2]) * 0.5f;

            // 모든 TrashDropZone 찾기
            UI_TrashDropZone[] allTrashZones = FindObjectsOfType<UI_TrashDropZone>();
            foreach (var zone in allTrashZones)
            {
                RectTransform zoneRect = zone.GetComponent<RectTransform>();
                if (zoneRect != null)
                {
                    Vector3[] zoneCorners = new Vector3[4];
                    zoneRect.GetWorldCorners(zoneCorners);
                    
                    // 오브젝트 중심이 TrashZone 영역 내에 있는지 확인
                    if (centerWorldPos.x >= zoneCorners[0].x && centerWorldPos.x <= zoneCorners[2].x &&
                        centerWorldPos.y >= zoneCorners[0].y && centerWorldPos.y <= zoneCorners[2].y)
                    {
                        trashZone = zone;
                        break;
                    }
                }
            }
        }

        if (trashZone != null)
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
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}

