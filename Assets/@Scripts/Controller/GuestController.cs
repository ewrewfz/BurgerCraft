using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;
using static Define;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class GuestController : MonoBehaviour
{
	[SerializeField, Range(1, 5)]
	private float _moveSpeed = 2;

	[SerializeField]
	private float _rotateSpeed = 360;

	private Animator _animator;
	private NavMeshAgent _navMeshAgent;
	private UI_OrderBubble _orderBubble;
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

	private EGuestState _guestState = EGuestState.None;
	public EGuestState GuestState
	{
		get { return _guestState; }
		set 
		{ 
			_guestState = value; 
			UpdateAnimation(); 
		}
	}

	public bool IsServing => Tray.Visible;

	public int CurrentDestQueueIndex;

	public Vector3 Destination
	{
		get { return _navMeshAgent.destination; }
		set 
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

	private void Awake()
	{
		_animator = GetComponent<Animator>();
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

	void Update()
	{
		if (HasArrivedAtDestination)
		{
			_navMeshAgent.isStopped = true;
			State = EState.Idle;
		}
		else
		{
			State = EState.Move;
			LookAtDestination();
		}
		

		// 중력 작용.
		transform.position = new Vector3(transform.position.x, 0, transform.position.z);
	}

	void LookAtDestination()
	{
		Vector3 moveDir = (Destination - transform.position).normalized;
		if (moveDir != Vector3.zero)
		{
			// 고개 돌리기.
			Quaternion lookRotation = Quaternion.LookRotation(moveDir);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * _rotateSpeed);
		}
	}

	int _lastAnim = -1;

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

		switch (GuestState)
		{
			case EGuestState.Eating:
				nextAnim = Define.EATING;
				break;
		}

		if (_lastAnim == nextAnim)
			return;

		_animator.CrossFade(nextAnim, 0.1f);
		_lastAnim = nextAnim;
	}
}
