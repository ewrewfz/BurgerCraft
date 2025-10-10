using UnityEngine;
using static Define;

[RequireComponent(typeof(WorkerInteraction))]
public class Office : MonoBehaviour
{
	private void Start()
	{
		// Door에서 Office를 호출하므로 여기서는 이벤트 연결 불필요
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
