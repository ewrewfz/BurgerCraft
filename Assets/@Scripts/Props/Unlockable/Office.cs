using UnityEngine;
using static Define;

[RequireComponent(typeof(WorkerInteraction))]
public class Office : UnlockableBase
{
	[SerializeField]
	private GameObject office_Wall;
    private void OnEnable()
    {
        office_Wall.gameObject.SetActive(false);
    }
    private void Start()
	{
		GetComponent<WorkerInteraction>().OnTriggerStart = OnEnterOffice;
		GetComponent<WorkerInteraction>().OnTriggerEnd = OnLeaveOffice;
        office_Wall.gameObject.SetActive(true);
    }

	public void OnEnterOffice(WorkerController wc)
	{
		if (wc.Tray.IsPlayer)
		{
			GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(true);
		}
	}

	public void OnLeaveOffice(WorkerController wc)
	{
		// 플레이어가 직접 나간 경우에만 팝업 닫기
		if (wc != null && wc.Tray != null && wc.Tray.IsPlayer)
		{
			GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(false);
		}
	}
}
