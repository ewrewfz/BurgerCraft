using DG.Tweening;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class MovingBurger : MonoBehaviour
{
    private Vector3 _originalPosition;
    private float _destZ;
    void Start()
    {
        _originalPosition = transform.position;
        _destZ = transform.position.z - 1;

        Move();
    }

    void Move()
    {
        transform.position = _originalPosition;

        transform.DOMoveZ(_destZ, 1)
                 .SetEase(Ease.Linear)
                 .OnComplete(Move);
    }
}
