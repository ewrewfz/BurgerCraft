using DG.Tweening;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
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
				Debug.Log($"[Counter] BurgerPickupPos 웨이포인트 초기화 완료. 웨이포인트 개수: {_pickupQueuePoints.Count}");
			}
			else
			{
				// Waypoints 컴포넌트가 없으면 에러 로그
				Debug.LogError($"[Counter] BurgerPickupPos에 Waypoints 컴포넌트가 없습니다! BurgerPickupPos={BurgerPickupPos.name}");
			}
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
	}
    public GuestController GetFirstGuest()
    {
        if (_queueGuests.Count > 0)
        {
            return _queueGuests[0];
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

	#region GuestAI
	public void UpdateGuestQueueAI()
	{
		// 줄서기 관리.
		for (int i = 0; i < _queueGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _queueGuests[guestIndex];
			if (guest.HasArrivedAtDestination == false)
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
	}
	#endregion
	
	#region PickupQueueAI
	/// <summary>
	/// 버거 픽업 큐 관리 (주문 완료 후 버거를 받으러 오는 손님들)
	/// </summary>
	private void UpdatePickupQueueAI()
	{
		// 줄서기 관리 및 버거 가져가기 처리
		for (int i = 0; i < _pickupQueueGuests.Count; i++)
		{
			int guestIndex = i;
			GuestController guest = _pickupQueueGuests[guestIndex];
			if (guest.HasArrivedAtDestination == false)
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
			
			// 맨 앞 손님(인덱스 0)이 도착했고, 버거를 가져갈 수 있는 상태인지 확인
			if (guestIndex == 0 && guest.CurrentDestQueueIndex == 0 && guest.HasArrivedAtDestination)
			{
				TryGiveBurgerToGuest(guest);
			}
		}
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
			_burgerPile.PileToTray(guest.Tray);
			_guestReceivedBurgers[guest] = receivedCount + 1;
			
			// 모든 버거를 받았으면 테이블로 보내기
			if (_guestReceivedBurgers[guest] >= orderCount)
			{
				SendGuestToTable(guest);
			}
		}
	}
	#endregion

	#region Interaction
	private void OnBurgerTriggerStart(WorkerController wc)
	{
		// 플레이어만 팝업 오픈
		if (wc == null || wc.GetComponent<PlayerController>() == null)
			return;

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

	private void OnOrderComplete(Define.BurgerRecipe recipe)
	{
		// Grill에 주문 전달
		Grill grill = FindObjectOfType<Grill>();
		if (grill != null)
		{
			grill.AddOrder(recipe);
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
	}
	
	/// <summary>
	/// 손님을 주문 큐에서 버거 픽업 큐로 이동시킵니다.
	/// </summary>
	private void MoveGuestToPickupQueue(GuestController guest)
	{
		if (guest == null || !_queueGuests.Contains(guest))
			return;
		
		// 주문 개수 저장 (리셋 전에)
		int orderCount = _nextOrderBurgerCount > 0 ? _nextOrderBurgerCount : (_guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 1);
		
		// 주문 큐에서 제거
		_queueGuests.Remove(guest);
		
		// 버거 픽업 큐에 추가
		if (_pickupQueuePoints.Count > 0)
		{
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
			_pickupQueueGuests.Add(guest);
			
			// 손님별 주문 개수 확인 (안전장치)
			if (!_guestOrderCounts.ContainsKey(guest))
			{
				_guestOrderCounts[guest] = orderCount;
				_guestReceivedBurgers[guest] = 0;
			}
			
			Debug.Log($"[Counter] MoveGuestToPickupQueue: 손님을 픽업 큐로 이동. dest={dest.position}, queueIndex={guest.CurrentDestQueueIndex}, queueCount={_pickupQueuePoints.Count}");
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
						_burgerPile.PileToTray(guest.Tray);
						_guestReceivedBurgers[guest] = receivedCount + 1;
						
						// 모든 버거를 받았으면 테이블로 보내기
						if (_guestReceivedBurgers[guest] >= orderCount)
						{
							SendGuestToTable(guest);
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
		// 주문 받는 로직은 OnBurgerTriggerStart에서 처리
		// 여기서는 더 이상 테이블로 보내지 않음 (버거를 받은 후에만 테이블로 감)
	}

	/// <summary>
	/// 주문 완료 시 손님을 처리합니다. (테이블로 보내거나 스폰 위치로 돌려보냄)
	/// </summary>
	public void ProcessOrderComplete(GuestController guest, bool failOrder)
	{
		if (guest == null || !_queueGuests.Contains(guest))
			return;
		
		if (failOrder)
		{
			// 실패 시 스폰 위치로 돌아가기
			guest.SetDestination(GuestSpawnPos.position, () =>
			{
				GameManager.Instance.DespawnGuest(guest.gameObject);
			});
			guest.GuestState = Define.EGuestState.Leaving;
			
			// 주문 버블 비활성화
			guest.OrderCount = 0;
			
			// 큐에서 제거
			_queueGuests.Remove(guest);
			
			// 픽업 큐에서도 제거
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
			
			// 주문 리셋
			_nextOrderBurgerCount = 0;
			_remainingOrderCount = 0;
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
