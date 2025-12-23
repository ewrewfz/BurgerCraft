using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World Space Progress Bar UI. 진행률을 표시하고 완료 시 콜백을 호출합니다.
/// </summary>
public class UI_Progressbar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _fillImage;
    
    [Header("Settings")]
    [SerializeField] private float _duration = 20f;
    [SerializeField] private float _rotationSpeed = 2f; // 회전 속도 (낮을수록 느림)
    
    public System.Action OnProgressComplete;
    
    private Coroutine _progressCoroutine;
    private float _currentProgress = 0f;
    private Camera _mainCamera;
    
    private void Awake()
    {
        // Fill Image 자동 찾기
        if (_fillImage == null)
        {
            Transform fillTransform = transform.Find("Progressbar/ProgressbarBG/Fill_Image");
            if (fillTransform != null)
            {
                _fillImage = fillTransform.GetComponent<Image>();
            }
        }
        
        // 초기 진행률 0으로 설정
        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }
        
        // 메인 카메라 찾기
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    private void OnEnable()
    {
        // 활성화 시 진행률 초기화
        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }
        _currentProgress = 0f;
    }
    
    /// <summary>
    /// 진행바를 시작합니다.
    /// </summary>
    public void StartProgress(float duration = -1f)
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
        }
        
        float progressDuration = duration > 0f ? duration : _duration;
        _progressCoroutine = StartCoroutine(CoUpdateProgress(progressDuration));
    }
    
    /// <summary>
    /// 진행바를 중지합니다.
    /// </summary>
    public void StopProgress()
    {
        if (_progressCoroutine != null)
        {
            StopCoroutine(_progressCoroutine);
            _progressCoroutine = null;
        }
    }
    
    /// <summary>
    /// 진행률을 업데이트하는 코루틴
    /// </summary>
    private IEnumerator CoUpdateProgress(float duration)
    {
        _currentProgress = 0f;
        
        if (_fillImage != null)
        {
            _fillImage.fillAmount = 0f;
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _currentProgress = elapsed / duration;
            
            if (_fillImage != null)
            {
                _fillImage.fillAmount = _currentProgress;
            }
            
            yield return null;
        }
        
        // 완료
        if (_fillImage != null)
        {
            _fillImage.fillAmount = 1f;
        }
        
        _progressCoroutine = null;
        OnProgressComplete?.Invoke();
    }
    
    /// <summary>
    /// 진행률을 직접 설정합니다 (0~1).
    /// </summary>
    public void SetProgress(float progress)
    {
        _currentProgress = Mathf.Clamp01(progress);
        
        if (_fillImage != null)
        {
            _fillImage.fillAmount = _currentProgress;
        }
    }
    
    /// <summary>
    /// 진행바를 숨깁니다.
    /// </summary>
    public void Hide()
    {
        StopProgress();
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 빌보드 효과: 카메라를 자연스럽게 따라가도록 회전
    /// </summary>
    private void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // 카메라의 forward 방향을 정확히 반영하여 회전 설정
            Quaternion targetRotation = Quaternion.LookRotation(-_mainCamera.transform.forward, _mainCamera.transform.up);
            
            // 부드럽게 회전 (Slerp 사용)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
        }
    }
}

