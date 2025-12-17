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
    private Image _image;
    private CanvasGroup _canvasGroup;
    private RectTransform _parentRectTransform;
    private bool _isDragging;
    private Vector3 _originPos;
    private Vector2 _dragOffset;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        // Canvas가 없으면 다시 찾기
        if (_canvas == null)
        {
            CacheComponents();
        }
    }

    private void CacheComponents()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();
        
        if (_parentRectTransform == null && _rectTransform != null && _rectTransform.parent != null)
            _parentRectTransform = _rectTransform.parent as RectTransform;
        
        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                _canvas = FindObjectOfType<Canvas>();
            }
        }
        
        if (_highlightImage == null)
            _highlightImage = GetComponent<Image>();
        
        if (_image == null)
            _image = GetComponent<Image>();
        
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
        
        if (_rectTransform != null)
            _originPos = _rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rectTransform == null)
            CacheComponents();
        
        if (_rectTransform == null)
            return;
        
        _isDragging = true;
        _originPos = _rectTransform.anchoredPosition;
        
        // Canvas가 없으면 다시 찾기
        if (_canvas == null)
        {
            CacheComponents();
        }
        
        // 부모 RectTransform 가져오기 (없으면 Canvas의 RectTransform 사용)
        RectTransform parentRect = _parentRectTransform;
        if (parentRect == null && _canvas != null)
        {
            parentRect = _canvas.transform as RectTransform;
        }
        if (parentRect == null)
        {
            parentRect = _rectTransform.root as RectTransform;
        }
        
        // 드래그 시작 시 마우스 위치와 오브젝트 위치의 오프셋 계산
        if (parentRect != null)
        {
            Camera cam = _canvas != null ? _canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    eventData.position,
                    cam,
                    out Vector2 mousePos))
            {
                _dragOffset = _rectTransform.anchoredPosition - mousePos;
            }
        }
        
        // 드래그 중인 오브젝트를 맨 위로 올리기
        transform.SetAsLastSibling();
        
        // CanvasGroup이 있으면 드래그 중 시각적 피드백
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0.8f;
        }
        
        SetHighlight(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _rectTransform == null)
            return;

        // Canvas가 없으면 다시 찾기
        if (_canvas == null)
        {
            CacheComponents();
        }
        
        // 부모 RectTransform 가져오기 (없으면 Canvas의 RectTransform 사용)
        RectTransform parentRect = _parentRectTransform;
        if (parentRect == null && _canvas != null)
        {
            parentRect = _canvas.transform as RectTransform;
        }
        if (parentRect == null)
        {
            parentRect = _rectTransform.root as RectTransform;
        }
        
        if (parentRect == null)
        {
            Debug.LogWarning("[UI_BurgerStack] 부모 RectTransform을 찾을 수 없습니다.");
            return;
        }

        Camera cam = _canvas != null ? _canvas.worldCamera : null;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                cam,
                out Vector2 localPoint);
        
        if (success)
        {
            // 오프셋을 적용하여 마우스 위치로 이동
            _rectTransform.anchoredPosition = localPoint + _dragOffset;
        }
        else
        {
            // 변환 실패 시 delta를 사용한 상대 이동으로 폴백
            Vector2 delta = eventData.delta / (_canvas != null ? _canvas.scaleFactor : 1f);
            _rectTransform.anchoredPosition += delta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        
        // CanvasGroup 복원
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
        
        SetHighlight(false);

        // 드랍 대상 찾기: pointerEnter와 Raycast 모두 확인
        UI_TrashDropZone dropZone = null;
        
        // 1. pointerEnter에서 찾기
        if (eventData.pointerEnter != null)
        {
            dropZone = eventData.pointerEnter.GetComponent<UI_TrashDropZone>();
            if (dropZone == null)
            {
                dropZone = eventData.pointerEnter.GetComponentInParent<UI_TrashDropZone>();
            }
        }
        
        // 2. 현재 마우스 위치에서 Raycast로 찾기
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

        // 3. 모든 TrashDropZone 찾아서 거리 확인
        if (dropZone == null)
        {
            UI_TrashDropZone[] allDropZones = FindObjectsOfType<UI_TrashDropZone>();
            
            if (_rectTransform != null && allDropZones.Length > 0)
            {
                Vector3 worldPos = _rectTransform.position;
                float minDistance = float.MaxValue;
                UI_TrashDropZone closestZone = null;
                
                foreach (var zone in allDropZones)
                {
                    if (zone == null || zone.gameObject == null) continue;
                    
                    RectTransform zoneRect = zone.GetComponent<RectTransform>();
                    if (zoneRect == null) continue;
                    
                    Vector3 zoneWorldPos = zoneRect.position;
                    float distance = Vector3.Distance(worldPos, zoneWorldPos);
                    
                    // 마우스 위치가 TrashDropZone 영역 안에 있는지 확인
                    if (RectTransformUtility.RectangleContainsScreenPoint(zoneRect, eventData.position, _canvas != null ? _canvas.worldCamera : null))
                    {
                        dropZone = zone;
                        break;
                    }
                    
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestZone = zone;
                    }
                }
                
                if (dropZone == null && closestZone != null && minDistance < 200f)
                {
                    dropZone = closestZone;
                }
            }
        }

        // TrashDropZone에 드롭된 경우
        if (dropZone != null)
        {
            // 콜백 호출
            if (OnTrashDropped != null)
            {
                OnTrashDropped.Invoke(this);
            }
            else
            {
                // 콜백이 없으면 직접 처리
                UI_CookingPopup popup = FindObjectOfType<UI_CookingPopup>();
                if (popup != null)
                {
                    popup.OnBurgerTrashed(this);
                }
            }
            return;
        }

        // 실패 시 원위치
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _originPos;
        }
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

