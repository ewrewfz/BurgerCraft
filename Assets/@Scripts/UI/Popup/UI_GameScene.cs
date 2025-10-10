using TMPro;
using UnityEngine;
using static Define;

public class UI_GameScene : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI _moneyCountText;

	private void OnEnable()
	{
		RefreshUI();
		GameManager.Instance.AddEventListener(EEventType.MoneyChanged, RefreshUI);
		//GameManager.Instance.OnMoneyChanged += RefreshUI;
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.MoneyChanged, RefreshUI);
		//GameManager.Instance.OnMoneyChanged -= RefreshUI;
	}

	public void RefreshUI()
	{
		long money = GameManager.Instance.Money;
		_moneyCountText.text = Utils.GetMoneyText(money);
	}
}
