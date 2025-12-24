using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Guide : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _verticalMoveDistance = 0.5f; // 위아래 이동 거리
    [SerializeField] private float _verticalMoveDuration = 1f; // 위아래 이동 시간

    private Image _image;
    private Tweener _verticalMoveTweener;
    private Vector3 _basePosition;

    private void Awake()
    {
        // Image 컴포넌트 찾기
        _image = GetComponent<Image>();
        if (_image == null)
        {
            // Image가 없으면 자식에서 찾기
            _image = GetComponentInChildren<Image>();
        }

        _basePosition = transform.position;
    }



    private void OnEnable()
    {
        OnGuideEffect();
    }

    private void OnDisable()
    {
        OffGuideEffect();
    }

    /// <summary>
    /// 가이드 효과 시작 (위아래 애니메이션)
    /// </summary>
    private void OnGuideEffect()
    {
        // 위아래 이동 애니메이션
        StartVerticalAnimation();
    }

    /// <summary>
    /// 가이드 효과 중지
    /// </summary>
    private void OffGuideEffect()
    {
        // 애니메이션 중지
        if (_verticalMoveTweener != null && _verticalMoveTweener.IsActive())
        {
            _verticalMoveTweener.Kill();
            _verticalMoveTweener = null;
        }

        // 위치 복원
        transform.position = _basePosition;
    }

    /// <summary>
    /// 위아래로 움직이는 애니메이션 시작
    /// </summary>
    private void StartVerticalAnimation()
    {
        if (_verticalMoveTweener != null && _verticalMoveTweener.IsActive())
        {
            _verticalMoveTweener.Kill();
        }

        _basePosition = transform.position;
        Vector3 topPosition = _basePosition + Vector3.up * _verticalMoveDistance;
        Vector3 bottomPosition = _basePosition - Vector3.up * _verticalMoveDistance;

        // 위로 이동 후 아래로 이동하는 무한 반복 애니메이션
        _verticalMoveTweener = transform.DOMove(topPosition, _verticalMoveDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 특정 위치에 가이드를 표시합니다.
    /// </summary>
    public void ShowAtPosition(Vector3 position)
    {
        _basePosition = position + Vector3.up * 1.5f;
        transform.position = _basePosition;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 가이드를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

