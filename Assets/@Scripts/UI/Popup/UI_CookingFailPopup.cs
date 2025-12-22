using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조리 실패 팝업: 3회 실패 시 손님이 떠나도록 처리하는 팝업.
/// </summary>
public class UI_CookingFailPopup : MonoBehaviour
{
    [Header("Fail Images")]
    [SerializeField] private GameObject _failImage1;
    [SerializeField] private GameObject _failImage2;
    [SerializeField] private GameObject _failImage3;

    [Header("UI References")]
    [SerializeField] private Button _nextButton;

    private int _currentFailCount = 0;
    private const int MAX_FAIL_COUNT = 3;
    
    private GuestController _associatedGuest; // 이 팝업과 연결된 손님

    public System.Action OnNextButtonClicked;
    public System.Action OnMaxFailReached;

    private void Awake()
    {
        if (_nextButton != null)
        {
            _nextButton.onClick.RemoveAllListeners();
            _nextButton.onClick.AddListener(OnNextClicked);
        }

        ResetFailImages();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        
        // 손님이 연결되어 있으면 손님의 FailCount를 가져와서 동기화
        if (_associatedGuest != null)
        {
            _currentFailCount = _associatedGuest.FailCount;
        }
        
        UpdateFailImages();
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
        }
    }

    public void Hide()
    {
        // 이미 비활성화되어 있으면 풀에 이미 반환된 상태
        if (!gameObject.activeSelf)
            return;

        // PoolManager에 반환 (다음에 재활성화 처리)
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Push(gameObject);
        }
    }

    public void AddFailCount()
    {
        // 연결된 손님의 FailCount 먼저 증가
        if (_associatedGuest != null)
        {
            _associatedGuest.AddFailCount();
            // 손님의 FailCount로 동기화
            _currentFailCount = _associatedGuest.FailCount;
        }
        else
        {
            // 손님이 없으면 로컬 카운트만 증가
            if (_currentFailCount >= MAX_FAIL_COUNT)
                return;
            _currentFailCount++;
        }

        // FailCount가 최대치를 넘으면 더 이상 진행하지 않음
        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            _currentFailCount = MAX_FAIL_COUNT;
            UpdateFailImages();
            
            if (GameManager.Instance != null)
            {
                Utils.ApplyMoneyChange(-100, 2f, clampZero: true, animate: true);
            }
            // 최대 실패 호출 (Hide 전에)
            OnMaxFailReached?.Invoke();
            // 팝업 풀에 반환
            Hide();
            return;
        }
        
        UpdateFailImages();
    }

    private void UpdateFailImages()
    {
        // 손님이 연결되어 있으면 손님의 FailCount로 동기화
        if (_associatedGuest != null)
        {
            _currentFailCount = _associatedGuest.FailCount;
        }

        // 첫 번째 실패 이미지
        if (_failImage1 != null)
        {
            bool shouldBeActive = _currentFailCount >= 1;
            if (shouldBeActive)
            {
                if (!_failImage1.activeSelf)
                {
                    _failImage1.SetActive(true);
                    // DOTween 애니메이션: 스케일을 0에서 1로
                    RectTransform rectTransform = _failImage1.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.zero;
                        rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                    }
                }
            }
            else
            {
                _failImage1.SetActive(false);
            }
        }

        // 두 번째 실패 이미지
        if (_failImage2 != null)
        {
            bool shouldBeActive = _currentFailCount >= 2;
            if (shouldBeActive)
            {
                if (!_failImage2.activeSelf)
                {
                    _failImage2.SetActive(true);
                    // DOTween 애니메이션: 스케일을 0에서 1로
                    RectTransform rectTransform = _failImage2.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.zero;
                        rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                    }
                }
            }
            else
            {
                _failImage2.SetActive(false);
            }
        }

        // 세 번째 실패 이미지
        if (_failImage3 != null)
        {
            bool shouldBeActive = _currentFailCount >= 3;
            if (shouldBeActive)
            {
                if (!_failImage3.activeSelf)
                {
                    _failImage3.SetActive(true);
                    // DOTween 애니메이션: 스케일을 0에서 1로
                    RectTransform rectTransform = _failImage3.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.localScale = Vector3.zero;
                        rectTransform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack);
                    }
                }
            }
            else
            {
                _failImage3.SetActive(false);
            }
        }
    }

    public void ResetFailCount()
    {
        ResetFailImages();
    }

    private void ResetFailImages()
    {
        _currentFailCount = 0;
        
        // 연결된 손님의 FailCount도 초기화
        if (_associatedGuest != null)
        {
            _associatedGuest.ResetFailCount();
        }
        
        if (_failImage1 != null) _failImage1.SetActive(false);
        if (_failImage2 != null) _failImage2.SetActive(false);
        if (_failImage3 != null) _failImage3.SetActive(false);
    }

    private void OnNextClicked()
    {
        OnNextButtonClicked?.Invoke();
        Hide();
    }
}
