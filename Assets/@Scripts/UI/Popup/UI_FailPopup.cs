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

    private GuestController _associatedGuest; // �� �˾��� ����� �մ�

    public Action OnAllFailsReached; // 3ȸ ���� �� ȣ��� �ݹ�
    public Action OnNextButtonClicked; // ���� �ܰ� ��ư Ŭ�� �� ȣ��� �ݹ� (�ֹ� �˾��� �ٽ� ���� ����)

    private void Awake()
    {

        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        }

        // �ʱ� ����: ��� �̹��� ��Ȱ��ȭ
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

        // �ִϸ��̼�
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
                    // PoolManager�� ��ȯ
                    if (PoolManager.Instance != null)
                    {
                        PoolManager.Instance.Push(gameObject);
                    }
                });
        }
        else
        {
            gameObject.SetActive(false);
            // PoolManager�� ��ȯ
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Push(gameObject);
            }
        }
    }

    /// <summary>
    /// �� �˾��� ����� �մ��� �����մϴ�.
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
    /// ���� ī��Ʈ�� ������Ű�� �ش� �̹����� Ȱ��ȭ�մϴ�.
    /// </summary>
    public void AddFailCount()
    {
        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            return;
        }

        _currentFailCount++;

        // ����� �մ��� ���� ī��Ʈ�� ����
        if (_associatedGuest != null)
        {
            _associatedGuest.AddFailCount();
        }

        // �ش� ���� �̹��� Ȱ��ȭ
        UpdateFailImages();

        // 3ȸ ���� �� �ݹ� ȣ��
        if (_currentFailCount >= MAX_FAIL_COUNT)
        {
            OnAllFailsReached?.Invoke();
        }
    }

    private void UpdateFailImages()
    {
        // ù ��° ���� �̹���
        if (_failImage1 != null)
        {
            bool shouldBeActive = _currentFailCount >= 1;
            if (shouldBeActive && !_failImage1.activeSelf)
            {
                _failImage1.SetActive(true);
                // DOTween �ִϸ��̼�: ������ 0���� 1��
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

        // �� ��° ���� �̹���
        if (_failImage2 != null)
        {
            bool shouldBeActive = _currentFailCount >= 2;
            if (shouldBeActive && !_failImage2.activeSelf)
            {
                _failImage2.SetActive(true);
                // DOTween �ִϸ��̼�: ������ 0���� 1��
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

        // �� ��° ���� �̹���
        if (_failImage3 != null)
        {
            bool shouldBeActive = _currentFailCount >= 3;
            if (shouldBeActive && !_failImage3.activeSelf)
            {
                _failImage3.SetActive(true);
                // DOTween �ִϸ��̼�: ������ 0���� 1��
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
    /// ���� ���� ī��Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    public int GetCurrentFailCount()
    {
        return _currentFailCount;
    }

    /// <summary>   
    /// ���� ī��Ʈ�� �����մϴ�.
    /// </summary>
    public void ResetFailCount()
    {
        ResetFailImages();
    }

    private void OnNextLevelButtonClick()
    {
        // 3ȸ ���а� �ƴϸ� �ֹ� �˾��� �ٽ� ������ �ݹ� ȣ��
        if (_currentFailCount < MAX_FAIL_COUNT)
        {
            OnNextButtonClicked?.Invoke();
        }

        Hide();
    }
}
