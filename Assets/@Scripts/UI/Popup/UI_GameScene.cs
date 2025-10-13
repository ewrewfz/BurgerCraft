using TMPro;
using UnityEngine;
using static Define;

public class UI_GameScene : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI _moneyCountText;

	[SerializeField]
	TextMeshProUGUI _toastMessageText;

	private void OnEnable()
	{
		RefreshUI();
		GameManager.Instance.AddEventListener(EEventType.MoneyChanged, RefreshUI);
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.MoneyChanged, RefreshUI);
	}

	public void RefreshUI()
	{
		long money = GameManager.Instance.Money;
		_moneyCountText.text = Utils.GetMoneyText(money);
	}

	public void SetToastMessage(string message)
	{
		_toastMessageText.text = message;
		_toastMessageText.enabled = (string.IsNullOrEmpty(message) == false);
	}
}
