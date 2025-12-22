using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
using static Define;

public class GuestController : StickmanController
{
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
    }
    
    /// <summary>
    /// 실패 카운트를 초기화합니다. (주문 성공 시 호출)
    /// </summary>
    public void ResetFailCount()
    {
        _failCount = 0;
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
        

        GuestState = EGuestState.Leaving;
        SetDestination(Define.GUEST_LEAVE_POS, () =>
        {
            GameManager.Instance.DespawnGuest(gameObject);
        });

    }
}
