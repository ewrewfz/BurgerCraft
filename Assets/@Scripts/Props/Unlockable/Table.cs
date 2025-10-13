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

				_burgerPile.TrayToPile(guest.Tray);
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

			// 버거 제거.
			for (int i = 0; i < Guests.Count; i++)
				_burgerPile.DespawnObject();

			// 쓰레기 생성.
			SpawnTrashRemaining = Guests.Count;

			// 돈 생성
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
