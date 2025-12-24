using System.Collections;
using System.Linq;
using UnityEngine;
//using static UnityEngine.Rendering.DebugUI;

public enum ETutorialState
{
	None,
	// CreateDoor
	CreateFirstTable,
	CreateBurgerMachine,
	CreateCounter,
	TakeOrders,
    MaketheBurger,
    PutBurgerOnCounter,
	SellBurger,
	CleanTable,
	CreateSecondTable,
	// OpenDistrictArea
	CreateOffice,

	Done,
}

public class Tutorial : MonoBehaviour
{
	[SerializeField]
	private MainCounterSystem _mainCounterSystem;

	[SerializeField]
	private GameObject _guidePrefab;

	private Guide _guideComponent;

	private RestaurantData _data;

	private ETutorialState _state
	{
		get { return _data.TutorialState; }
		set { _data.TutorialState = value; }
	}

    public void SetInfo(RestaurantData data)
	{
		_data = data;

		if (_state == ETutorialState.None)
			_state = ETutorialState.CreateFirstTable;

		// 가이드 생성
		CreateGuide();

		StartCoroutine(CoStartTutorial());
	}

	/// <summary>
	/// 가이드 오브젝트를 생성합니다.
	/// </summary>
	private void CreateGuide()
	{
		// 이미 생성되어 있으면 스킵
		if (_guideComponent != null && _guideComponent.gameObject != null)
			return;

		// 프리팹이 있으면 생성
		if (_guidePrefab != null)
		{
			GameObject guideObj = Instantiate(_guidePrefab);
			guideObj.name = "Guide";
			_guideComponent = guideObj.GetComponent<Guide>();
			if (_guideComponent == null)
			{
				_guideComponent = guideObj.AddComponent<Guide>();
			}
			guideObj.SetActive(false);
		}
		else
		{
			// 프리팹이 없으면 동적으로 생성
			GameObject guideObj = new GameObject("Guide");
			_guideComponent = guideObj.AddComponent<Guide>();
			guideObj.SetActive(false);
		}
	}

	IEnumerator CoStartTutorial()
	{
		yield return new WaitForEndOfFrame();

		Counter counter = _mainCounterSystem.Counter;
		Grill grill = _mainCounterSystem.Grill;
		Table firstTable = _mainCounterSystem.Tables[0];
		Table secondTable = _mainCounterSystem.Tables[1];
		Office office = _mainCounterSystem.Office;
		TrashCan trashCan = _mainCounterSystem.TrashCan;

		counter.SetUnlockedState(EUnlockedState.Hidden);
		grill.SetUnlockedState(EUnlockedState.Hidden);
		firstTable.SetUnlockedState(EUnlockedState.Hidden);
		secondTable.SetUnlockedState(EUnlockedState.Hidden);
		office.SetUnlockedState(EUnlockedState.Hidden);

		grill.StopSpawnBurger = true;

		if (_state == ETutorialState.CreateFirstTable)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Create First Table");

			firstTable.SetUnlockedState(EUnlockedState.ProcessingConstruction);
			
			// 가이드를 첫 번째 테이블의 UI_ConstructionArea 위치에 표시
			if (_guideComponent != null && firstTable != null && firstTable.ConstructionArea != null)
			{
				Vector3 guidePos = firstTable.ConstructionArea.transform.position;
				_guideComponent.ShowAtPosition(guidePos);
			}
			
			yield return new WaitUntil(() => firstTable.IsUnlocked);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.CreateBurgerMachine;
		}

		firstTable.SetUnlockedState(EUnlockedState.Unlocked);

		if (_state == ETutorialState.CreateBurgerMachine)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Create BurgerMachine");

			grill.SetUnlockedState(EUnlockedState.ProcessingConstruction);
			
			// 가이드를 그릴의 UI_ConstructionArea 위치에 표시
			if (_guideComponent != null && grill != null && grill.ConstructionArea != null)
			{
				Vector3 guidePos = grill.ConstructionArea.transform.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			yield return new WaitUntil(() => grill.IsUnlocked);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.CreateCounter;
		}

		grill.SetUnlockedState(EUnlockedState.Unlocked);

		if (_state == ETutorialState.CreateCounter)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Create Counter");

			counter.SetUnlockedState(EUnlockedState.ProcessingConstruction);
			
			// 가이드를 카운터의 UI_ConstructionArea 위치에 표시
			if (_guideComponent != null && counter != null && counter.ConstructionArea != null)
			{
				Vector3 guidePos = counter.ConstructionArea.transform.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			yield return new WaitUntil(() => counter.IsUnlocked);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}

