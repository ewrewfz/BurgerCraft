using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 조리 성공 팝업 UI. 버거 조리가 성공했을 때 표시됩니다.
/// </summary>
public class UI_CookingComplete : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Image _StarImage;
    
    [Header("UI References")]
    [SerializeField] private Button _nextButton;
    
    public System.Action OnCompletePopupClosed;
    
    private Vector3 _starInitialPosition;
    private Tween _starTween;
    
    private void Awake()
    {
        // 별 이미지 초기 위치 저장
        if (_StarImage != null)
        {
            _starInitialPosition = _StarImage.transform.localPosition;
        }
        
        if (_nextButton != null)
        {
            _nextButton.onClick.AddListener(Hide);
        }
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 팝업 애니메이션
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        
        // 별 이미지 애니메이션 (위에서 아래로 떨어지는 효과)
        if (_StarImage != null)
        {
            // 초기 위치로 리셋
            _StarImage.transform.localPosition = _starInitialPosition;
            
            // 위에서 아래로 떨어지는 애니메이션
            _starTween = _StarImage.transform.DOLocalMoveY(650f, 1.5f);
        }
    }
    
    public void Hide()
    {
        // 트윈 정리
        if (_starTween != null)
        {
            _starTween.Kill();
            _starTween = null;
        }
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => 
                {
                    OnCompletePopupClosed?.Invoke();
                    gameObject.SetActive(false);
                    // PoolManager에 반환
                    if (PoolManager.Instance != null)
                    {
                        PoolManager.Instance.Push(gameObject);
                    }
                });
        }
        else
        {
            OnCompletePopupClosed?.Invoke();
            gameObject.SetActive(false);
            // PoolManager에 반환
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Push(gameObject);
            }
        }
    }
    
    private void OnDisable()
    {
        // 트윈 정리
        if (_starTween != null)
        {
            _starTween.Kill();
            _starTween = null;
        }
        
        // 별 이미지 위치 리셋
        if (_StarImage != null)
        {
            _StarImage.transform.localPosition = _starInitialPosition;
        }
    }
}

