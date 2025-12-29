using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public enum EUpgradeEmployeePopupItemType
{
	None,
	Speed,
    WorkerBooster,
	Hire
}

public class UI_UpgradeEmployeePopupItem : MonoBehaviour
{
	// TODO : 나머지 UI 연동

	[SerializeField]
	private Button _purchaseButton;

	[SerializeField]
	private TextMeshProUGUI _costText;

	EUpgradeEmployeePopupItemType _type = EUpgradeEmployeePopupItemType.None;

	long _money = 0;
	
    void Start()
    {
		_purchaseButton.onClick.AddListener(OnClickPurchaseButton);
    }

	public void SetInfo(EUpgradeEmployeePopupItemType type, long money)
	{
		_type = type;
		_money = money;
		RefreshUI();
	}

	public void RefreshUI()
	{
		_costText.text = Utils.GetMoneyText(_money);
	}

	public void OnClickPurchaseButton()
	{
		if (GameManager.Instance.Money < _money)
			return;

		// 고용 버튼인 경우 알바생 수 체크
		if (_type == EUpgradeEmployeePopupItemType.Hire)
		{
			if (GameManager.Instance.Restaurant != null)
			{
				int currentWorkerCount = GameManager.Instance.Restaurant.Workers.Count;
				if (currentWorkerCount >= Define.MAX_WORKER_COUNT)
				{
					return; // 최대 알바생 수에 도달했으면 고용 불가
				}
			}
		}
		
		// Speed 버튼인 경우 알바생 수 및 최대 레벨 체크
		if (_type == EUpgradeEmployeePopupItemType.Speed)
		{
			if (GameManager.Instance.Restaurant != null)
			{
				int currentWorkerCount = GameManager.Instance.Restaurant.Workers.Count;
				if (currentWorkerCount == 0)
				{
					return; // 알바생이 없으면 구매 불가
				}
				
				int currentLevel = GameManager.Instance.Restaurant.WorkerSpeedLevel;
				if (currentLevel >= Define.MAX_WORKER_SPEED_LEVEL)
				{
					return; // 최대 레벨에 도달했으면 구매 불가
				}
			}
		}
		
		// Worker Booster 버튼인 경우 알바생 수 및 최대 레벨 체크
		if (_type == EUpgradeEmployeePopupItemType.WorkerBooster)
		{
			if (GameManager.Instance.Restaurant != null)
			{
				int currentWorkerCount = GameManager.Instance.Restaurant.Workers.Count;
				if (currentWorkerCount == 0)
				{
					return; // 알바생이 없으면 구매 불가
				}
				
				int currentLevel = GameManager.Instance.Restaurant.WorkerBoosterLevel;
				if (currentLevel >= Define.MAX_WORKER_BOOSTER_LEVEL)
				{
					return; // 최대 레벨에 도달했으면 구매 불가
				}
			}
		}

		// 돈 소모.
		GameManager.Instance.Money -= _money;
		
		// 업그레이드 버튼 클릭 시 사운드 재생
		SoundManager.Instance.PlaySFX("SFX_UpgradeButton");

		switch (_type)
		{
			case EUpgradeEmployeePopupItemType.Speed:
				{
					if (GameManager.Instance.Restaurant != null)
					{
						// Speed 레벨 증가
						GameManager.Instance.Restaurant.WorkerSpeedLevel++;
						
						// 모든 알바생의 속도 업데이트
						GameManager.Instance.Restaurant.UpdateAllWorkersSpeed();
						
						// 이벤트 브로드캐스트 (저장을 위해)
						GameManager.Instance.BroadcastEvent(EEventType.WorkerSpeedUpgraded);
						
						// UI 새로고침
						RefreshUI();
						GameManager.Instance.UpgradeEmployeePopup.RefreshUI();
					}
				}
				break;
			case EUpgradeEmployeePopupItemType.WorkerBooster:
				{
					if (GameManager.Instance.Restaurant != null)
					{
						// 부스터 레벨 증가
						GameManager.Instance.Restaurant.WorkerBoosterLevel++;
						
						// 이벤트 브로드캐스트 (저장을 위해)
						GameManager.Instance.BroadcastEvent(EEventType.WorkerBoosterUpgraded);
						
						// UI 새로고침
						RefreshUI();
						GameManager.Instance.UpgradeEmployeePopup.RefreshUI();
					}
				}
				break;
			case EUpgradeEmployeePopupItemType.Hire:
				{
					GameManager.Instance.BroadcastEvent(EEventType.HireWorker);
					// 고용 후 UI 새로고침
					GameManager.Instance.UpgradeEmployeePopup.RefreshUI();
				}
				break;
		}
	}

	public void SetInteractable(bool interactable)
	{
		if (_purchaseButton != null)
		{
			_purchaseButton.interactable = interactable;
		}
	}
}
