using DG.Tweening;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_JoyStick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{

    [SerializeField]
    private GameObject _background;
    [SerializeField]
    private GameObject _cursor;

    private float _radius;
    private Vector2 _touchPos;

    // 페이드 IN,Out 변수
    private CanvasGroup _backgroundCanvasGroup;
    private CanvasGroup _cursorCanvasGroup;

    public void Start()
    {
        _radius = _background.GetComponent<RectTransform>().sizeDelta.y / 3;
        
        _backgroundCanvasGroup = _background.GetComponent<CanvasGroup>();
        if (_backgroundCanvasGroup == null)
            _backgroundCanvasGroup = _background.AddComponent<CanvasGroup>();
            
        _cursorCanvasGroup = _cursor.GetComponent<CanvasGroup>();
        if (_cursorCanvasGroup == null)
            _cursorCanvasGroup = _cursor.AddComponent<CanvasGroup>();
        
        _backgroundCanvasGroup.alpha = 0f;
        _cursorCanvasGroup.alpha = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _background.transform.position = eventData.position;
        _cursor.transform.position = eventData.position;
        _touchPos = eventData.position;
        
        _backgroundCanvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
        _cursorCanvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        _cursor.transform.position = _touchPos;

        GameManager.Instance.JoystickDir = Vector2.zero;
        
        _backgroundCanvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad);
        _cursorCanvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad);
    }
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchDir = (eventData.position - _touchPos);

        float moveDist = Mathf.Min(touchDir.magnitude, _radius);
        Vector2 moveDir = touchDir.normalized;
        Vector2 newPosition = _touchPos + moveDir * moveDist;
        _cursor.transform.position = newPosition;

        GameManager.Instance.JoystickDir = moveDir;
    }

}