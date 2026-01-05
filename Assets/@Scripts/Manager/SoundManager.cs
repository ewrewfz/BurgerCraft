using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using static Define;

/// <summary>
/// 사운드 관리자 - BGM 및 SFX 재생을 담당
/// 최적화: 오디오 소스 풀링, 클립 캐싱
/// 확장성: 사운드 타입별 관리, 볼륨 조절, 페이드 효과
/// </summary>
public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource _bgmSource; // BGM 전용 오디오 소스
    [SerializeField] private int _sfxPoolSize = 10; // SFX 오디오 소스 풀 크기
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float _masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _bgmVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _sfxVolume = 1f;
    
    [Header("BGM Settings")]
    [SerializeField] private bool _bgmFadeEnabled = true;
    [SerializeField] private float _bgmFadeDuration = 1f;
    
    // 오디오 클립 캐싱 (이름으로 찾기)
    private Dictionary<string, AudioClip> _audioClipCache = new Dictionary<string, AudioClip>();
    
    // BGM/SFX 배열 (인덱스로 찾기)
    private List<AudioClip> _bgmClips = new List<AudioClip>();
    private List<AudioClip> _sfxClips = new List<AudioClip>();
    
    // 이름으로 인덱스 찾기
    private Dictionary<string, int> _bgmNameToIndex = new Dictionary<string, int>();
    private Dictionary<string, int> _sfxNameToIndex = new Dictionary<string, int>();
    
    // SFX 오디오 소스 풀
    private Queue<AudioSource> _sfxSourcePool = new Queue<AudioSource>();
    private List<AudioSource> _activeSfxSources = new List<AudioSource>();
    private Transform _sfxPoolParent;
    
    // 현재 재생 중인 BGM
    private string _currentBGMName = string.Empty;
    private Coroutine _bgmFadeCoroutine;
    
    // 캐싱 완료 플래그
    private bool _isCachingComplete = false;
    public bool IsCachingComplete => _isCachingComplete;
    
    // 볼륨 프로퍼티
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            _masterVolume = Mathf.Clamp01(value);
            UpdateAllVolumes();
        }
    }
    
    public float BGMVolume
    {
        get => _bgmVolume;
        set
        {
            _bgmVolume = Mathf.Clamp01(value);
            UpdateBGMVolume();
        }
    }
    
    public float SFXVolume
    {
        get => _sfxVolume;
        set
        {
            _sfxVolume = Mathf.Clamp01(value);
            UpdateSFXVolume();
        }
    }
    
    protected void Awake()
    {
        // 저장된 볼륨 설정 로드 (게임 시작 시 자동 로드)
        LoadVolumeSettings();
        
        // 오디오 소스 초기화
        InitializeAudioSources();
        
        // 모든 사운드 캐싱 (비동기)
        StartCoroutine(CoCacheAllAudioClips());
    }
    
    /// <summary>
    /// 모든 오디오 클립을 미리 캐싱 (Addressables 사용)
    /// </summary>
    private IEnumerator CoCacheAllAudioClips()
    {
        // Addressables에서 "Sounds" 라벨로 모든 오디오 클립 로드 시도
        AsyncOperationHandle<IList<AudioClip>> handle = default;
        
        try
        {
            handle = Addressables.LoadAssetsAsync<AudioClip>(
                "Sounds", 
                null, 
                Addressables.MergeMode.Union
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SoundManager] Addressables 초기화 실패: {e.Message}");
            Debug.LogError("[SoundManager] Addressables 그룹을 설정하고 콘텐츠를 빌드해야 합니다.");
            yield break;
        }
        
        yield return handle;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[SoundManager] 오디오 클립 로드 실패: {handle.OperationException?.Message}");
            Debug.LogError("[SoundManager] Addressables 설정 확인:");
            Debug.LogError("1. Window > Asset Management > Addressables > Groups");
            Debug.LogError("2. @Resources/Sounds 폴더의 오디오 클립들을 Addressables 그룹에 추가");
            Debug.LogError("3. 모든 오디오 클립에 'Sounds' 라벨 추가");
            Debug.LogError("4. Addressables Groups 창에서 'Build' > 'New Build' > 'Default Build Script' 실행");
            
            // 핸들 해제
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            
            yield break;
        }
        
        IList<AudioClip> allClips = handle.Result;
        
        if (allClips == null || allClips.Count == 0)
        {
            Debug.LogWarning("[SoundManager] 오디오 클립을 찾을 수 없습니다. Addressables 그룹에서 'Sounds' 라벨을 확인하세요.");
            
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
            
            yield break;
        }
        
        int bgmIndex = 0;
        int sfxIndex = 0;
        
        foreach (AudioClip clip in allClips)
        {
            if (clip == null)
                continue;
            
            string clipName = clip.name;
            
            // 캐시에 추가 (이름으로 찾기)
            _audioClipCache[clipName] = clip;
            
            // BGM과 SFX 분류
            if (clipName.StartsWith("BGM_"))
            {
                _bgmClips.Add(clip);
                _bgmNameToIndex[clipName] = bgmIndex;
                bgmIndex++;
            }
            else if (clipName.StartsWith("SFX_"))
            {
                _sfxClips.Add(clip);
                _sfxNameToIndex[clipName] = sfxIndex;
                sfxIndex++;
            }
        }
        
        // 캐싱 완료 플래그 설정
        _isCachingComplete = true;
    }
    
    /// <summary>
    /// 오디오 소스 초기화
    /// </summary>
    private void InitializeAudioSources()
    {
        // BGM 소스가 없으면 생성
        if (_bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM_Source");
            bgmObject.transform.SetParent(transform);
            _bgmSource = bgmObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
        }
        
        // SFX 풀 부모 오브젝트 생성
        GameObject poolParent = new GameObject("SFX_Pool");
        poolParent.transform.SetParent(transform);
        _sfxPoolParent = poolParent.transform;
        
        // SFX 오디오 소스 풀 생성
        for (int i = 0; i < _sfxPoolSize; i++)
        {
            GameObject sfxObject = new GameObject($"SFX_Source_{i}");
            sfxObject.transform.SetParent(_sfxPoolParent);
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _sfxSourcePool.Enqueue(source);
        }
        
        UpdateAllVolumes();
    }
    
    #region BGM
    
    /// <summary>
    /// BGM 재생 (이름으로)
    /// </summary>
    /// <param name="clipName">오디오 클립 이름</param>
    /// <param name="fadeIn">페이드 인 사용 여부</param>
    public void PlayBGM(string clipName, bool fadeIn = true)
    {
        if (string.IsNullOrEmpty(clipName))
            return;
        
        // 캐싱이 완료되지 않았으면 대기 후 재생
        if (!_isCachingComplete)
        {
            StartCoroutine(CoWaitForCachingAndPlayBGM(clipName, fadeIn));
            return;
        }
        
        // 같은 BGM이면 재생하지 않음
        if (_currentBGMName == clipName && _bgmSource.isPlaying)
            return;
        
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM 클립을 찾을 수 없습니다: {clipName}");
            return;
        }
        
        PlayBGM(clip, fadeIn);
    }
    
    /// <summary>
    /// 캐싱 완료를 기다린 후 BGM 재생
    /// </summary>
    private IEnumerator CoWaitForCachingAndPlayBGM(string clipName, bool fadeIn)
    {
        yield return new WaitUntil(() => _isCachingComplete);
        PlayBGM(clipName, fadeIn);
    }
    
    /// <summary>
    /// BGM 재생 (인덱스로)
    /// </summary>
    /// <param name="index">BGM 인덱스</param>
    /// <param name="fadeIn">페이드 인 사용 여부</param>
    public void PlayBGM(int index, bool fadeIn = true)
    {
        if (index < 0 || index >= _bgmClips.Count)
        {
            Debug.LogWarning($"[SoundManager] BGM 인덱스가 범위를 벗어났습니다: {index} (최대: {_bgmClips.Count - 1})");
            return;
        }
        
        AudioClip clip = _bgmClips[index];
        PlayBGM(clip, fadeIn);
    }
    
    /// <summary>
    /// BGM 재생 (내부 메서드)
    /// </summary>
    private void PlayBGM(AudioClip clip, bool fadeIn)
    {
        if (clip == null)
        {
            Debug.LogError("[SoundManager] PlayBGM: clip이 null입니다.");
            return;
        }
        
        if (_bgmSource == null)
        {
            Debug.LogError("[SoundManager] PlayBGM: _bgmSource가 null입니다.");
            return;
        }
        
        _currentBGMName = clip.name;
        Debug.Log($"[SoundManager] BGM 재생 시작: {clip.name}");
        
        if (fadeIn && _bgmFadeEnabled && _bgmSource.isPlaying)
        {
            // 기존 BGM 페이드 아웃 후 새 BGM 재생
            StartCoroutine(CoFadeOutAndPlayBGM(clip));
        }
        else
        {
            _bgmSource.clip = clip;
            _bgmSource.volume = _bgmVolume * _masterVolume;
            _bgmSource.Play();
            Debug.Log($"[SoundManager] BGM 재생: {clip.name}, Volume: {_bgmSource.volume}, isPlaying: {_bgmSource.isPlaying}");
        }
    }
    
    /// <summary>
    /// BGM 정지
    /// </summary>
    /// <param name="fadeOut">페이드 아웃 사용 여부</param>
    public void StopBGM(bool fadeOut = true)
    {
        if (!_bgmSource.isPlaying)
            return;
        
        if (fadeOut && _bgmFadeEnabled)
        {
            StartCoroutine(CoFadeOutBGM());
        }
        else
        {
            _bgmSource.Stop();
            _currentBGMName = string.Empty;
        }
    }
    
    /// <summary>
    /// BGM 일시 정지
    /// </summary>
    public void PauseBGM()
    {
        if (_bgmSource.isPlaying)
        {
            _bgmSource.Pause();
        }
    }
    
    /// <summary>
    /// BGM 재개
    /// </summary>
    public void ResumeBGM()
    {
        if (!_bgmSource.isPlaying && _bgmSource.clip != null)
        {
            _bgmSource.UnPause();
        }
    }
    
    /// <summary>
    /// BGM 페이드 아웃 후 새 BGM 재생
    /// </summary>
    private IEnumerator CoFadeOutAndPlayBGM(AudioClip newClip)
    {
        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
        }
        
        // 페이드 아웃
        yield return StartCoroutine(CoFadeOutBGM());
        
        // 새 BGM 재생
        _bgmSource.clip = newClip;
        _bgmSource.volume = 0f;
        _bgmSource.Play();
        
        // 페이드 인
        _bgmFadeCoroutine = StartCoroutine(CoFadeInBGM());
    }
    
    /// <summary>
    /// BGM 페이드 아웃
    /// </summary>
    private IEnumerator CoFadeOutBGM()
    {
        float startVolume = _bgmSource.volume;
        float elapsed = 0f;
        
        while (elapsed < _bgmFadeDuration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / _bgmFadeDuration);
            yield return null;
        }
        
        _bgmSource.Stop();
        _currentBGMName = string.Empty;
    }
    
    /// <summary>
    /// BGM 페이드 인
    /// </summary>
    private IEnumerator CoFadeInBGM()
    {
        float targetVolume = _bgmVolume * _masterVolume;
        float elapsed = 0f;
        
        while (elapsed < _bgmFadeDuration)
        {
            elapsed += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / _bgmFadeDuration);
            yield return null;
        }
        
        _bgmSource.volume = targetVolume;
        _bgmFadeCoroutine = null;
    }
    
    #endregion
    
    #region SFX
    
    /// <summary>
    /// SFX 재생 (이름으로)
    /// </summary>
    /// <param name="clipName">오디오 클립 이름</param>
    /// <param name="volume">볼륨 (0~1, 기본값: 1)</param>
    /// <param name="pitch">피치 (기본값: 1)</param>
    /// <param name="loop">루프 여부</param>
    /// <returns>재생 중인 AudioSource (정지 시 사용)</returns>
    public AudioSource PlaySFX(string clipName, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (string.IsNullOrEmpty(clipName))
            return null;
        
        // 캐싱이 완료되지 않았으면 대기 후 재생
        if (!_isCachingComplete)
        {
            StartCoroutine(CoWaitForCachingAndPlaySFX(clipName, volume, pitch, loop));
            return null;
        }
        
        AudioClip clip = GetAudioClip(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"[SoundManager] SFX 클립을 찾을 수 없습니다: {clipName}");
            return null;
        }
        
        return PlaySFX(clip, volume, pitch, loop);
    }
    
    /// <summary>
    /// 캐싱 완료를 기다린 후 SFX 재생
    /// </summary>
    private IEnumerator CoWaitForCachingAndPlaySFX(string clipName, float volume, float pitch, bool loop)
    {
        yield return new WaitUntil(() => _isCachingComplete);
        PlaySFX(clipName, volume, pitch, loop);
    }
    
    /// <summary>
    /// SFX 재생 (인덱스로)
    /// </summary>
    /// <param name="index">SFX 인덱스</param>
    /// <param name="volume">볼륨 (0~1, 기본값: 1)</param>
    /// <param name="pitch">피치 (기본값: 1)</param>
    /// <param name="loop">루프 여부</param>
    /// <returns>재생 중인 AudioSource (정지 시 사용)</returns>
    public AudioSource PlaySFX(int index, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (index < 0 || index >= _sfxClips.Count)
        {
            Debug.LogWarning($"[SoundManager] SFX 인덱스가 범위를 벗어났습니다: {index} (최대: {_sfxClips.Count - 1})");
            return null;
        }
        
        AudioClip clip = _sfxClips[index];
        return PlaySFX(clip, volume, pitch, loop);
    }
    
    /// <summary>
    /// SFX 재생 (내부 메서드)
    /// </summary>
    private AudioSource PlaySFX(AudioClip clip, float volume, float pitch, bool loop)
    {
        if (clip == null)
            return null;
        
        AudioSource source = GetSFXSource();
        if (source == null)
        {
            Debug.LogWarning("[SoundManager] 사용 가능한 SFX 오디오 소스가 없습니다.");
            return null;
        }
        
        source.clip = clip;
        source.volume = volume * _sfxVolume * _masterVolume;
        source.pitch = pitch;
        source.loop = loop;
        source.Play();
        
        _activeSfxSources.Add(source);
        
        // 루프가 아니면 재생 완료 후 풀에 반환
        if (!loop)
        {
            StartCoroutine(CoReturnSFXSourceToPool(source, clip.length / pitch));
        }
        
        return source;
    }
    
    /// <summary>
    /// SFX 정지
    /// </summary>
    /// <param name="source">정지할 AudioSource</param>
    public void StopSFX(AudioSource source)
    {
        if (source == null || !source.isPlaying)
            return;
        
        source.Stop();
        ReturnSFXSourceToPool(source);
    }
    
    /// <summary>
    /// 모든 SFX 정지
    /// </summary>
    public void StopAllSFX()
    {
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = _activeSfxSources[i];
            if (source != null && source.isPlaying)
            {
                source.Stop();
                ReturnSFXSourceToPool(source);
            }
        }
        _activeSfxSources.Clear();
    }
    
    /// <summary>
    /// SFX 오디오 소스 가져오기 (풀에서)
    /// </summary>
    private AudioSource GetSFXSource()
    {
        if (_sfxSourcePool.Count > 0)
        {
            return _sfxSourcePool.Dequeue();
        }
        
        // 풀이 비어있으면 새로 생성 (확장성)
        GameObject sfxObject = new GameObject($"SFX_Source_Extra_{_activeSfxSources.Count}");
        sfxObject.transform.SetParent(_sfxPoolParent);
        AudioSource source = sfxObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        return source;
    }
    
    /// <summary>
    /// SFX 오디오 소스를 풀에 반환
    /// </summary>
    private void ReturnSFXSourceToPool(AudioSource source)
    {
        if (source == null)
            return;
        
        source.Stop();
        source.clip = null;
        
        if (_activeSfxSources.Contains(source))
        {
            _activeSfxSources.Remove(source);
        }
        
        if (!_sfxSourcePool.Contains(source))
        {
            _sfxSourcePool.Enqueue(source);
        }
    }
    
    /// <summary>
    /// SFX 재생 완료 후 풀에 반환 (코루틴)
    /// </summary>
    private IEnumerator CoReturnSFXSourceToPool(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnSFXSourceToPool(source);
    }
    
    #endregion
    
    #region Audio Clip Management
    
    /// <summary>
    /// 오디오 클립 가져오기 (캐시에서)
    /// </summary>
    private AudioClip GetAudioClip(string clipName)
    {
        // 확장자 제거 (예: "BGM_Opening.mp3" -> "BGM_Opening")
        string nameWithoutExtension = clipName;
        if (clipName.Contains("."))
        {
            nameWithoutExtension = clipName.Substring(0, clipName.LastIndexOf('.'));
        }
        
        // 먼저 확장자 없는 이름으로 찾기
        if (_audioClipCache.TryGetValue(nameWithoutExtension, out AudioClip clip))
        {
            return clip;
        }
        
        // 원본 이름으로도 찾기
        if (_audioClipCache.TryGetValue(clipName, out clip))
        {
            return clip;
        }
        
        Debug.LogWarning($"[SoundManager] 캐시에 없는 오디오 클립: {clipName} (캐시된 클립 수: {_audioClipCache.Count})");
        Debug.LogWarning($"[SoundManager] 캐시된 클립 목록: {string.Join(", ", _audioClipCache.Keys)}");
        return null;
    }
    
    /// <summary>
    /// BGM 인덱스로 이름 가져오기
    /// </summary>
    public string GetBGMName(int index)
    {
        if (index < 0 || index >= _bgmClips.Count)
            return string.Empty;
        
        return _bgmClips[index].name;
    }
    
    /// <summary>
    /// SFX 인덱스로 이름 가져오기
    /// </summary>
    public string GetSFXName(int index)
    {
        if (index < 0 || index >= _sfxClips.Count)
            return string.Empty;
        
        return _sfxClips[index].name;
    }
    
    /// <summary>
    /// BGM 이름으로 인덱스 가져오기
    /// </summary>
    public int GetBGMIndex(string clipName)
    {
        if (_bgmNameToIndex.TryGetValue(clipName, out int index))
        {
            return index;
        }
        return -1;
    }
    
    /// <summary>
    /// SFX 이름으로 인덱스 가져오기
    /// </summary>
    public int GetSFXIndex(string clipName)
    {
        if (_sfxNameToIndex.TryGetValue(clipName, out int index))
        {
            return index;
        }
        return -1;
    }
    
    /// <summary>
    /// BGM 개수
    /// </summary>
    public int BGMCount => _bgmClips.Count;
    
    /// <summary>
    /// SFX 개수
    /// </summary>
    public int SFXCount => _sfxClips.Count;
    
    #endregion
    
    #region Volume Management
    
    /// <summary>
    /// 모든 볼륨 업데이트
    /// </summary>
    private void UpdateAllVolumes()
    {
        UpdateBGMVolume();
        UpdateSFXVolume();
    }
    
    /// <summary>
    /// BGM 볼륨 업데이트
    /// </summary>
    private void UpdateBGMVolume()
    {
        if (_bgmSource != null)
        {
            _bgmSource.volume = _bgmVolume * _masterVolume;
        }
    }
    
    /// <summary>
    /// SFX 볼륨 업데이트
    /// </summary>
    private void UpdateSFXVolume()
    {
        foreach (AudioSource source in _activeSfxSources)
        {
            if (source != null && source.isPlaying)
            {
                // 원래 볼륨 비율 유지
                float originalVolume = source.volume / (_sfxVolume * _masterVolume);
                source.volume = originalVolume * _sfxVolume * _masterVolume;
            }
        }
    }
    
    #endregion
    
    #region Public Utility Methods
    
    /// <summary>
    /// 사운드 타입별 재생 (확장성)
    /// </summary>
    public void PlaySound(ESoundType soundType, string clipName, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        switch (soundType)
        {
            case ESoundType.BGM:
                PlayBGM(clipName);
                break;
            case ESoundType.SFX:
                PlaySFX(clipName, volume, pitch, loop);
                break;
            default:
                Debug.LogWarning($"[SoundManager] 알 수 없는 사운드 타입: {soundType}");
                break;
        }
    }
    
    /// <summary>
    /// 볼륨 설정 저장 (PlayerPrefs)
    /// </summary>
    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", _bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 볼륨 설정 로드 (PlayerPrefs)
    /// </summary>
    public void LoadVolumeSettings()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            MasterVolume = PlayerPrefs.GetFloat("MasterVolume");
        }
        if (PlayerPrefs.HasKey("BGMVolume"))
        {
            BGMVolume = PlayerPrefs.GetFloat("BGMVolume");
        }
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            SFXVolume = PlayerPrefs.GetFloat("SFXVolume");
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnDestroy()
    {
        // 코루틴 정리
        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
        }
        
        // 볼륨 설정 저장
        SaveVolumeSettings();
    }
    
    #endregion
}
