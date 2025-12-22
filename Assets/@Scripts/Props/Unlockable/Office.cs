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
		GameManager.Instance.UpgradeEmployeePopup.gameObject.SetActive(false);
	}
}
