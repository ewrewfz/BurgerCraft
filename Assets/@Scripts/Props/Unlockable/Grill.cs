using System.Collections.Generic;
using UnityEngine;

// 1. 패티 에니메이션 (OK)
// 2. 햄버거 주기적으로 생성 (OK)
// 3. [Collider] 길찾기 막기 (OK)
// 4. Burger Pile (OK)
// 5. [Trigger] 햄버거 영역 안으로 들어오면, 플레이어가 갖고감.
public class Grill : UnlockableBase
{
	[SerializeField] GameObject cookingPopup;
	public GameObject CookingPopupPrefab => cookingPopup;

	private BurgerPile _burgers;
	private WorkerInteraction _interaction;

	public int BurgerCount => _burgers.ObjectCount;
	public WorkerController CurrentWorker => _interaction.CurrentWorker;
	public Transform WorkerPos;
	public GameObject MaxObject;
	public bool StopSpawnBurger = true;

	protected void Awake()
	{
		_burgers = Utils.FindChild<BurgerPile>(gameObject);

		// 햄버거 인터랙션.
		_interaction = _burgers.GetComponent<WorkerInteraction>();
		_interaction.InteractInterval = 0.2f;
		_interaction.OnInteraction = OnWorkerBurgerInteraction;
		_interaction.OnTriggerStart = OnGrillTriggerStart;
	}

	Coroutine _coSpawnBurger;

	private void OnEnable()
	{
		
		// if (_coSpawnBurger != null) StopCoroutine(_coSpawnBurger);
		// _coSpawnBurger = StartCoroutine(CoSpawnBurgers());
	}

	private void OnDisable()
	{
		if (_coSpawnBurger != null)
			StopCoroutine(_coSpawnBurger);
		_coSpawnBurger = null;
	}

	//IEnumerator CoSpawnBurgers()
	//{
	//	while(true)
	//	{
	//		// 최대치 미만이 될 때까지 대기 (여기 도달했다는 것은 미만 상태)
 //           yield return new WaitUntil(() => _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT);

	//		// 미만이면 꺼준다
	//		if (MaxObject != null && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
	//			MaxObject.SetActive(false);

	//		if (StopSpawnBurger == false)
	//		{
	//			_burgers.SpawnObject();

	//			// 스폰 후 최대치 도달 시 켠다
	//			if (MaxObject != null && _burgers.ObjectCount == Define.GRILL_MAX_BURGER_COUNT)
	//				MaxObject.SetActive(true);
	//		}

	//		yield return new WaitForSeconds(Define.GRILL_SPAWN_BURGER_INTERVAL);
	//	}
	//}

	private void OnGrillTriggerStart(WorkerController wc)
	{
		// 플레이어만 팝업 오픈
		if (wc == null || wc.GetComponent<PlayerController>() == null)
			return;

		if (cookingPopup == null)
			return;

		// PoolManager에서 팝업 가져오기 (풀에서 재사용하거나 새로 생성)
		GameObject instance = PoolManager.Instance.Pop(cookingPopup);
		UI_CookingPopup popup = instance.GetComponent<UI_CookingPopup>();
		
		if (popup != null)
		{
			popup.gameObject.SetActive(true);
			
			// Counter에서 대기 중인 주문들을 가져와서 추가
			Counter counter = FindObjectOfType<Counter>();
			if (counter != null)
			{
				List<Define.BurgerRecipe> pendingOrders = counter.GetPendingOrders();
				foreach (Define.BurgerRecipe order in pendingOrders)
				{
					popup.AddOrder(order);
				}
			}
		}
	}

	public void OnWorkerBurgerInteraction(WorkerController pc)
	{
		// 쓰레기 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObjectType == Define.EObjectType.Trash)
			return;

		_burgers.PileToTray(pc.Tray);

		// 가져가서 개수가 줄어들었으면 끈다
		if (MaxObject != null && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
			MaxObject.SetActive(false);
	}
}
