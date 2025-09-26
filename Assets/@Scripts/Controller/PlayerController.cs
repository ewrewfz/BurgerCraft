using System;
using UnityEngine;
using static Define;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
	[SerializeField, Range(1, 5)]
	private float _moveSpeed = 3;

	[SerializeField]
	private float _rotateSpeed = 360;

	private Animator _animator;
	private CharacterController _controller;
	private AudioSource _audioSource;
	public TrayController Tray { get; private set; }

	private EState _state = EState.None;
	public EState State
	{
		get { return _state; }
		private set
		{
			if (_state == value)
				return;

			_state = value;

			UpdateAnimation();
		}
	}

	public bool IsServing => Tray.Visible;

	private void Awake()
	{
		_animator = GetComponent<Animator>();
		_controller = GetComponent<CharacterController>();
		_audioSource = GetComponent<AudioSource>();

		Tray = Utils.FindChild<TrayController>(gameObject);
	}

	private void Update()
	{
		Vector3 dir = GameManager.Instance.JoystickDir;
		Vector3 moveDir = new Vector3(dir.x, 0, dir.y);
		moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;

		if (moveDir != Vector3.zero)
		{
			// 이동.
			_controller.Move(moveDir * Time.deltaTime * _moveSpeed);

			// 고개 돌리기.
			Quaternion lookRotation = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);

			State = EState.Move;
		}
		else
		{
			State = EState.Idle;
		}

		// 중력 작용.
		transform.position = new Vector3(transform.position.x, 0, transform.position.z);
	}

	int _lastAnim = - 1;

	public void UpdateAnimation()
	{
		int nextAnim = -1;

		switch (State)
		{
			case EState.Idle:
				nextAnim = IsServing ? Define.SERVING_IDLE : Define.IDLE;
				//_animator.CrossFade(IsServing ? Define.SERVING_IDLE : Define.IDLE, 0.1f);
				break;
			case EState.Move:
				nextAnim = IsServing ? Define.SERVING_MOVE : Define.MOVE;
				//_animator.CrossFade(IsServing ? Define.SERVING_MOVE : Define.MOVE, 0.05f);
				break;
		}

		if (_lastAnim == nextAnim)
			return;
		
		_animator.CrossFade(nextAnim, 0.01f);
		_lastAnim = nextAnim;
	}
}
