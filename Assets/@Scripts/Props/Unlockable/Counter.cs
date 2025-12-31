using DG.Tweening;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static Define;

// 1. 햄버거 쌓이는 Pile (OK)
// 2. 햄버거 쌓이는 Trigger (OK)
// 3. 돈 쌓이는 Pile (OK)
// 4. 돈 먹는 Trigger (OK)
// 5. 손님 줄 (OK)
// 6. 손님 계산 받기 Trigger (손님 있어야 함. 햄버거 있어야 함. 자리 있어야 함) (OK)
public class Counter : UnlockableBase
{
	[SerializeField] GameObject orderPopup;

	private BurgerPile _burgerPile;
	private MoneyPile _moneyPile;

	public MainCounterSystem Owner;

	int _spawnMoneyRemaining = 0;

	// 주문하는 햄버거 수 (첫 번째 손님의 총 주문 개수)
	int _nextOrderBurgerCount = 0;
	
	// 첫 번째 손님의 남은 주문 개수
	int _remainingOrderCount = 0;

	private List<Transform> _queuePoints = new List<Transform>();
	List<GuestController> _queueGuests = new List<GuestController>();
	
	// 버거 픽업 큐 (주문 완료 후 버거를 받으러 오는 손님들)
	private List<Transform> _pickupQueuePoints = new List<Transform>();
	List<GuestController> _pickupQueueGuests = new List<GuestController>();
	
	// 테이블 대기 중인 손님들 (테이블이 없어서 대기 중)
	private List<GuestController> _waitingForTableGuests = new List<GuestController>();
	
	// 손님별 주문한 버거 개수 추적
	private Dictionary<GuestController, int> _guestOrderCounts = new Dictionary<GuestController, int>();
	
	// 손님별 받은 버거 개수 추적
	private Dictionary<GuestController, int> _guestReceivedBurgers = new Dictionary<GuestController, int>();
	
	// 손님별 주문 번호 추적 (게스트 ID → 주문 번호)
	private Dictionary<int, int> _guestOrderNumbers = new Dictionary<int, int>();
	
	// 다음 주문 번호 (순차적으로 증가)
	private int _nextOrderNumber = 1;

	public List<WorkerController> Workers = new List<WorkerController>();
	public List<Table> Tables => Owner?.Tables;

	private WorkerInteraction _burgerInteraction;
	public WorkerController CurrentBurgerWorker => _burgerInteraction.CurrentWorker;
	public Transform BurgerWorkerPos;
	public int BurgerCount => _burgerPile.ObjectCount;
	public bool NeedMoreBurgers => (_nextOrderBurgerCount > 0 && BurgerCount < _nextOrderBurgerCount);

	private WorkerInteraction _cashierInteraction;
	public WorkerController CurrentCashierWorker => _cashierInteraction.CurrentWorker;
	public Transform CashierWorkerPos;
	public bool NeedCashier => (CurrentCashierWorker == null);

	public Transform GuestSpawnPos;
	public Transform BurgerPickupPos;
	private GameObject _pickupGuestPool; // 픽업 큐 손님들을 관리하는 풀 게임오브젝트 (@PickupGuestPool)




	void Start()
    {
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_queuePoints = Utils.FindChild<Waypoints>(gameObject).GetPoints();
		
		// BurgerPickupPos도 기존 queuePoints와 동일한 방식으로 처리
		if (BurgerPickupPos != null)
		{
			// BurgerPickupPos 자체에 Waypoints 컴포넌트가 있는지 확인
			Waypoints pickupWaypoints = BurgerPickupPos.GetComponent<Waypoints>();
			if (pickupWaypoints == null)
			{
				// 없으면 자식에서 찾기
				pickupWaypoints = Utils.FindChild<Waypoints>(BurgerPickupPos.gameObject);
			}
			
			if (pickupWaypoints != null)
			{
				_pickupQueuePoints = pickupWaypoints.GetPoints();
			}
		}
		
		// PickupGuestPool 게임오브젝트 생성 (GuestPool처럼 @ 접두사 사용)
		_pickupGuestPool = GameObject.Find("@PickupGuestPool");
		if (_pickupGuestPool == null)
		{
			_pickupGuestPool = new GameObject("@PickupGuestPool");
		}

		// 햄버거 인터랙션.
		_burgerInteraction = _burgerPile.GetComponent<WorkerInteraction>();
		_burgerInteraction.InteractInterval = 0.1f;
		_burgerInteraction.OnInteraction = OnBurgerInteraction;
		
		// 돈 인터랙션.
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;

		// 손님 인터랙션 (주문 받는 장소).
		GameObject machine = Utils.FindChild(gameObject, "Machine"); 
		_cashierInteraction = machine.GetComponent<WorkerInteraction>();
		_cashierInteraction.InteractInterval = 1;
		_cashierInteraction.OnTriggerStart = OnBurgerTriggerStart;
		_cashierInteraction.OnTriggerEnd = OnBurgerTriggerEnd;
        _cashierInteraction.OnInteraction = OnGuestInteraction;
	}

	private void OnEnable()
	{
		// 손님 스폰.		
		StartCoroutine(CoSpawnGuest());
		// 돈 스폰.
		StartCoroutine(CoSpawnMoney());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void Update()
	{
		// 손님 AI.
		UpdateGuestQueueAI();
		UpdateGuestOrderAI();
		UpdatePickupQueueAI();
		UpdateWaitingForTableGuests();
	}

	IEnumerator CoSpawnGuest()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.GUEST_SPAWN_INTERVAL);

