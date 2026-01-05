using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using DG.Tweening;

[RequireComponent(typeof(WorkerInteraction))]
public class UI_ConstructionArea : MonoBehaviour
{
    [SerializeField]
    Slider _slider;

    [SerializeField]
    TextMeshProUGUI _moneyText;

    public UnlockableBase Owner;
    public long TotalUpgradeMoney;
    public long MoneyRemaining => TotalUpgradeMoney - SpentMoney;

    public long SpentMoney
    {
        get { return Owner.SpentMoney; }
        set { Owner.SpentMoney = value; }
    }
    
    private float _lastSoundPlayTime = 0f;
    private const float SOUND_PLAY_INTERVAL = 0.1f;
    
    private Camera _mainCamera;
    private CameraController _cameraController;
    private Vector3 _originalCameraPosition;
    private Quaternion _originalCameraRotation;
    private float _originalOrthographicSize;
    private bool _isCameraMoving = false;
    private Tween _cameraTween; 

    void Start()
    {
        GetComponent<WorkerInteraction>().OnInteraction = OnWorkerInteraction;
        GetComponent<WorkerInteraction>().InteractInterval = Define.CONSTRUCTION_UPGRADE_INTERVAL;

        // TODO : 데이터 참고해서 업그레이드 비용 설정.
        TotalUpgradeMoney = 300;
        
        // 카메라 참조 가져오기
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindObjectOfType<Camera>();
        }
        
        _cameraController = _mainCamera != null ? _mainCamera.GetComponent<CameraController>() : null;
    }
    
    private void OnEnable()
    {
        // UI_ConstructionArea가 활성화될 때 카메라 이동
        if (!_isCameraMoving)
        {
            StartCoroutine(CoMoveCameraToConstructionArea());
        }
    }
    
    private void OnDisable()
    {
        // 카메라 이동 중지
        _cameraTween?.Kill();
        _isCameraMoving = false;
        
        // Orthographic Size 복원 (혹시 모를 경우를 대비)
        if (_mainCamera != null && _originalOrthographicSize > 0)
        {
            _mainCamera.orthographicSize = _originalOrthographicSize;
        }
    }
    
    /// <summary>
    /// 카메라를 ConstructionArea 위치로 이동시키고 2초 후 원래 위치로 복귀합니다.
    /// </summary>
    private IEnumerator CoMoveCameraToConstructionArea()
    {
        if (_mainCamera == null || _isCameraMoving)
            yield break;
            
        _isCameraMoving = true;
        
        // 카메라 컨트롤러 일시 비활성화
        if (_cameraController != null)
        {
            _cameraController.enabled = false;
        }
        
        // 현재 카메라 위치 및 설정 저장
        _originalCameraPosition = _mainCamera.transform.position;
        _originalCameraRotation = _mainCamera.transform.rotation;
        _originalOrthographicSize = _mainCamera.orthographicSize;
        
        // ConstructionArea의 중점 계산
        Vector3 constructionCenter = GetConstructionAreaCenter();
        
        Vector3 cameraForward = _mainCamera.transform.forward;
        Vector3 cameraRight = _mainCamera.transform.right;
        Vector3 cameraUp = _mainCamera.transform.up;
        
        float cameraDistance = 8f; // 카메라와 오브젝트 사이의 거리 (조정 가능)
        Vector3 targetPosition = constructionCenter - cameraForward * cameraDistance;
        
        float heightOffset = 4f;

        targetPosition.y = Mathf.Max(_originalCameraPosition.y + 1f, constructionCenter.y + heightOffset);
        
        // 카메라를 ConstructionArea 위치로 이동하고 줌 효과 (1초)
        _cameraTween?.Kill();
        
        // 위치와 Orthographic Size를 동시에 애니메이션
        // 줌 효과: Orthographic Size를 줄여서 오브젝트를 더 크게 보이게 함
        float zoomedOrthographicSize = _originalOrthographicSize * 0.7f; // 30% 줄여서 줌인 효과
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.Append(_mainCamera.transform.DOMove(targetPosition, 1f).SetEase(Ease.OutQuad));
        moveSequence.Join(DOTween.To(() => _mainCamera.orthographicSize, x => _mainCamera.orthographicSize = x, zoomedOrthographicSize, 1f).SetEase(Ease.OutQuad));
        _cameraTween = moveSequence;
        
        yield return _cameraTween.WaitForCompletion();
        
        // 2초 대기
        yield return new WaitForSeconds(1f);
        
        // 카메라를 원래 위치로 복귀하고 Orthographic Size 복원 (1초)
        _cameraTween?.Kill();
        
        Sequence returnSequence = DOTween.Sequence();
        returnSequence.Append(_mainCamera.transform.DOMove(_originalCameraPosition, 1f).SetEase(Ease.InQuad));
        returnSequence.Join(DOTween.To(() => _mainCamera.orthographicSize, x => _mainCamera.orthographicSize = x, _originalOrthographicSize, 1f).SetEase(Ease.InQuad));
        returnSequence.OnComplete(() =>
        {
            // 카메라 컨트롤러 다시 활성화
            if (_cameraController != null)
            {
                _cameraController.enabled = true;
            }
            _isCameraMoving = false;
        });
        _cameraTween = returnSequence;
        
        yield return _cameraTween.WaitForCompletion();
    }
    
    /// <summary>
    /// ConstructionArea의 중점 위치를 월드 좌표로 계산합니다.
    /// </summary>
    private Vector3 GetConstructionAreaCenter()
    {
        // RectTransform이 있으면 월드 좌표로 중심점 계산
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3[] worldCorners = new Vector3[4];
            rectTransform.GetWorldCorners(worldCorners);
            
            Vector3 center = Vector3.zero;
            for (int i = 0; i < 4; i++)
            {
                center += worldCorners[i];
            }
            center /= 4f;
            
            return center;
        }
        
        // RectTransform이 없으면 Owner 프랍의 월드 위치 사용
        if (Owner != null && Owner.transform != null)
        {
            return Owner.transform.position;
        }
        
        // 둘 다 없으면 현재 transform의 월드 위치 사용
        return transform.position;
    }

    public void OnWorkerInteraction(WorkerController wc)
    {
        if (Owner == null & wc.GetComponent<WorkerController>())
            return;

        

        long money = (long)(TotalUpgradeMoney / (1 / Define.CONSTRUCTION_UPGRADE_INTERVAL));
        if (money == 0)
            money = 1;

        if (GameManager.Instance.Money < money)
            return;

        // 돈이 충분할 때만 사운드 재생 (사운드 재생 간격 제한)
        float currentTime = Time.time;
        if (currentTime - _lastSoundPlayTime >= SOUND_PLAY_INTERVAL)
        {
            SoundManager.Instance.PlaySFX("SFX_Stack");
            _lastSoundPlayTime = currentTime;
        }

        GameManager.Instance.Money -= money;
        SpentMoney += money;

        if (SpentMoney >= TotalUpgradeMoney)
        {
            SpentMoney = TotalUpgradeMoney;

            // 해금 완료 사운드 재생
            SoundManager.Instance.PlaySFX("SFX_Levelup");
            // 해금 완료.
            Owner.SetUnlockedState(EUnlockedState.Unlocked);

            GameManager.Instance.BroadcastEvent(EEventType.UnlockProp);
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        _slider.value = SpentMoney / (float)TotalUpgradeMoney;
        _moneyText.text = Utils.GetMoneyText(MoneyRemaining);
    }
}
