using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using static Define;
//using static UnityEngine.Rendering.DebugUI;

public class MainCounterSystem : SystemBase
{
	public Grill Grill;
	public Counter Counter;
	public List<Table> Tables = new List<Table>();
	public TrashCan TrashCan;
	public List<WorkerController> Workers = new List<WorkerController>();

	// 직원들이 담당하는 일들.
	public WorkerController[] Jobs = new WorkerController[(int)EMainCounterJob.MaxCount];

	private void Awake()
	{
		FindProps();
	}

	private void OnEnable()
	{
		GameManager.Instance.AddEventListener(EEventType.HireWorker, OnHireWorker);
		GameManager.Instance.AddEventListener(EEventType.UnlockProp, FindProps);
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.HireWorker, OnHireWorker);
		GameManager.Instance.RemoveEventListener(EEventType.UnlockProp, FindProps);
	}

	private void Update()
	{
	}

	void FindProps()
	{
		if (Grill == null)
			Grill = Utils.FindChild<Grill>(gameObject, recursive: true);
		if (Counter == null)
			Counter = Utils.FindChild<Counter>(gameObject, recursive: true);
		if (TrashCan == null)
			TrashCan = Utils.FindChild<TrashCan>(gameObject, recursive: true);

		Tables = gameObject.GetComponentsInChildren<Table>().ToList();

		if (Counter != null)
			Counter.Owner = this;
	}

	#region Worker
	void OnHireWorker()
	{
		GameObject go = GameManager.Instance.SpawnWorker();
		WorkerController wc = go.GetComponent<WorkerController>();
		go.transform.position = Vector3.zero;

		// 나중에는 직원 배치를 여러 시스템(MainCounter, Drive-Thru 등)중 하나를 골라서 한다.
		Workers.Add(wc);

		StartCoroutine(DoMainCounterWorkerJob(wc));
	}

	bool ShouldDoJob(EMainCounterJob jobType)
	{
		// 이미 다른 직원이 점유중이라면 스킵.
		WorkerController wc = Jobs[(int)jobType];
		if (wc != null)
			return false;

		// 일감이 있는지 확인.
		switch (jobType)
		{
			case EMainCounterJob.MoveBurger:
				{
					if (Grill == null)
						return false;
					if (Grill.CurrentWorker != null)
						return false;
					if (Grill.BurgerCount == 0)
						return false;
					if (Counter.NeedMoreBurgers == false)
						return false;
					return true;
				}
			case EMainCounterJob.CounterCashier:
				{
					if (Counter == null)
						return false;
					if (Counter.CurrentCashierWorker != null)
						return false;
					if (Counter.NeedCashier == false)
						return false;
					if (Counter.FindTableToServeGuests() == null)
						return false;

					return true;
				}
			case EMainCounterJob.CleanTable:
				{
					foreach (Table table in Tables)
					{
						if (table.TableState == ETableState.Dirty)
							return true;
					}
					return false;
				}
		}

		return false;
	}

	IEnumerator DoMainCounterWorkerJob(WorkerController wc)
	{
		while (true)
		{
			yield return new WaitForSeconds(1);

			// 햄버거 운반.
			if (ShouldDoJob(EMainCounterJob.MoveBurger))
			{
				// 일감 점유.
				Jobs[(int)EMainCounterJob.MoveBurger] = wc;

				// 그릴로 이동.
				wc.SetDestination(Grill.WorkerPos.position);

				// 가는중.
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);

				// 그릴 도착했으면 일정 시간 대기.
				wc.transform.rotation = Grill.WorkerPos.rotation;
				yield return new WaitForSeconds(3);

				// 햄버거 수집했으면 카운터로 이동.
				wc.SetDestination(Counter.BurgerWorkerPos.position);

				// 가는중.
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);

				// 카운터 도착했으면 일정 시간 대기.
				wc.transform.rotation = Counter.BurgerWorkerPos.rotation;
				yield return new WaitForSeconds(2); // TODO : Tray에 MAX 수치 만들어지면, 다 채울 때까지로 수정.

				// 일감 점유 해제.
				Jobs[(int)EMainCounterJob.MoveBurger] = null;
			}

			// 카운터 계산대.
			if (ShouldDoJob(EMainCounterJob.CounterCashier))
			{
				// 일감 점유.
				Jobs[(int)EMainCounterJob.CounterCashier] = wc;

				// 계산대로 이동.
				wc.SetDestination(Counter.CashierWorkerPos.position);

				// 가는중.
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);

				// 계산대 도착했으면 일정 시간 대기.
				wc.transform.rotation = Counter.CashierWorkerPos.rotation;
				yield return new WaitUntil(() => Counter.FindTableToServeGuests() == null);

				// 일감 점유 해제.
				Jobs[(int)EMainCounterJob.CounterCashier] = null;
			}

			// 테이블 청소.
			if (ShouldDoJob(EMainCounterJob.CleanTable))
			{
				Table table = Tables.Where(t => t.TableState == ETableState.Dirty).FirstOrDefault();
				if (table == null)
					continue;

				// 일감 점유.
				Jobs[(int)EMainCounterJob.CleanTable] = wc;

				// 테이블로 이동.
				wc.SetDestination(table.WorkerPos.position);

				// 가는중.
				yield return new WaitUntil(() => wc.HasArrivedAtDestination);

				// 테이블 도착했으면 일정 시간 대기.
				wc.transform.rotation = table.WorkerPos.rotation;
				yield return new WaitUntil(() => table.TableState != ETableState.Dirty);

				// 쓰레기통으로 이동.
				wc.SetDestination(TrashCan.WorkerPos.position);

				// 쓰레기통 도착했으면 일정 시간 대기.
				wc.transform.rotation = table.WorkerPos.rotation;
				yield return new WaitUntil(() => wc.IsServing == false);

				// 일감 점유 해제.
				Jobs[(int)EMainCounterJob.CleanTable] = null;
			}
		}
	}

	public bool HasEmptyCleanTable()
	{
		foreach (Table table in Tables)
		{
			if (table.TableState == ETableState.None)
				return true;
		}

		return false;
	}
	#endregion
}
