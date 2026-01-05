using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class Restaurant : MonoBehaviour
{
	public List<SystemBase> RestaurantSystems = new List<SystemBase>();

	public int StageNum = 0;
	public List<UnlockableBase> Props = new List<UnlockableBase>();
	public List<WorkerController> Workers = new List<WorkerController>();

	private RestaurantData _data;
	
	/// <summary>
	/// Worker Booster 레벨 (0~3)
	/// </summary>
	public int WorkerBoosterLevel
	{
		get
		{
			if (_data != null)
				return _data.WorkerBoosterLevel;
			return 0;
		}
		set
		{
			if (_data != null)
			{
				_data.WorkerBoosterLevel = Mathf.Clamp(value, 0, Define.MAX_WORKER_BOOSTER_LEVEL);
			}
		}
	}
	
	/// <summary>
	/// Worker Speed 레벨 (0~3)
	/// </summary>
	public int WorkerSpeedLevel
	{
		get
		{
			if (_data != null)
				return _data.WorkerSpeedLevel;
			return 0;
		}
		set
		{
			if (_data != null)
			{
				_data.WorkerSpeedLevel = Mathf.Clamp(value, 0, Define.MAX_WORKER_SPEED_LEVEL);
			}
		}
	}
	
	/// <summary>
	/// 부스터 레벨에 따른 작업 시간을 반환합니다.
	/// </summary>
	public float GetWorkerWorkDuration()
	{
		float duration = Define.BASE_WORKER_WORK_DURATION;
		duration -= WorkerBoosterLevel * Define.WORKER_BOOSTER_TIME_REDUCTION;
		return Mathf.Max(duration, 0.1f); // 최소 0.1초는 보장
	}
	
	/// <summary>
	/// Speed 레벨에 따른 이동 속도를 반환합니다.
	/// </summary>
	public float GetWorkerMoveSpeed()
	{
		float speed = Define.BASE_WORKER_MOVE_SPEED;
		speed += WorkerSpeedLevel * Define.WORKER_SPEED_INCREASE;
		return speed;
	}
	
	/// <summary>
	/// 모든 알바생의 이동 속도를 업데이트합니다.
	/// </summary>
	public void UpdateAllWorkersSpeed()
	{
		float newSpeed = GetWorkerMoveSpeed();
		foreach (var worker in Workers)
		{
			if (worker != null)
			{
				StickmanController stickman = worker.GetComponent<StickmanController>();
				if (stickman != null)
				{
					stickman.SetMoveSpeed(newSpeed);
				}
			}
		}
	}

	private void OnEnable()
	{
		GameManager.Instance.AddEventListener(EEventType.HireWorker, OnHireWorker);
		StartCoroutine(CoDistributeWorkerAI());
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.HireWorker, OnHireWorker);
	}

	public void SetInfo(RestaurantData data)
	{
		_data = data;

		RestaurantSystems = GetComponentsInChildren<SystemBase>().ToList();
		Props = GetComponentsInChildren<UnlockableBase>().ToList();

		// UnlockableStates 리스트 초기화
		if (data.UnlockableStates == null)
		{
			data.UnlockableStates = new List<UnlockableStateData>();
		}

		// 기존 프랍들에 저장된 상태 적용
		for (int i = 0; i < Props.Count; i++)
		{
			// UnlockableStates 리스트 확장
			while (data.UnlockableStates.Count <= i)
			{
				data.UnlockableStates.Add(new UnlockableStateData());
			}

			UnlockableStateData stateData = data.UnlockableStates[i];
			Props[i].SetInfo(stateData);
		}

		Tutorial tutorial = GetComponent<Tutorial>();
		if (tutorial != null)
			tutorial.SetInfo(data);

		for (int i = 0; i < data.WorkerCount; i++)
			OnHireWorker();
		
		// 기존 알바생들의 속도 업데이트
		UpdateAllWorkersSpeed();
	}

	void OnHireWorker()
	{
		// 최대 알바생 수 체크
		if (Workers.Count >= Define.MAX_WORKER_COUNT)
		{
			return;
		}

		GameObject go = GameManager.Instance.SpawnWorker();
		WorkerController wc = go.GetComponent<WorkerController>();
		go.transform.position = Define.WORKER_SPAWN_POS;

		Workers.Add(wc);
		
		// 새로 고용된 알바생의 속도 설정
		float currentSpeed = GetWorkerMoveSpeed();
		StickmanController stickman = wc.GetComponent<StickmanController>();
		if (stickman != null)
		{
			stickman.SetMoveSpeed(currentSpeed);
		}

		// 필요하면 세이브 파일 갱신.
		_data.WorkerCount = Mathf.Max(_data.WorkerCount, Workers.Count);
	}

	IEnumerator CoDistributeWorkerAI()
	{
		while (true)
		{
			yield return new WaitForSeconds(1);

			yield return new WaitUntil(() => Workers.Count > 0);

			foreach (WorkerController worker in Workers)
			{				
				// 어딘가 소속되어 있으면 스킵.
				if (worker.CurrentSystem != null)
					continue;

				// 어떤 시스템에 일감이 남아 있으면, 해당 시스템으로 배정.
				foreach (SystemBase system in RestaurantSystems)
				{	
					if (system.HasJob)
					{
						system.AddWorker(worker);
					}
				}
			}
		}
	}
}
