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
	
	// 손님별 픽업 큐 진입 시간 추적 (30초 이상 대기 시 실패 처리)
	private Dictionary<GuestController, float> _pickupQueueEntryTimes = new Dictionary<GuestController, float>();
	
	// 손님별 픽업 큐 맨 앞 도착 시간 추적 (맨 앞에 도착한 시점부터 타임아웃 시작)
	private Dictionary<GuestController, float> _orderStartTimes = new Dictionary<GuestController, float>();
	
	// 픽업 큐 대기 시간 제한 (초)
	private const float PICKUP_QUEUE_TIMEOUT = 30f;

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
	public Transform MoneyPilePos => _moneyPile != null ? _moneyPile.transform : null;
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
		CheckPickupQueueTimeouts(); // 픽업 큐 타임아웃 체크
	}
	
	/// <summary>
	/// 픽업 큐에서 30초 이상 대기한 손님들을 실패 처리합니다.
	/// (픽업 큐 맨 앞에 도착한 시점부터 시간 측정, 맨 앞 손님만 체크)
	/// </summary>
	private void CheckPickupQueueTimeouts()
	{
		// 맨 앞 손님(인덱스 0)만 타임아웃 체크
		if (_pickupQueueGuests.Count == 0)
			return;
		
		GuestController firstGuest = _pickupQueueGuests[0];
		if (firstGuest == null)
			return;
		
		// 모든 손님의 타임아웃 텍스트를 먼저 숨기기 (맨 앞 손님만 나중에 활성화)
		foreach (var guest in _pickupQueueGuests)
		{
			if (guest != null && guest != firstGuest)
			{
				guest.HideTimeOutText();
			}
		}
		
		// 맨 앞에 도착하지 않았으면 타임아웃 체크 안 함 (텍스트도 숨기기)
		if (firstGuest.CurrentDestQueueIndex != 0 || !firstGuest.HasArrivedAtDestination)
		{
			// 맨 앞에 도착하지 않았으면 텍스트 숨기기
			firstGuest.HideTimeOutText();
			return;
		}
		
		// 손님이 받은 버거 개수와 주문 개수 확인
		int receivedCount = _guestReceivedBurgers.ContainsKey(firstGuest) ? _guestReceivedBurgers[firstGuest] : 0;
		int orderCount = _guestOrderCounts.ContainsKey(firstGuest) ? _guestOrderCounts[firstGuest] : 0;
		
		// 모든 버거를 받았으면 타임아웃 체크 안 함 (타임아웃 텍스트 숨기기)
		if (receivedCount >= orderCount && orderCount > 0)
		{
			// 모든 버거를 받았으면 타임아웃 텍스트 숨기기
			firstGuest.HideTimeOutText();
			return; // 모든 버거를 받았으면 타임아웃 체크 안 함
		}
		
		float currentTime = Time.time;
		List<GuestController> guestsToProcess = new List<GuestController>();
		float remainingTime = 0f;
		
		// 픽업 큐 맨 앞 도착 시간을 기준으로 체크 (맨 앞에 도착한 시점부터 시간 측정)
		if (_orderStartTimes.ContainsKey(firstGuest))
		{
			float orderStartTime = _orderStartTimes[firstGuest];
			float waitTime = currentTime - orderStartTime;
			remainingTime = PICKUP_QUEUE_TIMEOUT - waitTime;
			
			// 남은 시간을 타임아웃 텍스트에 표시
			firstGuest.UpdateTimeOutText(remainingTime);
			
			// 30초 이상 대기했으면 실패 처리 대상으로 추가
			if (waitTime >= PICKUP_QUEUE_TIMEOUT)
			{
				guestsToProcess.Add(firstGuest);
			}
		}
		// 타임아웃 시작 시간이 없으면 픽업 큐 진입 시간으로 폴백 (하위 호환성)
		else if (_pickupQueueEntryTimes.ContainsKey(firstGuest))
		{
			float entryTime = _pickupQueueEntryTimes[firstGuest];
			float waitTime = currentTime - entryTime;
			remainingTime = PICKUP_QUEUE_TIMEOUT - waitTime;
			
			// 남은 시간을 타임아웃 텍스트에 표시
			firstGuest.UpdateTimeOutText(remainingTime);
			
			if (waitTime >= PICKUP_QUEUE_TIMEOUT)
			{
				guestsToProcess.Add(firstGuest);
			}
		}
		else
		{
			// 타임아웃 시작 시간이 없으면 텍스트 숨기기
			firstGuest.HideTimeOutText();
		}
		
		// 타임아웃된 손님들을 처리 (foreach 루프 밖에서 처리하여 컬렉션 수정 문제 방지)
		foreach (var guest in guestsToProcess)
		{
			if (guest == null)
				continue;
			
			float waitTime = _orderStartTimes.ContainsKey(guest) 
				? (Time.time - _orderStartTimes[guest]) 
				: (_pickupQueueEntryTimes.ContainsKey(guest) ? (Time.time - _pickupQueueEntryTimes[guest]) : 0f);
			
			Debug.LogWarning($"[Counter] 픽업 큐 타임아웃: 손님 {guest.name}이(가) 타임아웃되었습니다. 대기 시간={waitTime:F1}초, 실패 처리합니다.");
			
			// 실패한 손님의 주문과 버거 제거
			RemoveFailedGuestOrdersAndBurgers(guest);
			
			// 실패 카운트 증가 (angryEmoji도 자동으로 표시됨)
			guest.AddFailCount();
			
			// 이전 실패 처리와 동일하게 지정된 위치로 이동 후 사라지도록 처리
			// ProcessOrderComplete 내부에서 큐에서 제거하므로 별도로 제거할 필요 없음
			ProcessOrderComplete(guest, true);
		}
	}
	
	/// <summary>
	/// 실패한 손님의 주문 레시피와 만들어진 버거를 제거합니다.
	/// 모든 위치(버거파일, 트레이, 테이블 등)에서 해당 주문 번호의 버거를 찾아서 제거합니다.
	/// </summary>
	public void RemoveFailedGuestOrdersAndBurgers(GuestController guest)
	{
		if (guest == null)
			return;
		
		int guestId = guest.GetInstanceID();
		
		// 주문 번호 가져오기
		string orderNumber = null;
		if (_guestOrderNumbers.ContainsKey(guestId))
		{
			int orderNum = _guestOrderNumbers[guestId];
			orderNumber = $"주문 #{orderNum}";
		}
		
		// 주문 개수 가져오기
		int orderCount = _guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 0;
		
		// Grill에서 해당 손님의 주문 제거
		Grill grill = FindObjectOfType<Grill>();
		if (grill != null && orderCount > 0)
		{
			grill.RemoveGuestOrders(guestId, orderCount);
			Debug.Log($"[Counter] 실패한 손님의 주문 {orderCount}개를 Grill에서 제거했습니다.");
		}
		
		// 모든 위치에서 해당 주문 번호의 버거 제거 (주문 개수만큼 강제로 제거)
		int totalRemovedCount = 0;
		int targetRemovalCount = orderCount > 0 ? orderCount : 1; // 주문 개수만큼 제거
		
		// 주문 번호가 있으면 주문 번호로 제거 시도
		if (!string.IsNullOrEmpty(orderNumber))
		{
			// 주문 번호 형식 변형 시도 ("주문 #29", "주문#29", "#29", "29" 등)
			string[] orderNumberVariants = new string[]
			{
				orderNumber, // "주문 #29"
				orderNumber.Replace(" ", ""), // "주문#29"
				orderNumber.Replace("주문 ", ""), // "#29"
				orderNumber.Replace("주문 #", ""), // "29"
				$"#{orderNumber.Replace("주문 #", "")}", // "#29" (다시)
			};
			
			// 1. Grill의 BurgerPile에서 제거
			if (grill != null && grill.BurgerPile != null)
			{
				foreach (string variant in orderNumberVariants)
				{
					int removedCount = grill.BurgerPile.RemoveBurgersByOrderNumber(variant);
					totalRemovedCount += removedCount;
					if (removedCount > 0)
					{
						Debug.Log($"[Counter] 실패한 손님의 버거 {removedCount}개를 Grill의 BurgerPile에서 제거했습니다. (주문 번호: {variant})");
					}
				}
			}
			
			// 2. Counter의 BurgerPile에서 제거
			if (_burgerPile != null)
			{
				foreach (string variant in orderNumberVariants)
				{
					int removedCount = _burgerPile.RemoveBurgersByOrderNumber(variant);
					totalRemovedCount += removedCount;
					if (removedCount > 0)
					{
						Debug.Log($"[Counter] 실패한 손님의 버거 {removedCount}개를 Counter의 BurgerPile에서 제거했습니다. (주문 번호: {variant})");
					}
				}
			}
			
			// 3. 모든 알바생의 트레이에서 제거
			WorkerController[] allWorkers = FindObjectsOfType<WorkerController>();
			foreach (WorkerController worker in allWorkers)
			{
				if (worker == null || worker.Tray == null)
					continue;
				
				// 트레이에 버거가 있는지 확인
				if (worker.Tray.CurrentTrayObjectType == Define.EObjectType.Burger && worker.Tray.ItemCount > 0)
				{
					foreach (string variant in orderNumberVariants)
					{
						int removedFromTray = RemoveBurgersFromTray(worker.Tray, variant);
						totalRemovedCount += removedFromTray;
						if (removedFromTray > 0)
						{
							Debug.Log($"[Counter] 실패한 손님의 버거 {removedFromTray}개를 알바생 트레이에서 제거했습니다. (주문 번호: {variant}, Worker={worker.name})");
						}
					}
				}
			}
			
			// 4. 플레이어의 트레이에서도 제거
			PlayerController player = FindObjectOfType<PlayerController>();
			if (player != null && player.Tray != null)
			{
				if (player.Tray.CurrentTrayObjectType == Define.EObjectType.Burger && player.Tray.ItemCount > 0)
				{
					foreach (string variant in orderNumberVariants)
					{
						int removedFromTray = RemoveBurgersFromTray(player.Tray, variant);
						totalRemovedCount += removedFromTray;
						if (removedFromTray > 0)
						{
							Debug.Log($"[Counter] 실패한 손님의 버거 {removedFromTray}개를 플레이어 트레이에서 제거했습니다. (주문 번호: {variant})");
						}
					}
				}
			}
			
			// 5. 씬의 모든 버거 오브젝트에서 직접 검색하여 제거
			GameObject[] allBurgers = GameObject.FindGameObjectsWithTag("Burger");
			foreach (GameObject burgerObj in allBurgers)
			{
				if (burgerObj == null)
					continue;
				
				BurgerOrderNumber orderNumberComponent = burgerObj.GetComponent<BurgerOrderNumber>();
				if (orderNumberComponent != null)
				{
					foreach (string variant in orderNumberVariants)
					{
						if (orderNumberComponent.MatchesOrderNumber(variant))
						{
							Debug.Log($"[Counter] 씬에서 발견된 실패한 손님의 버거를 제거합니다. (주문 번호: {variant})");
							GameManager.Instance.DespawnBurger(burgerObj);
							totalRemovedCount++;
							break; // 하나 제거했으면 다음 버거로
						}
					}
				}
			}
		}
		
		// 주문 개수만큼 제거되지 않았으면 강제로 추가 제거 (주문 번호와 상관없이)
		if (totalRemovedCount < targetRemovalCount && orderCount > 0)
		{
			Debug.LogWarning($"[Counter] 주문 번호로 {totalRemovedCount}개만 제거되었습니다. 목표: {targetRemovalCount}개. 추가로 {targetRemovalCount - totalRemovedCount}개를 강제 제거합니다.");
			
			// 주문 개수만큼 제거될 때까지 모든 위치에서 버거 제거
			int remainingToRemove = targetRemovalCount - totalRemovedCount;
			
			// Grill의 BurgerPile에서 강제 제거
			if (grill != null && grill.BurgerPile != null && grill.BurgerPile.ObjectCount > 0)
			{
				for (int i = 0; i < remainingToRemove && grill.BurgerPile.ObjectCount > 0; i++)
				{
					grill.BurgerPile.DespawnObject(); // DespawnObject()는 void를 반환하므로 내부에서 이미 삭제 처리됨
					totalRemovedCount++;
					Debug.Log($"[Counter] Grill의 BurgerPile에서 강제로 버거 1개를 제거했습니다. (남은 목표: {remainingToRemove - i - 1}개)");
				}
			}
			
			// Counter의 BurgerPile에서 강제 제거
			remainingToRemove = targetRemovalCount - totalRemovedCount;
			if (_burgerPile != null && _burgerPile.ObjectCount > 0 && remainingToRemove > 0)
			{
				for (int i = 0; i < remainingToRemove && _burgerPile.ObjectCount > 0; i++)
				{
					_burgerPile.DespawnObject(); // DespawnObject()는 void를 반환하므로 내부에서 이미 삭제 처리됨
					totalRemovedCount++;
					Debug.Log($"[Counter] Counter의 BurgerPile에서 강제로 버거 1개를 제거했습니다. (남은 목표: {remainingToRemove - i - 1}개)");
				}
			}
		}
		
		if (totalRemovedCount == 0)
		{
			Debug.LogWarning($"[Counter] 실패한 손님의 버거를 찾을 수 없습니다. (주문 번호: {orderNumber}, 주문 개수: {orderCount})");
		}
		else
		{
			Debug.Log($"[Counter] 총 {totalRemovedCount}개의 실패한 버거를 제거했습니다. (주문 번호: {orderNumber}, 목표: {targetRemovalCount}개)");
		}
		
		// 주문 번호 딕셔너리에서도 제거
		if (_guestOrderNumbers.ContainsKey(guestId))
		{
			_guestOrderNumbers.Remove(guestId);
			guest.SetOrderNumberDisplay(0);
		}
	}
	
	/// <summary>
	/// 트레이에서 해당 주문 번호의 버거를 제거합니다.
	/// </summary>
	private int RemoveBurgersFromTray(TrayController tray, string orderNumber)
	{
		if (tray == null || tray.ItemCount == 0)
			return 0;
		
		int removedCount = 0;
		List<Transform> itemsToRemove = new List<Transform>();
		
		// 트레이의 모든 아이템 확인 (TrayController의 _items 리스트를 직접 접근할 수 없으므로
		// 자식 오브젝트를 확인하거나, RemoveFromTray를 반복 호출하여 확인)
		// 하지만 RemoveFromTray는 마지막 아이템만 제거하므로, 직접 접근이 필요함
		// 리플렉션을 사용하거나, 다른 방법을 사용해야 함
		
		// 일단 자식 오브젝트를 확인하는 방법 사용
		// 트레이의 아이템들은 트레이의 자식으로 추가되므로 자식 오브젝트 확인
		for (int i = 0; i < tray.transform.childCount; i++)
		{
			Transform child = tray.transform.GetChild(i);
			if (child == null)
				continue;
			
			BurgerOrderNumber orderNumberComponent = child.GetComponent<BurgerOrderNumber>();
			if (orderNumberComponent != null && orderNumberComponent.MatchesOrderNumber(orderNumber))
			{
				itemsToRemove.Add(child);
			}
		}
		
		// 찾은 버거들을 제거
		foreach (Transform burgerTransform in itemsToRemove)
		{
			if (burgerTransform != null)
			{
				GameObject burgerObj = burgerTransform.gameObject;
				// 트레이에서 제거 (TrayController의 내부 리스트에서도 제거해야 함)
				// 하지만 직접 접근이 불가능하므로, 버거를 삭제하면 트레이가 자동으로 정리될 것으로 예상
				// 일단 버거를 삭제하고, 트레이의 CurrentTrayObjectType을 확인하여 업데이트
				GameManager.Instance.DespawnBurger(burgerObj);
				removedCount++;
			}
		}
		
		// 트레이가 비어있으면 타입 초기화
		if (tray.ItemCount == 0 && removedCount > 0)
		{
			tray.CurrentTrayObjectType = Define.EObjectType.None;
		}
		
		return removedCount;
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
				// 이전에 맨 앞(인덱스 0)이었던 손님의 타임아웃 텍스트 숨기기
				if (guest.CurrentDestQueueIndex == 1 && guestIndex == 0)
				{
					guest.HideTimeOutText();
				}
				
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
				// 픽업 큐 맨 앞에 도착했을 때 타임아웃 시작 시간 기록
				if (!_orderStartTimes.ContainsKey(firstGuest))
				{
					_orderStartTimes[firstGuest] = Time.time;
				}
				
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
		
		// 픽업 큐 진입 시간 기록 (타임아웃 체크용)
		_pickupQueueEntryTimes[guest] = Time.time;
	}
	
	/// <summary>
	/// 버거 픽업 큐에서 손님 제거
	/// </summary>
	public void RemoveGuestFromPickupQueue(GuestController guest)
	{
		if (guest == null)
			return;
		
		_pickupQueueGuests.Remove(guest);
		
		// 픽업 큐 진입 시간 기록 제거
		if (_pickupQueueEntryTimes.ContainsKey(guest))
		{
			_pickupQueueEntryTimes.Remove(guest);
		}
		
		// 타임아웃 텍스트 숨기기
		guest.HideTimeOutText();
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
				
				// 버거를 받았으면 타임아웃 텍스트 숨기기
				guest.HideTimeOutText();
				
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
				if (progressbar.gameObject.activeSelf)
				{
					Debug.LogWarning($"[Counter] 알바생이 주문 중에 Counter를 떠남: Worker={wc.name}, 남은 주문={_remainingOrderCount}");
				}
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
			
			Debug.Log($"[Counter] 알바생 주문 완료: Worker={wc.name}, 레시피={randomRecipe}, 남은 주문={_remainingOrderCount}");
			
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
				Debug.Log($"[Counter] 알바생 모든 주문 완료: Worker={wc.name}");
				
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
			
			Debug.Log($"[Counter] OnOrderComplete: 주문 #{orderNumber}을 Grill에 추가, 손님={firstGuest.name}, 레시피={recipe}");
			
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
				
				Debug.Log($"[Counter] 모든 주문 완료, 손님을 픽업 큐로 이동: 손님={firstGuest.name}, 주문 개수={_nextOrderBurgerCount}");
				
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
	

	public void OnBurgerInteraction(WorkerController wc)
	{
		if (wc == null)
			return;
		
		// 플레이어가 버거를 가져가는 경우
		if (wc.GetComponent<PlayerController>() != null)
		{
			_burgerPile.TrayToPile(wc.Tray);
			return;
		}
		
		// 알바생이 버거를 옮기는 경우 (트레이에 버거가 있으면 BurgerPile로 옮기기)
		if (wc.GetComponent<PlayerController>() == null && wc.GetComponent<GuestController>() == null)
		{
			if (wc.Tray != null && wc.Tray.CurrentTrayObjectType == Define.EObjectType.Burger && wc.Tray.ItemCount > 0)
			{
				_burgerPile.TrayToPile(wc.Tray);
				return;
			}
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
		
		// 타임아웃 텍스트 숨기기
		guest.HideTimeOutText();
		
			// 딕셔너리에서도 제거
			if (_pickupQueueEntryTimes.ContainsKey(guest))
			{
				_pickupQueueEntryTimes.Remove(guest);
			}
			// 주문 시작 시간도 제거 (테이블로 가면 타임아웃 체크 불필요)
			if (_orderStartTimes.ContainsKey(guest))
			{
				_orderStartTimes.Remove(guest);
			}
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
			if (_pickupQueueEntryTimes.ContainsKey(guest))
			{
				_pickupQueueEntryTimes.Remove(guest);
			}
			// 주문 시작 시간도 제거
			if (_orderStartTimes.ContainsKey(guest))
			{
				_orderStartTimes.Remove(guest);
			}
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
