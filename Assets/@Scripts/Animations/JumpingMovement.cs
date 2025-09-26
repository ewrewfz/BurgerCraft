using DG.Tweening;
using NUnit.Framework.Internal;
using System;
using UnityEngine;

public class JumpingMovement : MonoBehaviour
{
	Transform _target;
	Action _onCompleted;

	float _horizontalSpeed = 10;
	float _verticalSpeed = 10;

	private void OnEnable()
	{
		_target = null;
	}

	private void OnDisable()
	{
		_target = null;
	}

	public void StartJump(Transform target, Action onCompleted)
	{
		_target = target;
		_onCompleted = onCompleted;

		_horizontalSpeed = 5;
		_verticalSpeed = 20;
	}

    void Update()
    {
		if (_target == null)
			return;

		if (_verticalSpeed < 0 && transform.position.y <= 0)
		{
			_onCompleted?.Invoke();
			return;
		}

		// 수평 이동.
		Vector3 dir = _target.position - transform.position;
		dir.y = 0;
		float moveDist = Time.deltaTime * _horizontalSpeed;
		transform.Translate(dir.normalized * moveDist);

		// 수직 이동.
		transform.Translate(Vector3.up * Time.deltaTime * _verticalSpeed);
		_verticalSpeed -= 200 * Time.deltaTime;
	}
}
