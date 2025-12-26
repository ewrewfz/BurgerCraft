using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_UpgradeEmployeePopup : MonoBehaviour
{
	[SerializeField]
	Button _closeButton;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _speedItem;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _workerBoosterItem;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _hireItem;

    void Start()
    {
		_closeButton.onClick.AddListener(OnClickCloseButton);

		_hireItem.SetInfo(EUpgradeEmployeePopupItemType.Hire, 1);
		_speedItem.SetInfo(EUpgradeEmployeePopupItemType.Speed, 1);
		_workerBoosterItem.SetInfo(EUpgradeEmployeePopupItemType.WorkerBooster, 1);
    }

	void OnEnable()
	{
		GameManager.Instance.AddEventListener(EEventType.HireWorker, OnHireWorker);
		RefreshUI();
	}

	void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.HireWorker, OnHireWorker);
	}

	void OnHireWorker()
	{
		RefreshUI();
	}

	public void RefreshUI()
	{
		// 알바생 수에 따라 고용 버튼 활성/비활성화
		if (GameManager.Instance.Restaurant != null)
		{
			int currentWorkerCount = GameManager.Instance.Restaurant.Workers.Count;
			bool canHire = currentWorkerCount < Define.MAX_WORKER_COUNT;
			_hireItem.SetInteractable(canHire);
			
			bool hasWorkers = currentWorkerCount > 0;
			
			// Speed: 알바생이 한 명 이상 있고, 레벨이 최대가 아닐 때만 활성화
			int currentSpeedLevel = GameManager.Instance.Restaurant.WorkerSpeedLevel;
			bool canUpgradeSpeed = hasWorkers && currentSpeedLevel < Define.MAX_WORKER_SPEED_LEVEL;
			
			// 알바생이 없으면 버튼 게임오브젝트 자체를 비활성화
			if (!hasWorkers)
			{
				_speedItem.gameObject.SetActive(false);
			}
			else
			{
				_speedItem.gameObject.SetActive(true);
				_speedItem.SetInteractable(canUpgradeSpeed);
			}
			
			// Worker Booster: 알바생이 한 명 이상 있고, 레벨이 최대가 아닐 때만 활성화
			int currentBoosterLevel = GameManager.Instance.Restaurant.WorkerBoosterLevel;
			bool canUpgradeBooster = hasWorkers && currentBoosterLevel < Define.MAX_WORKER_BOOSTER_LEVEL;
			
			// 알바생이 없으면 버튼 게임오브젝트 자체를 비활성화
			if (!hasWorkers)
			{
				_workerBoosterItem.gameObject.SetActive(false);
			}
			else
			{
				_workerBoosterItem.gameObject.SetActive(true);
				_workerBoosterItem.SetInteractable(canUpgradeBooster);
			}
		}
	}

	void OnClickCloseButton()
	{
		gameObject.SetActive(false);
	}
}
