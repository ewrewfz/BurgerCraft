using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(WorkerInteraction))]
public class Door : MonoBehaviour
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
            _doorTransform.DOLocalRotate(_openAngle, 0.5f, RotateMode.LocalAxisAdd);
        else
            _doorTransform.DOLocalRotate(-_openAngle, 0.5f, RotateMode.LocalAxisAdd);
    }

    public void CloseDoor(WorkerController wc)
    {
        _doorTransform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutBounce);
    }
}
