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
	UI_UpgradeEmployeePopupItem _capacityItem;

	[SerializeField]
	UI_UpgradeEmployeePopupItem _hireItem;

    void Start()
    {
		_closeButton.onClick.AddListener(OnClickCloseButton);

		_hireItem.SetInfo(EUpgradeEmployeePopupItemType.Hire, 1);
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
		}
	}

	void OnClickCloseButton()
	{
		gameObject.SetActive(false);
	}
}
