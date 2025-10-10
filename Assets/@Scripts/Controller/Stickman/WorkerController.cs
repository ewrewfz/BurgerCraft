using UnityEngine;
using static Define;

// A) MainCounter
// - BurgerGrill
// - Counter : Burger
// - Counter : Cash
// - Table : Cleanup
// - TrashBin 

[RequireComponent(typeof(CharacterController))]
public class WorkerController : StickmanController
{
	protected CharacterController _controller;

	protected override void Awake()
	{
		base.Awake();

		_controller = GetComponent<CharacterController>();
	}

	private void Start()
	{
		State = Define.EAnimState.Move;
	}

	protected override void Update()
    {
		base.Update();

		if (HasArrivedAtDestination)
		{
			_navMeshAgent.isStopped = true;
			State = EAnimState.Idle;
		}
		else
		{
			State = EAnimState.Move;
			LookAtDestination();
		}
	}
}
