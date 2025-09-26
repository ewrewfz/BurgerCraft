using DG.Tweening;
using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

// 1. 햄버거 쌓이는 Pile (OK)
// 2. 햄버거 쌓이는 Trigger (OK)
// 3. 돈 쌓이는 Pile (OK)
// 4. 돈 먹는 Trigger (OK)
// 5. 손님 줄 (OK)
// 6. 손님 계산 받기 Trigger (손님 있어야 함. 햄버거 있어야 함. 자리 있어야 함) (OK)
public class Counter : MonoBehaviour
{
	private BurgerPile _burgerPile;
	private MoneyPile _moneyPile;
	
	int _spawnMoneyRemaining = 0;

	// 주문하는 햄버거 수.
	int _nextOrderBurgerCount = 0;	

	private List<Vector3> _queuePoints = new List<Vector3>();
	List<GuestController> _queueGuests = new List<GuestController>();

	public List<Table> Tables = new List<Table>();

	void Start()
    {
		_burgerPile = Utils.FindChild<BurgerPile>(gameObject);
		_moneyPile = Utils.FindChild<MoneyPile>(gameObject);
		_queuePoints = Utils.FindChild<Waypoints>(gameObject).GetPoints();

		// 햄버거 인터랙션.
		_burgerPile.GetComponent<PlayerInteraction>().InteractInterval = 0.1f;
		_burgerPile.GetComponent<PlayerInteraction>().OnPlayerInteraction = OnPlayerBurgerInteraction;

		// 돈 인터랙션.
		_moneyPile.GetComponent<PlayerInteraction>().InteractInterval = 0.02f;
		_moneyPile.GetComponent<PlayerInteraction>().OnPlayerInteraction = OnPlayerMoneyInteraction;

		// 손님 인터랙션.
		GameObject machine = Utils.FindChild(gameObject, "Machine");
		machine.GetComponent<PlayerInteraction>().InteractInterval = 1;
		machine.GetComponent<PlayerInteraction>().OnPlayerInteraction = OnPlayerGuestInteraction;

		// 손님 스폰.		
		StartCoroutine(CoSpawnGuest());

		// 돈 스폰.
		StartCoroutine(CoSpawnMoney());

		// TEMP
		_spawnMoneyRemaining = 30;
	}

	private void Update()
	{
		// 손님 AI.
		UpdateGuestQueueAI();
		UpdateGuestOrderAI();
	}

	IEnumerator CoSpawnGuest()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.GUEST_SPAWN_INTERVAL);

			if (_queueGuests.Count == _queuePoints.Count)
				continue;

			GameObject go = GameManager.Instance.SpawnGuest();
			GuestController guest = go.GetComponent<GuestController>();
			guest.CurrentDestQueueIndex = _queuePoints.Count - 1;
			guest.GuestState = Define.EGuestState.Queuing;
			guest.Destination = _queuePoints.Last(); 			

			_queueGuests.Add(guest);
		}
	}

	IEnumerator CoSpawnMoney()
	{
		while (true)
		{
			yield return new WaitForSeconds(Define.MONEY_SPAWN_INTERVAL);

			if (_spawnMoneyRemaining <= 0)
				continue;

			_spawnMoneyRemaining--;

			GameObject go = GameManager.Instance.SpawnMoney();
			_moneyPile.AddToPile(go, jump: true);
		}
	}

	#region GuestAI
	private void UpdateGuestQueueAI()
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

				Vector3 dest = _queuePoints[guest.CurrentDestQueueIndex];
				guest.Destination = dest;
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

		// 주문 진행.
		int orderCount = Random.Range(1, maxOrderCount + 1);
		_nextOrderBurgerCount = orderCount;
		guest.OrderCount = orderCount;
	}
	#endregion

	#region Interaction
	void OnPlayerBurgerInteraction(PlayerController pc)
	{
		Transform t = pc.Tray.RemoveFromTray();
		if (t == null)
			return;

		_burgerPile.AddToPile(t.gameObject, jump: true);
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

	void OnPlayerGuestInteraction(PlayerController pc)
	{
		// 손님 있어야 함.
		if (_nextOrderBurgerCount == 0)
			return;

		// 햄버거 있어야 함.
		if (_burgerPile.ObjectCount < _nextOrderBurgerCount)
			return;

		// 자리 수가 맞는 테이블이 있어야 함.
		Table destTable = null;
		foreach (Table table in Tables)
		{
			if (table.IsOccupied)
				continue;

			if (_nextOrderBurgerCount > table.Chairs.Count)
				continue;

			destTable = table;
			break;
		}

		if (destTable == null)
			return;

		for (int i = 0; i < _nextOrderBurgerCount; i++)
		{
			GuestController guest = _queueGuests[i];
			guest.Destination = destTable.Chairs[i].position; // 의자로 향해 이동.
			guest.GuestState = Define.EGuestState.Serving;
			guest.OrderCount = 0;

			GameObject burger = _burgerPile.RemoveFromPile();
			guest.Tray.AddToTray(burger.transform);
		}

		// 점유한다.
		destTable.Guests = _queueGuests.GetRange(0, _nextOrderBurgerCount);
		destTable.TableState = Define.ETableState.Reserved;

		// 줄에서 제거.
		_queueGuests.RemoveRange(0, _nextOrderBurgerCount);

		// 주문 처리 끝났으므로 0으로 리셋.
		_nextOrderBurgerCount = 0;
	}
	#endregion
}
