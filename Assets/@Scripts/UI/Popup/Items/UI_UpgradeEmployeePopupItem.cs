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
					GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(false);
				}
				break;
		}
	}
}
