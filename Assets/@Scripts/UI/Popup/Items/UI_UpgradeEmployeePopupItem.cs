using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public enum EUpgradeEmployeePopupItemType
{
	None,
	Speed,
	Capacity,
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

		// 돈 소모.
		GameManager.Instance.Money -= _money;

		switch (_type)
		{
			case EUpgradeEmployeePopupItemType.Speed:
				{
					// TODO
				}
				break;
			case EUpgradeEmployeePopupItemType.Capacity:
				{
					// TODO
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
