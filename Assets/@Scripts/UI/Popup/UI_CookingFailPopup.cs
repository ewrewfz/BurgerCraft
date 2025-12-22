using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 조리 실패 팝업: 3회 실패 시 손님이 떠나는 대신 소지금에서 차감.
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
        UpdateFailImages();
    }

    public void Hide()
    {
        // 이미 비활성화되어 있으면 풀에 이미 반환된 상태
        if (!gameObject.activeSelf)
            return;

        // PoolManager에 반환 (내부에서 비활성화 처리)
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Push(gameObject);
        }
    }

    public void AddFailCount()
    {
        if (_currentFailCount >= MAX_FAIL_COUNT)
            return;

        _currentFailCount++;
        UpdateFailImages();

        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            if (GameManager.Instance != null)
            {
                Utils.ApplyMoneyChange(-100, 2f, clampZero: true, animate: true);
            }
            // 콜백 먼저 호출 (Hide 전에)
            OnMaxFailReached?.Invoke();
            // 그 다음 풀에 반환
            Hide();
            return;
        }
    }



    private void UpdateFailImages()
    {
        //if (_failImage1 != null) _failImage1.SetActive(_currentFailCount >= 1);
        //if (_failImage2 != null) _failImage2.SetActive(_currentFailCount >= 2);
        //if (_failImage3 != null) _failImage3.SetActive(_currentFailCount >= 3);

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

    public void ResetFailCount()
    {
        ResetFailImages();
    }

    private void ResetFailImages()
    {
        _currentFailCount = 0;
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
