using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static Define;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(NavMeshAgent))]
public class StickmanController : MonoBehaviour
{
	[SerializeField, Range(1, 5)]
	protected float _moveSpeed = 3;

	[SerializeField]
	protected float _rotateSpeed = 360;

	protected Animator _animator;
	protected AudioSource _audioSource;
	protected NavMeshAgent _navMeshAgent;
	protected UI_OrderBubble _orderBubble;

	public TrayController Tray { get; protected set; }

	#region Animator
	private EAnimState _state = EAnimState.None;
	public EAnimState State
	{
		get { return _state; }
		set
		{
			if (_state == value)
				return;

			_state = value;

			UpdateAnimation();
		}
	}

	int _lastAnim = -1;

	public virtual void UpdateAnimation()
	{
		int nextAnim = -1;

		switch (State)
		{
			case EAnimState.Idle:
				nextAnim = IsServing ? Define.SERVING_IDLE : Define.IDLE;
				break;
			case EAnimState.Move:
				nextAnim = IsServing ? Define.SERVING_MOVE : Define.MOVE;
				break;
			case EAnimState.Eating:
				nextAnim = Define.EATING;
				break;
		}

		if (_lastAnim == nextAnim)
			return;

		_animator.CrossFade(nextAnim, 0.01f);
		_lastAnim = nextAnim;
	}
	#endregion

	#region NavMeshAgent
	public Vector3 Destination
	{
		get { return _navMeshAgent.destination; }
		protected set
		{
			_navMeshAgent.SetDestination(value);
			_navMeshAgent.isStopped = false;
			LookAtDestination();
		}
	}

	public bool HasArrivedAtDestination
	{
		get
		{
			Vector3 dir = Destination - transform.position;
			return dir.sqrMagnitude < 0.2f;
		}
	}

	protected void LookAtDestination()
	{
		Vector3 moveDir = (Destination - transform.position).normalized;
		if (moveDir != Vector3.zero && moveDir.sqrMagnitude > 0.01f)
		{
			Quaternion lookRotation = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);
		}
	}

	public Action OnArrivedAtDestCallback;

	public void SetDestination(Vector3 dest, Action onArrivedAtDest = null)
	{
		Destination = dest;
		OnArrivedAtDestCallback = onArrivedAtDest;
	}
	#endregion

	#region OrderBubble
	public int OrderCount
	{
		set
		{
			_orderBubble.Count = value;

			if (value > 0)
				_orderBubble.gameObject.SetActive(true);
			else
				_orderBubble.gameObject.SetActive(false);
		}
	}
	#endregion

	#region Tray
	public bool IsServing => Tray.Visible;
	#endregion

	protected virtual void Awake()
	{
		_animator = GetComponent<Animator>();
		_audioSource = GetComponent<AudioSource>();
		_navMeshAgent = GetComponent<NavMeshAgent>();
		_orderBubble = Utils.FindChild<UI_OrderBubble>(gameObject);
		Tray = Utils.FindChild<TrayController>(gameObject);

		_navMeshAgent.speed = _moveSpeed;
		_navMeshAgent.stoppingDistance = 0.05f;
		_navMeshAgent.radius = 0.1f;
		_navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

		Destination = transform.position;

		OrderCount = 0;
	}

	protected virtual void Update()
	{
		// 중력 작용.
		transform.position = new Vector3(transform.position.x, 0, transform.position.z);

		if (OnArrivedAtDestCallback != null)
		{
			if (HasArrivedAtDestination)
			{
				OnArrivedAtDestCallback?.Invoke();
				OnArrivedAtDestCallback = null;
			}
		}
	}
}
