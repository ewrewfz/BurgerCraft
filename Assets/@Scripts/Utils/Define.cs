using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Define
{
	public enum EState
	{
		None,
		Idle,
		Move,
	}

	public enum ETrayObject
	{
		None,
		Trash,
		Burger,
	}

	public enum EGuestState
	{
		None,
		Queuing,
		Serving,
		Eating,
		Leaving,
	}

	public enum ETableState
	{
		None,
		Reserved,
		Eating,
		Dirty,
	}

	// TEMP
	public const float GRILL_SPAWN_BURGER_INTERVAL = 0.5f;
	public const int GRILL_MAX_BURGER_COUNT = 20;

	public const float MONEY_SPAWN_INTERVAL = 0.1f;
	public const float TRASH_SPAWN_INTERVAL = 0.1f;
	public const float GUEST_SPAWN_INTERVAL = 1f;
	public const int GUEST_MAX_ORDER_BURGER_COUNT = 2;

	public static Vector3 GUEST_LEAVE_POS = new Vector3(0, 0, 0);

	public static int IDLE = Animator.StringToHash("Idle");
	public static int MOVE = Animator.StringToHash("Move");
	public static int SERVING_IDLE = Animator.StringToHash("ServingIdle");
	public static int SERVING_MOVE = Animator.StringToHash("ServingMove");
	public static int EATING = Animator.StringToHash("Eating");
}