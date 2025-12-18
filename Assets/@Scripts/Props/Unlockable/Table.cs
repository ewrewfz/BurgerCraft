using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

// 1. 의자 2~4 (OK)
// 2. 책상 1개 (Collision) (OK)
// 3. 쓰레기 스폰 (OK)
// 4. 쓰레기 수거 (Trigger) (OK)
// 5. 돈 스폰 (OK)
// 6. 돈 수거 (Trigger) (OK)
public class Table : UnlockableBase
{
	public List<Transform> Chairs = new List<Transform>();

	public List<GuestController> Guests = new List<GuestController>();
	
	// 손님별 주문한 버거 개수 (쓰레기 생성 시 사용)
	private Dictionary<GuestController, int> _guestOrderCounts = new Dictionary<GuestController, int>();

	private TrashPile _trashPile;
	private MoneyPile _moneyPile; 
	private BurgerPile _burgerPile;

	public Transform WorkerPos;

	public int SpawnMoneyRemaining = 0;
	public int SpawnTrashRemaining = 0;

	ETableState _tableState = ETableState.None;
	public ETableState TableState
	{
		get { return _tableState; }
		set
		{
			_tableState = value;
		}
	}

	public bool IsOccupied
	{
		get 
		{
			if (_trashPile.ObjectCount > 0)
				return true;

			return TableState != ETableState.None;
		}
	}

	private void Start()
	{
		_trashPile = Utils.FindChild<TrashPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);

		// 쓰레기 인터랙션.
		_trashPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_trashPile.GetComponent<WorkerInteraction>().OnInteraction = OnTrashInteraction;