            GameManager.Instance.GameSceneUI.SetToastMessage("We'll have a customer soon...");

            _state = ETutorialState.TakeOrders;
		}

		counter.SetUnlockedState(EUnlockedState.Unlocked);
		grill.StopSpawnBurger = false;

		if (_state == ETutorialState.TakeOrders)
		{
			// 첫 번째 손님이 큐에 도착할 때까지 대기
			yield return new WaitUntil(() =>
			{
				GuestController firstGuest = counter.GetFirstOrderQueueGuest();
				return firstGuest != null && 
				       firstGuest.HasArrivedAtDestination && 
				       firstGuest.CurrentDestQueueIndex == 0;
			});
			
			GameManager.Instance.GameSceneUI.SetToastMessage("Take Orders");

			// 가이드를 카운터 위치에 표시 (주문 받기)
			if (_guideComponent != null && counter != null && counter.CashierWorkerPos != null)
			{
				Vector3 guidePos = counter.CashierWorkerPos.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			yield return new WaitUntil(() => counter.CurrentCashierWorker != null);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.MaketheBurger;
		}
				
		if (_state == ETutorialState.MaketheBurger)
        {
			GameManager.Instance.GameSceneUI.SetToastMessage("Make the Burger");

			// 가이드를 그릴 위치에 표시 (버거 만들기)
			if (_guideComponent != null && grill != null && grill.WorkerPos != null)
			{
				Vector3 guidePos = grill.WorkerPos.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			yield return new WaitUntil(() => grill.CurrentWorker != null);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.SellBurger;
		}

		if (_state == ETutorialState.SellBurger)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Sell Burger");

			yield return new WaitUntil(() => firstTable.TableState == Define.ETableState.Reserved);
			_state = ETutorialState.CleanTable;
		}

		if (_state == ETutorialState.CleanTable)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("");

			// 테이블 위 쓰레기 생성 대기.
			yield return new WaitUntil(() => firstTable.TableState == Define.ETableState.Dirty);

			GameManager.Instance.GameSceneUI.SetToastMessage("Clean Table");

			// 가이드를 테이블 위치에 표시 (청소)
			if (_guideComponent != null && firstTable != null && firstTable.WorkerPos != null)
			{
				Vector3 guidePos = firstTable.WorkerPos.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			// 테이블 위 쓰레기를 줍고.
			yield return new WaitUntil(() => firstTable.TableState != Define.ETableState.Dirty);

			// 가이드를 쓰레기통 위치에 표시
			if (_guideComponent != null && trashCan != null && trashCan.WorkerPos != null)
			{
				Vector3 guidePos = trashCan.WorkerPos.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			// 쓰레기통에 버린다.
			yield return new WaitUntil(() => trashCan.CurrentWorker != null);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.CreateSecondTable;
		}

		if (_state == ETutorialState.CreateSecondTable)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Create Second Table");

			secondTable.SetUnlockedState(EUnlockedState.ProcessingConstruction);
			
			// 가이드를 두 번째 테이블의 UI_ConstructionArea 위치에 표시
			if (_guideComponent != null && secondTable != null && secondTable.ConstructionArea != null)
			{
				Vector3 guidePos = secondTable.ConstructionArea.transform.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			yield return new WaitUntil(() => secondTable.IsUnlocked);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.CreateOffice;
		}

		secondTable.SetUnlockedState(EUnlockedState.Unlocked);

		if (_state == ETutorialState.CreateOffice)
		{
			GameManager.Instance.GameSceneUI.SetToastMessage("Create Office");

			// 가이드를 오피스 위치에 표시
			if (_guideComponent != null && office != null && office.ConstructionArea != null)
			{
				Vector3 guidePos = office.ConstructionArea.transform.position;
				_guideComponent.ShowAtPosition(guidePos);
			}

			office.SetUnlockedState(EUnlockedState.ProcessingConstruction);
			yield return new WaitUntil(() => office.IsUnlocked);
			
			// 가이드 숨기기
			if (_guideComponent != null)
			{
				_guideComponent.Hide();
			}
			
			_state = ETutorialState.Done;
		}

		office.SetUnlockedState(EUnlockedState.Unlocked);

		// "Enjoy Game!" 메시지를 3초간 표시 후 페이드 아웃으로 사라지게 하기
		yield return Utils.ShowTutorialToastMessage("Enjoy Game!", 3f, 0.5f);

		yield return null;
	}
}
