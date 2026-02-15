using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
using TMPro;
using static Define;

public class GuestController : StickmanController
{
    [SerializeField] GameObject angryEmoji;
    [SerializeField] private float _angryEmojiDisplayDuration = 2f; // angryEmoji 표시 시간
    [SerializeField] private TextMeshProUGUI _timeOutText; // 타임아웃 텍스트
    [SerializeField] private GameObject _timeOutGameObject; // TimeOut GameObject
    
    private EGuestState _guestState = EGuestState.None;
    public EGuestState GuestState
    {
        get { return _guestState; }
        set
        {
            _guestState = value;

            if (value == EGuestState.Eating)
                State = EAnimState.Eating;

            UpdateAnimation();
        }
    }

    public int CurrentDestQueueIndex;
    
    // 실패 카운트 (3회 실패 시 떠남)
    private int _failCount = 0;
    public int FailCount => _failCount;
    
    // 인스펙터 표시용: 본인의 고유 주문 번호
    [SerializeField] private int _orderNumberDisplay = 0;
    public int OrderNumberDisplay => _orderNumberDisplay;
    
    // angryEmoji 비활성화 코루틴 참조
    private Coroutine _angryEmojiHideCoroutine;
    
    /// <summary>
    /// 주문 번호를 설정합니다 (인스펙터 표시용)
    /// </summary>
    public void SetOrderNumberDisplay(int orderNumber)
    {
        _orderNumberDisplay = orderNumber;
    }

    protected override void Awake()
    {
        base.Awake();
        if (angryEmoji != null)
        {
            angryEmoji.SetActive(false);
        }
        
        // TimeOut GameObject 초기화 (없으면 자식에서 찾기)
        if (_timeOutGameObject == null)
        {
            Transform timeOutTransform = transform.Find("TimeOut");
            if (timeOutTransform != null)
            {
                _timeOutGameObject = timeOutTransform.gameObject;
            }
        }
        
        // TimeOut 텍스트 초기화 (없으면 자식에서 찾기)
        if (_timeOutText == null && _timeOutGameObject != null)
        {
            Transform textTransform = _timeOutGameObject.transform.Find("TimeOut_Text");
            if (textTransform != null)
            {
                _timeOutText = textTransform.GetComponent<TextMeshProUGUI>();
            }
        }
        
        // TimeOut 텍스트 초기에는 비활성화
        if (_timeOutGameObject != null)
        {
            _timeOutGameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 타임아웃 텍스트를 업데이트합니다.
    /// </summary>
    public void UpdateTimeOutText(float remainingTime)
    {
        if (_timeOutText == null || _timeOutGameObject == null)
            return;
        
        // 남은 시간이 0 이하면 텍스트 숨기기
        if (remainingTime <= 0)
        {
            _timeOutGameObject.SetActive(false);
            return;
        }
        
        // 텍스트 활성화 및 업데이트
        _timeOutGameObject.SetActive(true);
        _timeOutText.text = Mathf.CeilToInt(remainingTime).ToString();
    }
    
    /// <summary>
    /// 타임아웃 텍스트를 숨깁니다.
    /// </summary>
    public void HideTimeOutText()
    {
        if (_timeOutGameObject != null)
        {
            _timeOutGameObject.SetActive(false);
        }
    }

    protected override void Update()
    {
        base.Update();

        if (GuestState != EGuestState.Eating)
        {
            if (HasArrivedAtDestination)
            {
                _navMeshAgent.isStopped = true;
                State = EAnimState.Idle;
            }
            else
            {
                State = EAnimState.Move;
                LookAtDestination();
            }
        }
        else
        {
            _navMeshAgent.isStopped = true;
        }
    }
    
    /// <summary>
    /// 실패 카운트를 증가시킵니다.
    /// </summary>
    public void AddFailCount()
    {
        _failCount++;
        
        // angryEmoji 활성화 및 일정 시간 후 비활성화
        ShowAngryEmoji();
    }
    
    /// <summary>
    /// angryEmoji를 표시하고 일정 시간 후 자동으로 숨깁니다.
    /// </summary>
    private void ShowAngryEmoji()
    {
        if (angryEmoji == null)
            return;
        
        // 이미 코루틴이 실행 중이면 중지
        if (_angryEmojiHideCoroutine != null)
        {
            StopCoroutine(_angryEmojiHideCoroutine);
        }
        
        // angryEmoji 활성화
        angryEmoji.SetActive(true);
        
        // 일정 시간 후 비활성화하는 코루틴 시작
        _angryEmojiHideCoroutine = StartCoroutine(HideAngryEmojiAfterDelay());
    }
    
    /// <summary>
    /// 일정 시간 후 angryEmoji를 비활성화하는 코루틴
    /// </summary>
    private IEnumerator HideAngryEmojiAfterDelay()
    {
        yield return new WaitForSeconds(_angryEmojiDisplayDuration);
        
        // 3회 실패로 떠나는 중이 아니면 비활성화
        if (GuestState != EGuestState.Leaving && angryEmoji != null)
        {
            angryEmoji.SetActive(false);
        }
        
        _angryEmojiHideCoroutine = null;
    }
    
    /// <summary>
    /// 실패 카운트를 초기화합니다. (주문 성공 시 호출)
    /// </summary>
    public void ResetFailCount()
    {
        _failCount = 0;
        
        // angryEmoji 비활성화 (떠나는 중이 아닐 때만)
        if (GuestState != EGuestState.Leaving)
        {
            if (_angryEmojiHideCoroutine != null)
            {
                StopCoroutine(_angryEmojiHideCoroutine);
                _angryEmojiHideCoroutine = null;
            }
            
            if (angryEmoji != null)
            {
                angryEmoji.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 3회 실패로 인해 손님이 떠나도록 합니다.
    /// </summary>
    public void LeaveDueToFailures()
    {
        if (GuestState == EGuestState.Leaving)
        {
            return;
        }
        
        OrderCount = 0;
        
        // 기존 코루틴 중지
        if (_angryEmojiHideCoroutine != null)
        {
            StopCoroutine(_angryEmojiHideCoroutine);
            _angryEmojiHideCoroutine = null;
        }
        
        // angryEmoji 활성화 (떠날 때까지 유지)
        if (angryEmoji != null)
        {
            angryEmoji.SetActive(true);
        }

        GuestState = EGuestState.Leaving;
        SetDestination(Define.GUEST_LEAVE_POS, () =>
        {
            GameManager.Instance.DespawnGuest(gameObject);
            if (angryEmoji != null)
            {
                angryEmoji.SetActive(false);
            }
        });

    }
}
