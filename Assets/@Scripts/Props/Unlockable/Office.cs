using UnityEngine;
using static Define;

[RequireComponent(typeof(WorkerInteraction))]
public class Office : UnlockableBase
{
	private void Start()
	{
		GetComponent<WorkerInteraction>().OnTriggerStart = OnEnterOffice;
		GetComponent<WorkerInteraction>().OnTriggerEnd = OnLeaveOffice;		
	}

	public void OnEnterOffice(WorkerController wc)
	{
		GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(true);
	}

	public void OnLeaveOffice(WorkerController wc)
	{
		GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(false);
	}
}
