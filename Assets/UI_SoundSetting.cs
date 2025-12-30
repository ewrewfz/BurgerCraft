using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 사운드 설정 UI - BGM, SFX, MASTER 볼륨을 컨트롤하는 팝업
/// </summary>
public class UI_SoundSetting : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;
    
    [Header("Volume Text")]
    [SerializeField] private TextMeshProUGUI _masterVolumeText;
    [SerializeField] private TextMeshProUGUI _bgmVolumeText;
    [SerializeField] private TextMeshProUGUI _sfxVolumeText;
    
    [Header("Volume Icons")]
    [SerializeField] private Image _masterVolumeIcon;
    [SerializeField] private Image _bgmVolumeIcon;
    [SerializeField] private Image _sfxVolumeIcon;
    
    [Header("Sprites")]
    [SerializeField] private Sprite _soundOnSprite;
    [SerializeField] private Sprite _soundOffSprite;
    
    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _githubButton;
    
    [Header("GitHub")]
    [SerializeField] private string _githubUrl = "https://github.com/ewrewfz";
    
    private SoundManager _soundManager;
    private bool _isInitializing = false;
    
    private void Awake()
    {
        _soundManager = SoundManager.Instance;
        
        // 슬라이더 이벤트 연결
        if (_masterSlider != null)
        {
            _masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (_bgmSlider != null)
        {
            _bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }
        
        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
        
        // 닫기 버튼 이벤트 연결
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
        
        // GitHub 버튼 이벤트 연결
        if (_githubButton != null)
        {
            _githubButton.onClick.AddListener(OnGithubButtonClick);
        }
    }
    
    private void OnEnable()
    {
        // 저장된 볼륨 설정 로드
        if (_soundManager != null)
        {
            _soundManager.LoadVolumeSettings();
        }
        
        // 슬라이더 값 초기화 (이벤트 발생 방지)
        _isInitializing = true;
        InitializeSliders();
        _isInitializing = false;
    }
    
    private void OnDisable()
    {
        // 볼륨 설정 저장
        if (_soundManager != null)
        {
            _soundManager.SaveVolumeSettings();
        }
    }
    
    /// <summary>
    /// 슬라이더를 현재 볼륨 값으로 초기화
    /// </summary>
    private void InitializeSliders()
    {
        if (_soundManager == null)
            return;
        
        // Master 볼륨
        if (_masterSlider != null)
        {
            _masterSlider.value = _soundManager.MasterVolume;
            UpdateMasterVolumeText(_soundManager.MasterVolume);
            UpdateMasterVolumeIcon(_soundManager.MasterVolume);
        }
        
        // BGM 볼륨
        if (_bgmSlider != null)
        {
            _bgmSlider.value = _soundManager.BGMVolume;
            UpdateBGMVolumeText(_soundManager.BGMVolume);
            UpdateBGMVolumeIcon(_soundManager.BGMVolume);
        }
        
        // SFX 볼륨
        if (_sfxSlider != null)
        {
            _sfxSlider.value = _soundManager.SFXVolume;
            UpdateSFXVolumeText(_soundManager.SFXVolume);
            UpdateSFXVolumeIcon(_soundManager.SFXVolume);
        }
    }
    
    /// <summary>
    /// Master 볼륨 변경 핸들러
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        if (_isInitializing || _soundManager == null)
            return;
        
        _soundManager.MasterVolume = value;
        UpdateMasterVolumeText(value);
        UpdateMasterVolumeIcon(value);
    }
    
    /// <summary>
    /// BGM 볼륨 변경 핸들러
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        if (_isInitializing || _soundManager == null)
            return;
        
        _soundManager.BGMVolume = value;
        UpdateBGMVolumeText(value);
        UpdateBGMVolumeIcon(value);
    }
    
    /// <summary>
    /// SFX 볼륨 변경 핸들러
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        if (_isInitializing || _soundManager == null)
            return;
        
        _soundManager.SFXVolume = value;
        UpdateSFXVolumeText(value);
        UpdateSFXVolumeIcon(value);
    }
    
    /// <summary>
    /// Master 볼륨 텍스트 업데이트
    /// </summary>
    private void UpdateMasterVolumeText(float volume)
    {
        if (_masterVolumeText != null)
        {
            _masterVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    
    /// <summary>
    /// BGM 볼륨 텍스트 업데이트
    /// </summary>
    private void UpdateBGMVolumeText(float volume)
    {
        if (_bgmVolumeText != null)
        {
            _bgmVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    
    /// <summary>
    /// SFX 볼륨 텍스트 업데이트
    /// </summary>
    private void UpdateSFXVolumeText(float volume)
    {
        if (_sfxVolumeText != null)
        {
            _sfxVolumeText.text = $"{Mathf.RoundToInt(volume * 100)}%";
        }
    }
    
    /// <summary>
    /// Master 볼륨 아이콘 업데이트
    /// </summary>
    private void UpdateMasterVolumeIcon(float volume)
    {
        if (_masterVolumeIcon != null)
        {
            _masterVolumeIcon.sprite = volume > 0f ? _soundOnSprite : _soundOffSprite;
        }
    }
    
    /// <summary>
    /// BGM 볼륨 아이콘 업데이트
    /// </summary>
    private void UpdateBGMVolumeIcon(float volume)
    {
        if (_bgmVolumeIcon != null)
        {
            _bgmVolumeIcon.sprite = volume > 0f ? _soundOnSprite : _soundOffSprite;
        }
    }
    
    /// <summary>
    /// SFX 볼륨 아이콘 업데이트
    /// </summary>
    private void UpdateSFXVolumeIcon(float volume)
    {
        if (_sfxVolumeIcon != null)
        {
            _sfxVolumeIcon.sprite = volume > 0f ? _soundOnSprite : _soundOffSprite;
        }
    }
    
    /// <summary>
    /// 팝업 숨기기
    /// </summary>
    public void Hide()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
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
            gameObject.SetActive(false);
            // PoolManager에 반환
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Push(gameObject);
            }
        }
    }
    
    /// <summary>
    /// 팝업 표시
    /// </summary>
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
    
    /// <summary>
    /// GitHub 버튼 클릭 핸들러
    /// </summary>
    private void OnGithubButtonClick()
    {
        if (!string.IsNullOrEmpty(_githubUrl))
        {
            Application.OpenURL(_githubUrl);
        }
        else
        {
            Debug.LogWarning("[UI_SoundSetting] GitHub URL이 설정되지 않았습니다.");
        }
    }
}
