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
public class Table : MonoBehaviour
{
	public List<Transform> Chairs = new List<Transform>();

	public List<GuestController> Guests = new List<GuestController>();

	private TrashPile _trashPile;
	private MoneyPile _moneyPile; 
	private BurgerPile _burgerPile;

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

	// TODO : 나중엔 코드 내부를 State 기반으로 바꾸는게 깔끔.
	public bool IsOccupied
	{
		get 
		{
			// 쓰레기 있으면 아직 사용 못 함.
			if (_trashPile.ObjectCount > 0)
				return true;

			return Guests.Count > 0; 
		}
	}

	private void Start()
	{
		_trashPile = Utils.FindChild<TrashPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);

		// 쓰레기 인터랙션.
		_trashPile.GetComponent<PlayerInteraction>().InteractInterval = 0.02f;
		_trashPile.GetComponent<PlayerInteraction>().OnPlayerInteraction = OnPlayerTrashInteraction;

		// 돈 인터랙션.
		_moneyPile.GetComponent<PlayerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<PlayerInteraction>().OnPlayerInteraction = OnPlayerMoneyInteraction;

		// 쓰레기 스폰.
		StartCoroutine(CoSpawnTrash());

		// 돈 스폰.
		StartCoroutine(CoSpawnMoney());

		//SpawnTrashRemaining = 4;
		//SpawnMoneyRemaining = 20;		
	}

	private void Update()
	{
		UpdateGuestAI();
	}

	float _eatingTimeRemaining = 0;

	private void UpdateGuestAI()
	{
		if (IsOccupied == false)
			return;

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

				Transform burger = guest.Tray.RemoveFromTray();
				_burgerPile.AddToPile(burger.gameObject, jump: true);
			}

			_eatingTimeRemaining = Random.Range(5, 11);
			TableState = ETableState.Eating;
		}
		else if (TableState == ETableState.Eating)
		{
			//Debug.Log("EATING !!");
			_eatingTimeRemaining -= Time.deltaTime;
			if (_eatingTimeRemaining < 0)
			{
				_eatingTimeRemaining = 0;

				// 버거 제거.
				for (int i = 0; i < Guests.Count; i++)
				{
					GameObject burger = _burgerPile.RemoveFromPile();
					if (burger == null)
						break;

					GameManager.Instance.DespawnBurger(burger);
				}

				// 쓰레기 생성.
				SpawnTrashRemaining = Guests.Count;

				// TODO : 돈 생성?

				// 손님 퇴장.
				foreach (GuestController guest in Guests)
				{
					guest.GuestState = EGuestState.Leaving;
					guest.Destination = Define.GUEST_LEAVE_POS;
					// TODO : 도착하면 삭제.
				}

				// 정리.
				Guests.Clear();
				TableState = ETableState.Dirty;
			}
			else if (TableState == ETableState.Dirty)
			{
				if (_trashPile.ObjectCount == 0)
					TableState = ETableState.None;
			}
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

			GameObject go = GameManager.Instance.SpawnTrash();
			_trashPile.AddToPile(go, jump: true);
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

			GameObject go = GameManager.Instance.SpawnMoney();
			_moneyPile.AddToPile(go, jump: true);
		}
	}

	#region Interaction
	void OnPlayerTrashInteraction(PlayerController pc)
	{
		// 버거 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObject == Define.ETrayObject.Burger)
			return;

		GameObject trash = _trashPile.RemoveFromPile();
		if (trash == null)
			return;

		pc.Tray.AddToTray(trash.transform);
	}

	void OnPlayerMoneyInteraction(PlayerController pc)
	{
		GameObject money = _moneyPile.RemoveFromPile();
		if (money == null)
			return;

		JumpingMovement jump = money.GetOrAddComponent<JumpingMovement>();
		jump.StartJump(pc.transform, () =>
		{
			GameManager.Instance.DespawnMoney(money);
		});
	}
	#endregion
}