			if (_queueGuests.Count == _queuePoints.Count)
				continue;

			SpawnSingleGuest();
		}
	}
	
	/// <summary>
	/// 손님 한 명을 스폰합니다.
	/// </summary>
	private void SpawnSingleGuest()
	{
		if (_queueGuests.Count >= _queuePoints.Count)
			return;

		GameObject go = GameManager.Instance.SpawnGuest();
		go.transform.position = GuestSpawnPos.position;

		Transform dest = _queuePoints.Last();

		GuestController guest = go.GetComponent<GuestController>();
		guest.CurrentDestQueueIndex = _queuePoints.Count - 1;
		guest.GuestState = Define.EGuestState.Queuing;
		guest.SetDestination(dest.position, () => 
		{ 
			guest.transform.rotation = dest.rotation;
		}); 			

		_queueGuests.Add(guest);
	}
	
	/// <summary>
	/// GuestPool에 손님이 10명 이하면 1명씩 생성합니다.
	/// </summary>
	public void CheckAndSpawnGuestIfNeeded()
	{
		// GuestPool 게임오브젝트 찾기
		GameObject guestPool = GameObject.Find("@GuestPool");
		if (guestPool == null)
			return;
		
		// GuestPool의 자식 개수 확인 (비활성화된 손님들)
		int guestPoolCount = guestPool.transform.childCount;
		
		// 10명 이하면 1명 생성
		if (guestPoolCount < 10)
		{
			GameObject go = GameObject.Instantiate(GameManager.Instance.GuestPrefab);
			go.transform.SetParent(guestPool.transform);
			go.SetActive(false);
			go.name = GameManager.Instance.GuestPrefab.name;
		}
	}

    /// <summary>
    /// 첫 번째 손님 반환 (주문 큐 우선, 없으면 픽업 큐)
    /// </summary>
    public GuestController GetFirstGuest()
    {
        // 주문 큐에 손님이 있으면 첫 번째 손님 반환
        GuestController orderGuest = GetFirstOrderQueueGuest();
        if (orderGuest != null)
        {
            return orderGuest;
        }
        // 주문 큐가 비어있으면 픽업 큐의 첫 번째 손님 반환
        GuestController pickupGuest = GetFirstPickupQueueGuest();
        if (pickupGuest != null)
        {
            return pickupGuest;
        }
        return null;
    }
    
    /// <summary>
    /// 특정 손님의 주문 개수를 반환합니다.
    /// </summary>
    public int GetGuestOrderCount(GuestController guest)
    {
        if (guest == null)
            return 0;
        
        // 첫 번째 손님이고 _nextOrderBurgerCount가 있으면 그것을 반환
        if (_queueGuests.Count > 0 && _queueGuests[0] == guest && _nextOrderBurgerCount > 0)
        {
            return _nextOrderBurgerCount;
        }
        
        // 딕셔너리에서 확인
        if (_guestOrderCounts.ContainsKey(guest))
        {
            return _guestOrderCounts[guest];
        }
        
        return 0;
    }
    
    /// <summary>
    /// 특정 손님의 남은 주문 개수를 반환합니다.
    /// </summary>
    public int GetRemainingOrderCount(GuestController guest)
    {
        if (guest == null)
            return 0;
        
        // 첫 번째 손님이고 _remainingOrderCount가 있으면 그것을 반환
        if (_queueGuests.Count > 0 && _queueGuests[0] == guest)
        {
            return _remainingOrderCount;
        }
        
        return 0;
    }
    
    /// <summary>
    /// 주문 번호로 손님을 찾습니다. (주문 번호 문자열에서 숫자 추출)
    /// </summary>
    public GuestController GetGuestByOrderNumber(string orderNumberText)
    {
        if (string.IsNullOrEmpty(orderNumberText))
            return null;
        
        // "주문 #1" 형식에서 숫자 추출
        int orderNumber = 0;
        if (orderNumberText.StartsWith("주문 #"))
        {
            string numberStr = orderNumberText.Substring("주문 #".Length);
            if (!int.TryParse(numberStr, out orderNumber))
            {
                return null;
            }
        }
        else if (int.TryParse(orderNumberText, out orderNumber))
        {
            // 숫자만 있는 경우
        }
        else
        {
            return null;
        }
        
        // _guestOrderNumbers 딕셔너리에서 주문 번호에 해당하는 게스트 ID 찾기
        foreach (var kvp in _guestOrderNumbers)
        {
            if (kvp.Value == orderNumber)
            {
                int guestId = kvp.Key;
                
                // _queueGuests에서 찾기
                foreach (var guest in _queueGuests)
                {
                    if (guest != null && guest.GetInstanceID() == guestId)
                    {
                        return guest;
                    }
                }
                
                // _pickupQueueGuests에서 찾기
                foreach (var guest in _pickupQueueGuests)
                {
                    if (guest != null && guest.GetInstanceID() == guestId)
                    {
                        return guest;
                    }
                }
            }
        }
        
        return null;
    }


    IEnumerator CoSpawnMoney()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.MONEY_SPAWN_INTERVAL);

			if (_spawnMoneyRemaining <= 0)
				continue;

			_spawnMoneyRemaining--;

			_moneyPile.SpawnObject();
		}
	}

	#region GuestAI - Order Queue (주문 대기 큐)
	/// <summary>
	/// 주문 대기 큐 관리 (줄서기 및 이동 처리)
	/// </summary>
	public void UpdateGuestQueueAI()
	{
		UpdateOrderQueueMovement();
	}
	
	/// <summary>
	/// 주문 대기 큐의 손님들 이동 처리
	/// </summary>
	private void UpdateOrderQueueMovement()
	{
		// 줄서기 관리.
		for (int i = 0; i < _queueGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _queueGuests[guestIndex];
			if (guest == null || guest.HasArrivedAtDestination == false)
				continue;

			// 다음 지점으로 이동.
			if (guest.CurrentDestQueueIndex > guestIndex)
			{
				guest.CurrentDestQueueIndex--;

				Transform dest = _queuePoints[guest.CurrentDestQueueIndex];
				guest.SetDestination(dest.position, () =>
				{
					guest.transform.rotation = dest.rotation;
				});
			}
		}
	}
	
	/// <summary>
	/// 주문 대기 큐에 손님 추가
	/// </summary>
	public void AddGuestToOrderQueue(GuestController guest)
	{
		if (guest == null || _queueGuests.Contains(guest))
			return;
		
		_queueGuests.Add(guest);
	}
	
	/// <summary>
	/// 주문 대기 큐에서 손님 제거
	/// </summary>
	public void RemoveGuestFromOrderQueue(GuestController guest)
	{
		if (guest == null)
			return;
		
		_queueGuests.Remove(guest);
	}
	
	/// <summary>
	/// 주문 대기 큐의 첫 번째 손님 반환
	/// </summary>
	public GuestController GetFirstOrderQueueGuest()
	{
		if (_queueGuests.Count > 0)
		{
			return _queueGuests[0];
		}
		return null;
	}

	private void UpdateGuestOrderAI()
	{
		// 이미 주문이 진행중이라면 리턴.
		if (_nextOrderBurgerCount > 0)
			return;

		// 손님이 없다면 리턴.
		int maxOrderCount = Mathf.Min(Define.GUEST_MAX_ORDER_BURGER_COUNT, _queueGuests.Count);
		if (maxOrderCount == 0)
			return;

		// 이동중인지 확인.
		GuestController guest = _queueGuests[0];
		if (guest.HasArrivedAtDestination == false)
			return;

		// 맨 앞 자리 도착.
		if (guest.CurrentDestQueueIndex != 0)
			return;

		// 주문 진행 (1~최대 주문 개수)
		int orderCount = UnityEngine.Random.Range(1, maxOrderCount + 1);
		_nextOrderBurgerCount = orderCount;
		_remainingOrderCount = orderCount;
		guest.OrderCount = orderCount;
		
		// 손님별 주문 개수 저장
		_guestOrderCounts[guest] = orderCount;
		_guestReceivedBurgers[guest] = 0;
		
		// 알바생이 Counter에 있으면 즉시 주문 시작
		if (CurrentCashierWorker != null && CurrentCashierWorker.GetComponent<PlayerController>() == null)
		{
			StartWorkerAutoOrder(CurrentCashierWorker);
		}
	}
	#endregion
	
	#region PickupQueueAI - Pickup Queue (버거 픽업 큐)
	/// <summary>
	/// 버거 픽업 큐 관리 (주문 완료 후 버거를 받으러 오는 손님들)
	/// </summary>
	private void UpdatePickupQueueAI()
	{
		UpdatePickupQueueMovement();
		UpdatePickupQueueInteraction();
	}
	
	/// <summary>
	/// 버거 픽업 큐의 손님들 이동 처리
	/// </summary>
	private void UpdatePickupQueueMovement()
	{
		// 줄서기 관리
		for (int i = 0; i < _pickupQueueGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _pickupQueueGuests[guestIndex];
			if (guest == null || guest.HasArrivedAtDestination == false)
				continue;

			// 다음 지점으로 이동.
			if (guest.CurrentDestQueueIndex > guestIndex)
			{
				guest.CurrentDestQueueIndex--;

				Transform dest = _pickupQueuePoints[guest.CurrentDestQueueIndex];
				guest.SetDestination(dest.position, () =>
				{
					guest.transform.rotation = dest.rotation;
				});
			}
		}
	}
	
	/// <summary>
	/// 버거 픽업 큐의 손님들과 버거 상호작용 처리
	/// </summary>
	private void UpdatePickupQueueInteraction()
	{
		// 맨 앞 손님(인덱스 0)이 도착했고, 버거를 가져갈 수 있는 상태인지 확인
		if (_pickupQueueGuests.Count > 0)
		{
			GuestController firstGuest = _pickupQueueGuests[0];
			if (firstGuest != null && firstGuest.CurrentDestQueueIndex == 0 && firstGuest.HasArrivedAtDestination)
			{
				TryGiveBurgerToGuest(firstGuest);
			}
		}
	}
	
	/// <summary>
	/// 버거 픽업 큐에 손님 추가
	/// </summary>
	public void AddGuestToPickupQueue(GuestController guest)
	{
		if (guest == null || _pickupQueueGuests.Contains(guest))
			return;
		
		_pickupQueueGuests.Add(guest);
	}
	
	/// <summary>
	/// 버거 픽업 큐에서 손님 제거
	/// </summary>
	public void RemoveGuestFromPickupQueue(GuestController guest)
	{
		if (guest == null)
			return;
		
		_pickupQueueGuests.Remove(guest);
	}
	
	/// <summary>
	/// 버거 픽업 큐의 첫 번째 손님 반환
	/// </summary>
	public GuestController GetFirstPickupQueueGuest()
	{
		if (_pickupQueueGuests.Count > 0)
		{
			return _pickupQueueGuests[0];
		}
		return null;
	}
	
	/// <summary>
	/// 손님에게 버거를 주려고 시도합니다.
	/// </summary>
	private void TryGiveBurgerToGuest(GuestController guest)
	{
		if (guest == null || !_pickupQueueGuests.Contains(guest))
			return;
		
		// 손님이 원하는 버거 개수 확인
		if (!_guestOrderCounts.ContainsKey(guest))
			return;
		
		int orderCount = _guestOrderCounts[guest];
		int receivedCount = _guestReceivedBurgers.ContainsKey(guest) ? _guestReceivedBurgers[guest] : 0;
		
		// 아직 받지 못한 버거가 있고, BurgerPile에 버거가 있으면 가져가기
		if (receivedCount < orderCount && _burgerPile.ObjectCount > 0)
		{
			// 손님의 주문 번호 가져오기
			int guestId = guest.GetInstanceID();
			string guestOrderNumber = null;
			if (_guestOrderNumbers.ContainsKey(guestId))
			{
				guestOrderNumber = $"주문 #{_guestOrderNumbers[guestId]}";
			}
			
			// 주문 번호가 일치하는 버거만 가져가기
			bool burgerTaken = false;
			if (!string.IsNullOrEmpty(guestOrderNumber))
			{
				burgerTaken = _burgerPile.PileToTrayWithOrderNumber(guest.Tray, guestOrderNumber);
			}
			else
			{
				// 주문 번호가 없으면 기존 방식으로 폴백
				_burgerPile.PileToTray(guest.Tray);
				burgerTaken = true;
			}
			
			if (burgerTaken)
			{
				// 손님이 버거를 받을 때 사운드 재생
				SoundManager.Instance.PlaySFX("SFX_Stack_Customer");
				
				_guestReceivedBurgers[guest] = receivedCount + 1;
				
				// 모든 버거를 받았으면 테이블로 보내기
				if (_guestReceivedBurgers[guest] >= orderCount)
				{
					// 경험치 추가 (손님이 버거를 받아서 테이블로 가면 경험치 +1)
					if (GameManager.Instance != null)
					{
						GameManager.Instance.AddExperience(EXP_PER_GUEST);
					}
					
					SendGuestToTable(guest);
				}
			}
		}
	}
	#endregion

	#region Interaction
	private void OnBurgerTriggerStart(WorkerController wc)
	{
		if (wc == null)
			return;

		// 플레이어인 경우 기존 로직 실행
		if (wc.GetComponent<PlayerController>() != null)
		{
			// 알바생이 이미 작업 중이면 플레이어는 나가게 함
			if (CurrentCashierWorker != null && CurrentCashierWorker.GetComponent<PlayerController>() == null)
			{
				// 알바생이 작업 중이므로 플레이어를 나가게 함
				Vector3 exitPos = CashierWorkerPos.position - CashierWorkerPos.forward * 1.5f;
				wc.SetDestination(exitPos);
				return;
			}
			
			if (orderPopup == null)
				return;

			// 첫 번째 손님이 있고 주문이 설정되어 있는지 확인
			if (_queueGuests.Count == 0 || _nextOrderBurgerCount == 0)
				return;

			// PoolManager에서 팝업 가져오기 (풀에서 재사용하거나 새로 생성)
			GameObject instance = PoolManager.Instance.Pop(orderPopup);
			UI_OrderPopup popup = instance.GetComponent<UI_OrderPopup>();

			if (popup != null)
			{
				// 주문 완료 이벤트 구독 (Grill의 UI_CookingPopup에 영수증 추가)
				popup.OnOrderComplete += OnOrderComplete;

				// 첫 번째 손님 설정
				GuestController firstGuest = _queueGuests[0];
				popup.SetCurrentGuest(firstGuest);

				// 주문 재료 리프레쉬 (새로운 랜덤 주문)
				popup.ShowWithRandomOrder();
			}
		}
		// 알바생인 경우 진행바 표시 및 자동 주문 완료
		else
		{
			// Worker가 Counter에 있으면, 손님이 도착할 때까지 대기하거나 즉시 주문 시작
			// 손님이 있고 주문이 설정되어 있으면 즉시 시작
			if (_queueGuests.Count > 0 && _nextOrderBurgerCount > 0)
			{
				StartWorkerAutoOrder(wc);
			}
			// 손님이 없거나 주문이 설정되지 않았으면, OnGuestInteraction에서 처리하도록 대기
		}
	}
	
	/// <summary>
	/// Worker가 Counter 존에서 나갈 때 호출
	/// </summary>
	private void OnBurgerTriggerEnd(WorkerController wc)
	{
		if (wc == null)
			return;
		
		// 알바생인 경우 진행바 비활성화
		if (wc.GetComponent<PlayerController>() == null)
		{
			UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
			if (progressbar != null)
			{
				progressbar.StopProgress();
				progressbar.gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// 알바생이 자동으로 주문을 완료하는 로직
	/// </summary>
	private void StartWorkerAutoOrder(WorkerController wc)
	{
		// Worker의 진행바 찾기
		UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
		if (progressbar == null)
		{
			Debug.LogWarning("[Counter] Worker의 UI_Progressbar를 찾을 수 없습니다.");
			return;
		}
		
		// 진행바 시작
		StartProgressbarForOrder(wc, progressbar);
	}
	
	/// <summary>
	/// 진행바를 시작합니다 (재귀적으로 호출되어 모든 주문 처리)
	/// </summary>
	private void StartProgressbarForOrder(WorkerController wc, UI_Progressbar progressbar)
	{
		// 남은 주문이 없으면 종료
		if (_remainingOrderCount <= 0 || _queueGuests.Count == 0)
		{
			// 진행바 비활성화
			if (progressbar != null)
				progressbar.gameObject.SetActive(false);
			return;
		}
		
		// 진행바 활성화
		progressbar.gameObject.SetActive(true);
		
		// 진행바 완료 콜백 설정
		progressbar.OnProgressComplete = () =>
		{
			// 랜덤 주문 생성
			Define.BurgerRecipe randomRecipe = UI_OrderSystem.GenerateRandomRecipe();
			
			// 주문 완료 처리
			OnOrderComplete(randomRecipe);
			
			// 남은 주문이 있으면 다음 주문 진행
			if (_remainingOrderCount > 0 && _queueGuests.Count > 0)
			{
				// 다음 주문을 위해 다시 진행바 시작
				StartProgressbarForOrder(wc, progressbar);
			}
			else
			{
				// 모든 주문 완료 - 진행바 비활성화
				progressbar.gameObject.SetActive(false);
				
				// 알바생을 Counter에서 나가게 해서 CurrentCashierWorker 해제
				// 약간 뒤로 이동시켜서 Trigger에서 나가게 함
				Vector3 exitPos = CashierWorkerPos.position - CashierWorkerPos.forward * 1.5f;
				wc.SetDestination(exitPos);
			}
		};
		
		// 진행바 시작 (부스터 레벨에 따라 시간 조정)
		float workDuration = Define.BASE_WORKER_WORK_DURATION;
		if (GameManager.Instance != null && GameManager.Instance.Restaurant != null)
		{
			workDuration = GameManager.Instance.Restaurant.GetWorkerWorkDuration();
		}
		progressbar.StartProgress(workDuration);
	}

	private void OnOrderComplete(Define.BurgerRecipe recipe)
	{
		// Grill에 주문 전달 (손님 정보 포함)
		Grill grill = FindObjectOfType<Grill>();
		if (grill != null && _queueGuests.Count > 0)
		{
			GuestController firstGuest = _queueGuests[0];
			int guestId = firstGuest.GetInstanceID();
			
			// 게스트별 주문 번호 할당 (처음 주문하는 경우에만 새 번호 부여)
			if (!_guestOrderNumbers.ContainsKey(guestId))
			{
				_guestOrderNumbers[guestId] = _nextOrderNumber;
				_nextOrderNumber++;
			}
			
			int orderNumber = _guestOrderNumbers[guestId];
			string orderNumberText = $"주문 #{orderNumber}";
			
			grill.AddOrder(recipe, firstGuest, orderNumberText);
			
			// GuestController에 주문 번호 표시 업데이트
			firstGuest.SetOrderNumberDisplay(orderNumber);
		}
		else
		{
			Debug.LogWarning($"[Counter] OnOrderComplete: 그릴 또는 손님을 찾을 수 없음. grill={grill != null}, _queueGuests.Count={_queueGuests.Count}");
		}
		
		// 첫 번째 손님의 남은 주문 개수 감소
		if (_queueGuests.Count > 0 && _remainingOrderCount > 0)
		{
			_remainingOrderCount--;
			GuestController firstGuest = _queueGuests[0];
			
			// 남은 주문 개수 업데이트 (UI 표시용)
			firstGuest.OrderCount = _remainingOrderCount;
			
			// 모든 주문이 완료되었으면 BurgerPickupPos 큐로 이동
			if (_remainingOrderCount == 0)
			{
				// 손님별 주문 개수가 딕셔너리에 없으면 추가 (안전장치)
				if (!_guestOrderCounts.ContainsKey(firstGuest))
				{
					_guestOrderCounts[firstGuest] = _nextOrderBurgerCount;
					_guestReceivedBurgers[firstGuest] = 0;
				}
				
				MoveGuestToPickupQueue(firstGuest);
			}
		}
		else
		{
			Debug.LogWarning($"[Counter] OnOrderComplete: 조건 불만족. _queueGuests.Count={_queueGuests.Count}, _remainingOrderCount={_remainingOrderCount}");
		}
	}
	
	/// <summary>
	/// 손님을 주문 큐에서 버거 픽업 큐로 이동시킵니다.
	/// </summary>
	private void MoveGuestToPickupQueue(GuestController guest)
	{
		if (guest == null)
		{
			Debug.LogWarning($"[Counter] MoveGuestToPickupQueue: 손님이 null입니다.");
			return;
		}
		
		// 주문 큐에 있는지 확인
		if (!_queueGuests.Contains(guest))
		{
			Debug.LogWarning($"[Counter] MoveGuestToPickupQueue: 손님이 _queueGuests에 없습니다. guest={guest?.name}, _queueGuests.Count={_queueGuests.Count}");
			return;
		}
		
		// 주문 개수 저장 (리셋 전에)
		int orderCount = _nextOrderBurgerCount > 0 ? _nextOrderBurgerCount : (_guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 1);
		
		// 주문 큐에서 제거
		RemoveGuestFromOrderQueue(guest);
		
		// 버거 픽업 큐에 추가
		if (_pickupQueuePoints.Count > 0)
		{
			// 주문 완료 시 GuestPool에서 PickupGuestPool로 이동
			if (_pickupGuestPool != null)
			{
				guest.transform.SetParent(_pickupGuestPool.transform);
			}
			
			// 픽업 큐의 마지막 위치로 이동 (기존 손님들 뒤에 서기)
			Transform dest = _pickupQueuePoints.Last();
			guest.CurrentDestQueueIndex = _pickupQueuePoints.Count - 1;
			guest.GuestState = Define.EGuestState.Queuing;
			
			// 즉시 목적지로 이동하도록 설정
			guest.SetDestination(dest.position, () =>
			{
				guest.transform.rotation = dest.rotation;
			});
			
			// 픽업 큐에 추가 (목적지 설정 후)
			AddGuestToPickupQueue(guest);
			
			// 손님별 주문 개수 확인 (안전장치)
			if (!_guestOrderCounts.ContainsKey(guest))
			{
				_guestOrderCounts[guest] = orderCount;
				_guestReceivedBurgers[guest] = 0;
			}
		}
		else
		{
			Debug.LogError($"[Counter] MoveGuestToPickupQueue: 픽업 큐 포인트가 없습니다! BurgerPickupPos={BurgerPickupPos?.name}");
		}
		
		// 주문 처리 끝났으므로 리셋 (다음 손님을 위해)
		_nextOrderBurgerCount = 0;
		_remainingOrderCount = 0;
	}
	
	/// <summary>
	/// 성공 팝업이 닫힌 후 다음 주문을 받을 수 있는지 확인하고 OrderPopup을 엽니다.
	/// </summary>
	public void CheckAndOpenNextOrder(GuestController guest)
	{
		// 손님이 주문 큐에 있는지 확인 (아직 모든 주문을 완료하지 않은 경우)
		if (_queueGuests.Count > 0 && _queueGuests[0] == guest && _remainingOrderCount > 0)
		{
			// OrderPopup 다시 열기 (새로운 랜덤 주문)
			if (orderPopup != null)
			{
				GameObject instance = PoolManager.Instance.Pop(orderPopup);
				UI_OrderPopup popup = instance.GetComponent<UI_OrderPopup>();
				
				if (popup != null)
				{
					popup.OnOrderComplete += OnOrderComplete;
					popup.SetCurrentGuest(guest);
					popup.ShowWithRandomOrder();
				}
			}
		}
	}
	
	/// <summary>
	/// 대기 중인 주문들이 있는지 확인합니다 (더 이상 사용하지 않음 - Grill에서 직접 관리)
	/// </summary>
	// public bool HasPendingOrders()
	// {
	// 	return _pendingOrders.Count > 0;
	// }
	
	/// <summary>
	/// 대기 중인 주문들을 가져옵니다 (더 이상 사용하지 않음 - Grill에서 직접 관리)
	/// </summary>
	// public List<Define.BurgerRecipe> GetPendingOrders()
	// {
	// 	List<Define.BurgerRecipe> orders = new List<Define.BurgerRecipe>(_pendingOrders);
	// 	_pendingOrders.Clear();
	// 	
	// 	// 주문을 가져갔으므로 점멸 해제 이벤트 호출
	// 	if (orders.Count > 0)
	// 	{
	// 		OnPendingOrdersCleared?.Invoke();
	// 	}
	// 	
	// 	return orders;
	// }
	
	/// <summary>
	/// 주문 큐가 비워졌을 때 호출되는 이벤트 (더 이상 사용하지 않음)
	/// </summary>
	// public static Action OnPendingOrdersCleared;
	
	/// <summary>
	/// 버거를 Counter의 BurgerPile에 추가합니다. (버거는 BurgerPile에서만 관리)
	/// </summary>
	public void AddBurgerToPile()
	{
		if (_burgerPile != null)
		{
			_burgerPile.SpawnObject();
		}
	}

	void OnBurgerInteraction(WorkerController wc)
	{
		if (wc == null)
			return;
		
		// 플레이어가 버거를 가져가는 경우
		if (wc.GetComponent<PlayerController>() != null)
		{
			_burgerPile.TrayToPile(wc.Tray);
			return;
		}
		
		// 손님이 버거를 가져가는 경우
		GuestController guest = wc.GetComponent<GuestController>();
		if (guest != null && _pickupQueueGuests.Contains(guest))
		{
			// 첫 번째 손님이고 맨 앞에 도착했는지 확인
			if (_pickupQueueGuests.Count > 0 && _pickupQueueGuests[0] == guest && guest.CurrentDestQueueIndex == 0 && guest.HasArrivedAtDestination)
			{
				// 손님이 원하는 버거 개수 확인
				if (_guestOrderCounts.ContainsKey(guest))
				{
					int orderCount = _guestOrderCounts[guest];
					int receivedCount = _guestReceivedBurgers.ContainsKey(guest) ? _guestReceivedBurgers[guest] : 0;
					
					// 아직 받지 못한 버거가 있고, BurgerPile에 버거가 있으면 가져가기
					if (receivedCount < orderCount && _burgerPile.ObjectCount > 0)
					{
						// 손님의 주문 번호 가져오기
						int guestId = guest.GetInstanceID();
						string guestOrderNumber = null;
						if (_guestOrderNumbers.ContainsKey(guestId))
						{
							guestOrderNumber = $"주문 #{_guestOrderNumbers[guestId]}";
						}
						
						// 주문 번호가 일치하는 버거만 가져가기
						bool burgerTaken = false;
						if (!string.IsNullOrEmpty(guestOrderNumber))
						{
							burgerTaken = _burgerPile.PileToTrayWithOrderNumber(guest.Tray, guestOrderNumber);
						}
						else
						{
							// 주문 번호가 없으면 기존 방식으로 폴백
							_burgerPile.PileToTray(guest.Tray);
							burgerTaken = true;
						}
						
						if (burgerTaken)
						{
							// 손님이 버거를 받을 때 사운드 재생
							SoundManager.Instance.PlaySFX("SFX_Stack_Customer");
							
							_guestReceivedBurgers[guest] = receivedCount + 1;
							
							// 모든 버거를 받았으면 테이블로 보내기
							if (_guestReceivedBurgers[guest] >= orderCount)
							{
								// 경험치 추가 (손님이 버거를 받아서 테이블로 가면 경험치 +1)
								if (GameManager.Instance != null)
								{
									GameManager.Instance.AddExperience(EXP_PER_GUEST);
								}
								
								SendGuestToTable(guest);
							}
						}
					}
				}
			}
		}
		else
		{
			// 일반적인 경우 (플레이어 등)
			if (guest == null)
			{
				_burgerPile.TrayToPile(wc.Tray);
			}
		}
	}
	
	/// <summary>
	/// 모든 버거를 받은 손님을 테이블로 보냅니다.
	/// </summary>
	private void SendGuestToTable(GuestController guest)
	{
		if (guest == null || !_pickupQueueGuests.Contains(guest))
			return;
		
		Table destTable = FindTableToServeGuests();
		if (destTable == null)
		{
			// 테이블이 없으면 대기 리스트에 추가
			if (!_waitingForTableGuests.Contains(guest))
			{
				_waitingForTableGuests.Add(guest);
			}
			return;
		}
		
		// 대기 리스트에서 제거 (있는 경우)
		_waitingForTableGuests.Remove(guest);
		
		// 버거 이동은 Table.cs에서 손님이 도착한 후에 처리
		
		// 의자의 자식인 SeatPoint 위치로 이동.
		Transform seatPoint = Utils.FindChild<Transform>(destTable.Chairs[0].gameObject, "SeatPoint");
		Vector3 destination = seatPoint != null ? seatPoint.position : destTable.Chairs[0].position;
		guest.SetDestination(destination);
		
		guest.GuestState = Define.EGuestState.Serving;
		guest.OrderCount = 0;
		
		// TODO : 돈 처리. (햄버거 가격은?)
		int orderCount = _guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 1;
		_spawnMoneyRemaining += orderCount * 10;
		
		// 손님의 주문 개수를 Table에 저장 (쓰레기 생성 시 사용)
		destTable.SetGuestOrderCount(guest, orderCount);
		
		// 점유한다.
		destTable.Guests = new List<GuestController> { guest };
		destTable.TableState = Define.ETableState.Reserved;
		
		// 픽업 큐에서 제거
		_pickupQueueGuests.Remove(guest);
		
		// 딕셔너리에서도 제거
		if (_guestOrderCounts.ContainsKey(guest))
		{
			_guestOrderCounts.Remove(guest);
		}
		if (_guestReceivedBurgers.ContainsKey(guest))
		{
			_guestReceivedBurgers.Remove(guest);
		}
		
		// 주문 번호도 제거
		int guestId = guest.GetInstanceID();
		if (_guestOrderNumbers.ContainsKey(guestId))
		{
			_guestOrderNumbers.Remove(guestId);
			guest.SetOrderNumberDisplay(0); // 인스펙터 표시도 초기화
		}
	}
	
	/// <summary>
	/// 테이블 대기 중인 손님들을 처리합니다. (쓰레기가 치워지면 다시 테이블로 보냄)
	/// </summary>
	private void UpdateWaitingForTableGuests()
	{
		if (_waitingForTableGuests.Count == 0)
			return;
		
		// 대기 중인 손님들을 확인하여 테이블이 있으면 보내기
		for (int i = _waitingForTableGuests.Count - 1; i >= 0; i--)
		{
			GuestController guest = _waitingForTableGuests[i];
			if (guest == null)
			{
				_waitingForTableGuests.RemoveAt(i);
				continue;
			}
			
			Table destTable = FindTableToServeGuests();
			if (destTable != null)
			{
				// 대기 리스트에서 제거
				_waitingForTableGuests.RemoveAt(i);
				
				// 버거 이동은 Table.cs에서 손님이 도착한 후에 처리
		
		// 의자의 자식인 SeatPoint 위치로 이동.
				Transform seatPoint = Utils.FindChild<Transform>(destTable.Chairs[0].gameObject, "SeatPoint");
				Vector3 destination = seatPoint != null ? seatPoint.position : destTable.Chairs[0].position;
				guest.SetDestination(destination);
				
				guest.GuestState = Define.EGuestState.Serving;
				guest.OrderCount = 0;
				
				// TODO : 돈 처리. (햄버거 가격은?)
				int orderCount = _guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 1;
				_spawnMoneyRemaining += orderCount * 10;
				
				// 손님의 주문 개수를 Table에 저장 (쓰레기 생성 시 사용)
				destTable.SetGuestOrderCount(guest, orderCount);
				
				// 점유한다.
				destTable.Guests = new List<GuestController> { guest };
				destTable.TableState = Define.ETableState.Reserved;
				
				// 픽업 큐에서 제거 (아직 있으면)
				_pickupQueueGuests.Remove(guest);
				
				// 딕셔너리에서도 제거
				if (_guestOrderCounts.ContainsKey(guest))
				{
					_guestOrderCounts.Remove(guest);
				}
				if (_guestReceivedBurgers.ContainsKey(guest))
				{
					_guestReceivedBurgers.Remove(guest);
				}
				
				// 주문 번호도 제거
				int guestId = guest.GetInstanceID();
				if (_guestOrderNumbers.ContainsKey(guestId))
				{
					_guestOrderNumbers.Remove(guestId);
					guest.SetOrderNumberDisplay(0); // 인스펙터 표시도 초기화
				}
			}
		}
	}

	void OnMoneyInteraction(WorkerController wc)
	{
		_moneyPile.DespawnObjectWithJump(wc.transform.position, () =>
		{
			// TODO : ADD MONEY
			Utils.ApplyMoneyChange(100);
		});
	}

	void OnGuestInteraction(WorkerController wc)
	{
		// 알바생이 Counter에 있고, 손님이 있고, 주문이 설정되어 있으면 주문 시작
		if (wc != null && wc.GetComponent<PlayerController>() == null)
		{
			if (_queueGuests.Count > 0 && _nextOrderBurgerCount > 0)
			{
				// 이미 진행 중인 주문이 있는지 확인
				UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
				if (progressbar != null && progressbar.gameObject.activeSelf)
				{
					// 이미 진행 중이면 스킵
					return;
				}
				StartWorkerAutoOrder(wc);
			}
		}
	}

	/// <summary>
	/// 주문 완료 시 손님을 처리합니다. (테이블로 보내거나 스폰 위치로 돌려보냄)
	/// </summary>
	public void ProcessOrderComplete(GuestController guest, bool failOrder)
	{
		if (guest == null)
			return;
		
		// _queueGuests 또는 _pickupQueueGuests에 있는지 확인
		bool inQueueGuests = _queueGuests.Contains(guest);
		bool inPickupQueueGuests = _pickupQueueGuests.Contains(guest);
		
		if (!inQueueGuests && !inPickupQueueGuests)
			return;
		
		if (failOrder)
		{
			// 3회 실패 시 leavepos로 이동 후 삭제
			// 즉시 큐에서 제거하여 다른 손님들이 바로 이동할 수 있도록 함
			bool wasFirstGuest = inQueueGuests && _queueGuests.Count > 0 && _queueGuests[0] == guest;
			
			// 즉시 큐에서 제거 (다른 손님들이 바로 이동할 수 있도록)
			if (_queueGuests.Contains(guest))
			{
				_queueGuests.Remove(guest);
			}
			if (_pickupQueueGuests.Contains(guest))
			{
				_pickupQueueGuests.Remove(guest);
			}
			
			// 딕셔너리에서도 제거
			if (_guestOrderCounts.ContainsKey(guest))
			{
				_guestOrderCounts.Remove(guest);
			}
			if (_guestReceivedBurgers.ContainsKey(guest))
			{
				_guestReceivedBurgers.Remove(guest);
			}
			
			// 주문 번호도 제거
			int guestId = guest.GetInstanceID();
			if (_guestOrderNumbers.ContainsKey(guestId))
			{
				_guestOrderNumbers.Remove(guestId);
				guest.SetOrderNumberDisplay(0); // 인스펙터 표시도 초기화
			}
			
			// 주문 리셋
			_nextOrderBurgerCount = 0;
			_remainingOrderCount = 0;
			
			// 실패한 손님이 첫 번째였고, 다음 손님이 있으면 다음 손님의 주문 설정
			if (wasFirstGuest && _queueGuests.Count > 0)
			{
				GuestController nextGuest = _queueGuests[0];
				if (nextGuest != null && nextGuest.HasArrivedAtDestination && nextGuest.CurrentDestQueueIndex == 0)
				{
					// 다음 손님의 주문 개수 설정
					int maxOrderCount = Mathf.Min(Define.GUEST_MAX_ORDER_BURGER_COUNT, _queueGuests.Count);
					if (maxOrderCount > 0)
					{
						int orderCount = UnityEngine.Random.Range(1, maxOrderCount + 1);
						_nextOrderBurgerCount = orderCount;
						_remainingOrderCount = orderCount;
						nextGuest.OrderCount = orderCount;
						
						// 손님별 주문 개수 저장
						_guestOrderCounts[nextGuest] = orderCount;
						_guestReceivedBurgers[nextGuest] = 0;
					}
				}
			}
			
			// 실패 시 스폰 위치(leavepos)로 돌아가기
			guest.SetDestination(GuestSpawnPos.position, () =>
			{
				// leavepos 도착 후 삭제
				if (guest != null && guest.gameObject != null)
				{
					Destroy(guest.gameObject);
				}
			});
			guest.GuestState = Define.EGuestState.Leaving;
			
			// 주문 버블 비활성화
			guest.OrderCount = 0;
		}
		// 성공 시는 OnGuestInteraction에서 처리하므로 여기서는 처리하지 않음
	}

	public Table FindTableToServeGuests()
	{
		// 자리 수가 맞는 테이블이 있어야 함 (1명씩 처리)
		foreach (Table table in Tables)
		{
			if (table.IsUnlocked == false)
				continue;
			if (table.IsOccupied)
				continue;

			if (table.Chairs.Count < 1)
				continue;

			return table;
		}

		return null;
	}
	#endregion
}
