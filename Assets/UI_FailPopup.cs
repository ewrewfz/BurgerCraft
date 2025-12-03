using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UI_FailPopup : MonoBehaviour
{
    [Header("Fail Images")]
    [SerializeField] private GameObject _failImage1;
    [SerializeField] private GameObject _failImage2;
    [SerializeField] private GameObject _failImage3;
    
    [Header("UI References")]
    [SerializeField] private Button _nextLevelButton;
    
    private int _currentFailCount = 0;
    private const int MAX_FAIL_COUNT = 3;
    
    private GuestController _associatedGuest; // 이 팝업과 연결된 손님
    
    public Action OnAllFailsReached; // 3회 실패 시 호출될 콜백
    public Action OnNextButtonClicked; // 다음 단계 버튼 클릭 시 호출될 콜백 (주문 팝업을 다시 열기 위한)
    
    private void Awake()
    {

        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        }
        
        // 초기 상태: 모든 이미지 비활성화
        ResetFailImages();
    }
    
  
    
    private void ResetFailImages()
    {
        if (_failImage1 != null) _failImage1.SetActive(false);
        if (_failImage2 != null) _failImage2.SetActive(false);
        if (_failImage3 != null) _failImage3.SetActive(false);
        _currentFailCount = 0;
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
        
        // 애니메이션
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }
    
    public void Hide()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => 
                {
                    gameObject.SetActive(false);
                    // 팝업이 완전히 닫힌 후 정리 작업은 필요시 호출하는 쪽에서 처리
                });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 이 팝업과 연결된 손님을 설정합니다.
    /// </summary>
    public void SetAssociatedGuest(GuestController guest)
    {
        _associatedGuest = guest;
        if (guest != null)
        {
            _currentFailCount = guest.FailCount;
            UpdateFailImages();
        }
    }
    
    /// <summary>
    /// 실패 카운트를 증가시키고 해당 이미지를 활성화합니다.
    /// </summary>
    public void AddFailCount()
    {
        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            return;
        }
        
        _currentFailCount++;
        
        // 연결된 손님의 실패 카운트도 증가
        if (_associatedGuest != null)
        {
            _associatedGuest.AddFailCount();
        }
        
        // 해당 실패 이미지 활성화
        UpdateFailImages();
        
        // 3회 실패 시 콜백 호출
        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            OnAllFailsReached?.Invoke();
        }
    }
    
    private void UpdateFailImages()
    {
        // 첫 번째 실패 이미지
        if (_failImage1 != null)
        {
            bool shouldBeActive = _currentFailCount >= 1;
            if (shouldBeActive && !_failImage1.activeSelf)
            {
                _failImage1.SetActive(true);
                // DOTween 애니메이션: 스케일 0에서 1로
                RectTransform rectTransform = _failImage1.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero;
                    rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                }
            }
            else if (!shouldBeActive)
            {
                _failImage1.SetActive(false);
            }
        }
        
        // 두 번째 실패 이미지
        if (_failImage2 != null)
        {
            bool shouldBeActive = _currentFailCount >= 2;
            if (shouldBeActive && !_failImage2.activeSelf)
            {
                _failImage2.SetActive(true);
                // DOTween 애니메이션: 스케일 0에서 1로
                RectTransform rectTransform = _failImage2.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero;
                    rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                }
            }
            else if (!shouldBeActive)
            {
                _failImage2.SetActive(false);
            }
        }
        
        // 세 번째 실패 이미지
        if (_failImage3 != null)
        {
            bool shouldBeActive = _currentFailCount >= 3;
            if (shouldBeActive && !_failImage3.activeSelf)
            {
                _failImage3.SetActive(true);
                // DOTween 애니메이션: 스케일 0에서 1로
                RectTransform rectTransform = _failImage3.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero;
                    rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                }
            }
            else if (!shouldBeActive)
            {
                _failImage3.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 현재 실패 카운트를 반환합니다.
    /// </summary>
    public int GetCurrentFailCount()
    {
        return _currentFailCount;
    }
    
    /// <summary>
    /// 실패 카운트를 리셋합니다.
    /// </summary>
    public void ResetFailCount()
    {
        ResetFailImages();
    }
    
    private void OnNextLevelButtonClick()
    {
        // 3회 실패가 아니면 주문 팝업을 다시 열도록 콜백 호출
        if (_currentFailCount < MAX_FAIL_COUNT)
        {
            OnNextButtonClicked?.Invoke();
        }
        
        Hide();
    }
}
