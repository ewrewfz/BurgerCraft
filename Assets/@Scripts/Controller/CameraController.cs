using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform _target;

    private Vector3 _offset;

    void Start()
    {
		_offset = transform.position - _target.position;
    }

    void LateUpdate()
    {
        transform.position = _offset + _target.position;
    }
}