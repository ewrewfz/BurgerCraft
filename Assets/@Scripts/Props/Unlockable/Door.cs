using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(WorkerInteraction))]
public class Door : UnlockableBase
{
	[SerializeField]
	private Transform _doorTransform;

	private Vector3 _openAngle = new Vector3(0f, 70f, 0f);

	private void Start()
	{
		GetComponent<WorkerInteraction>().OnTriggerStart = OpenDoor;
		GetComponent<WorkerInteraction>().OnTriggerEnd = CloseDoor;
	}

	public void OpenDoor(WorkerController wc)
	{
		Vector3 direction = (wc.transform.position - transform.position).normalized;
		float dot = Vector3.Dot(direction, transform.forward);

		if (dot > 0)
			_doorTransform.DOLocalRotate(_openAngle, 0.5f, RotateMode.LocalAxisAdd).OnComplete(() => {
				// 문이 완전히 열린 후 Office UI 호출
				Office office = GetComponentInParent<Office>();
				if (office != null)
					office.OnEnterOffice(wc);
			});
		else
			_doorTransform.DOLocalRotate(-_openAngle, 0.5f, RotateMode.LocalAxisAdd).OnComplete(() => {
				// 문이 완전히 열린 후 Office UI 호출
				Office office = GetComponentInParent<Office>();
				if (office != null)
					office.OnEnterOffice(wc);
			});
	}

	public void CloseDoor(WorkerController wc)
	{
		_doorTransform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutBounce);
		
		// Office UI도 함께 닫기
		Office office = GetComponentInParent<Office>();
		if (office != null)
			office.OnLeaveOffice(wc);
	}
}
