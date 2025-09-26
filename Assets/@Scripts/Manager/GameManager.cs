using UnityEngine;

public class GameManager : Singleton<GameManager>
{
	public Vector2 JoystickDir { get; set; } = Vector2.zero;

	#region Burger
	public GameObject BurgerPrefab;

	private Transform _burgerRoot;
	public Transform BurgerRoot
	{
		get
		{
			if (_burgerRoot == null)
			{
				GameObject go = new GameObject("@BurgerRoot");
				_burgerRoot = go.transform;
			}
			return _burgerRoot;
		}
	}

	public GameObject SpawnBurger()
	{
		GameObject go = GameObject.Instantiate(BurgerPrefab);
		go.name = BurgerPrefab.name;
		go.transform.parent = BurgerRoot;
		return go;
	}

	public void DespawnBurger(GameObject burger)
	{
		GameObject.Destroy(burger);
	}
	#endregion

	#region Money
	public GameObject MoneyPrefab;

	private Transform _moneyRoot;
	public Transform MoneyRoot
	{
		get
		{
			if (_moneyRoot == null)
			{
				GameObject go = new GameObject("@MoneyRoot");
				_moneyRoot = go.transform;
			}
			return _moneyRoot;
		}
	}

	public GameObject SpawnMoney()
	{
		GameObject go = GameObject.Instantiate(MoneyPrefab);
		go.name = MoneyPrefab.name;
		go.transform.parent = MoneyRoot;
		return go;
	}

	public void DespawnMoney(GameObject money)
	{
		GameObject.Destroy(money);
	}
	#endregion

	#region Trash
	public GameObject TrashPrefab;

	private Transform _trashRoot;
	public Transform TrashRoot
	{
		get
		{
			if (_trashRoot == null)
			{
				GameObject go = new GameObject("@TrashRoot");
				_trashRoot = go.transform;
			}
			return _trashRoot;
		}
	}

	public GameObject SpawnTrash()
	{
		GameObject go = GameObject.Instantiate(TrashPrefab);
		go.name = TrashPrefab.name;
		go.transform.parent = TrashRoot;
		return go;
	}

	public void DespawnTrash(GameObject trash)
	{
		GameObject.Destroy(trash);
	}
	#endregion

	#region Guest
	public GameObject GuestPrefab;

	private Transform _guestRoot;
	public Transform GuestRoot
	{
		get
		{
			if (_guestRoot == null)
			{
				GameObject go = new GameObject("@GuestRoot");
				_guestRoot = go.transform;
			}
			return _guestRoot;
		}
	}

	public GameObject SpawnGuest()
	{
		GameObject go = GameObject.Instantiate(GuestPrefab);
		go.name = GuestPrefab.name;
		go.transform.parent = GuestRoot;
		return go;
	}

	public void DespawnGuest(GameObject guest)
	{
		GameObject.Destroy(guest);
	}
	#endregion
}