		// 돈 인터랙션.
		_moneyPile.GetComponent<WorkerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<WorkerInteraction>().OnInteraction = OnMoneyInteraction;
	}

	private void OnEnable()
	{
		// 쓰레기 스폰.
		StartCoroutine(CoSpawnTrash());

		// 돈 스폰.
		StartCoroutine(CoSpawnMoney());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void Update()
	{
		UpdateGuestAndTableAI();
	}

	float _eatingTimeRemaining = 0;

	private void UpdateGuestAndTableAI()
	{
		if (TableState == ETableState.Reserved)
		{
			// 손님이 모두 착석하기 기다린다.
			foreach (GuestController guest in Guests)
			{
				if (guest.HasArrivedAtDestination == false)
					return;
			}

			// 식사 시작.
			for (int i = 0; i < Guests.Count; i++)
			{
				GuestController guest = Guests[i];
				guest.GuestState = EGuestState.Eating;
				guest.transform.rotation = Chairs[i].rotation;

				// 손님이 도착한 시점에 모든 버거를 테이블로 옮기기 (코루틴으로 처리)
				if (guest.Tray != null && guest.Tray.TotalItemCount > 0)
				{
					StartCoroutine(CoMoveAllBurgersToTable(guest));
				}
			}

			_eatingTimeRemaining = Random.Range(5, 11);
			TableState = ETableState.Eating;
		}
		else if (TableState == ETableState.Eating)
		{
			_eatingTimeRemaining -= Time.deltaTime;
			if (_eatingTimeRemaining > 0)
				return;

			_eatingTimeRemaining = 0;

			// 버거 제거 및 쓰레기 생성 (각 손님이 먹은 버거 개수만큼)
			int totalTrashCount = 0;
			foreach (GuestController guest in Guests)
			{
				// 손님이 주문한 버거 개수 확인
				int orderCount = _guestOrderCounts.ContainsKey(guest) ? _guestOrderCounts[guest] : 1;
				
				// 버거 제거 (주문 개수만큼)
				for (int i = 0; i < orderCount; i++)
				{
					_burgerPile.DespawnObject();
				}
				
				// 쓰레기 개수 누적
				totalTrashCount += orderCount;
			}

			// 쓰레기 생성.
			SpawnTrashRemaining = totalTrashCount;

			// 돈 생성 (손님 수만큼)
			SpawnMoneyRemaining = Guests.Count;

			// 손님 퇴장.
			foreach (GuestController guest in Guests)
			{
				guest.GuestState = EGuestState.Leaving;
				guest.SetDestination(Define.GUEST_LEAVE_POS, () =>
				{
					GameManager.Instance.DespawnGuest(guest.gameObject);
				});
			}

			// 정리.
			Guests.Clear();
			_guestOrderCounts.Clear();
			TableState = ETableState.Dirty;
		}
		else if (TableState == ETableState.Dirty)
		{
			if (SpawnTrashRemaining == 0 && _trashPile.ObjectCount == 0)
				TableState = ETableState.None;
		}
	}

	IEnumerator CoSpawnTrash()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.TRASH_SPAWN_INTERVAL);

			if (SpawnTrashRemaining <= 0)
				continue;

			SpawnTrashRemaining--;

			_trashPile.SpawnObject();
		}
	}

	IEnumerator CoSpawnMoney()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.MONEY_SPAWN_INTERVAL);

			if (SpawnMoneyRemaining <= 0)
				continue;

			SpawnMoneyRemaining--;

			_moneyPile.SpawnObject();
		}
	}

	/// <summary>
	/// 손님의 주문 개수를 저장합니다 (쓰레기 생성 시 사용)
	/// </summary>
	public void SetGuestOrderCount(GuestController guest, int orderCount)
	{
		if (guest != null)
		{
			_guestOrderCounts[guest] = orderCount;
		}
	}
	
	/// <summary>
	/// 트레이의 모든 버거를 테이블로 옮기는 코루틴 (애니메이션 완료 대기 포함)
	/// </summary>
	private IEnumerator CoMoveAllBurgersToTable(GuestController guest)
	{
		// 모든 버거를 옮기기 (애니메이션 중인 것 포함)
		int maxIterations = 20; // 안전장치: 최대 반복 횟수
		int iterations = 0;
		
		while (guest.Tray.TotalItemCount > 0 && iterations < maxIterations)
		{
			// TrayToPile 호출 전 TotalItemCount 저장
			int beforeTotalCount = guest.Tray.TotalItemCount;
			int beforeItemCount = guest.Tray.ItemCount;
			
			_burgerPile.TrayToPile(guest.Tray);
			
			// ItemCount가 감소했으면 성공적으로 옮긴 것
			if (guest.Tray.ItemCount < beforeItemCount)
			{
				// 다음 버거를 옮기기 전에 약간 대기 (애니메이션 시간)
				yield return new WaitForSeconds(0.1f);
			}
			// TotalItemCount가 변하지 않았고 ItemCount도 변하지 않았으면 애니메이션 완료 대기
			else if (guest.Tray.TotalItemCount == beforeTotalCount && guest.Tray.ItemCount == beforeItemCount)
			{
				// ReservedCount가 있으면 애니메이션 완료 대기
				if (guest.Tray.ReservedCount > 0)
				{
					// 애니메이션 완료까지 대기 (DOJump은 0.3초)
					yield return new WaitForSeconds(0.4f);
				}
				else
				{
					// 더 이상 옮길 수 없으면 종료
					break;
				}
			}
			
			iterations++;
		}
		
		// 모든 버거를 옮긴 후 트레이 비활성화
		if (guest.Tray != null)
		{
			guest.Tray.Visible = false;
		}
	}
	
	#region Interaction
	void OnTrashInteraction(WorkerController wc)
	{
		// 버거 운반 상태에선 안 됨.
		if (wc.Tray.CurrentTrayObjectType == Define.EObjectType.Burger)
			return;

		_trashPile.PileToTray(wc.Tray);
	}

	void OnMoneyInteraction(WorkerController wc)
	{
		_moneyPile.DespawnObjectWithJump(wc.transform.position, () =>
		{
			// TODO : ADD MONEY
			GameManager.Instance.Money += 100;
		});
	}
	#endregion
}
