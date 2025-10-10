using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WorkerInteraction : MonoBehaviour
{
	public Action<WorkerController> OnTriggerStart;
	public Action<WorkerController> OnInteraction;
	public Action<WorkerController> OnTriggerEnd;

	public float InteractInterval = 0.5f;
	public WorkerController CurrentWorker;
	private Coroutine _coWorkerInteraction;

	private void OnEnable()
	{
		if (_coWorkerInteraction != null)
			StopCoroutine(_coWorkerInteraction);

		_coWorkerInteraction = StartCoroutine(CoPlayerInteraction());
	}

	private void OnDisable()
	{
		if (_coWorkerInteraction != null)
			StopCoroutine(_coWorkerInteraction);

		_coWorkerInteraction = null;
	}

	IEnumerator CoPlayerInteraction()
	{
		while (true)
		{
			yield return new WaitForSeconds(InteractInterval);

			if (CurrentWorker != null)
				OnInteraction?.Invoke(CurrentWorker);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		WorkerController wc = other.GetComponent<WorkerController>();
		if (wc == null)
			return;

        CurrentWorker = wc;
		OnTriggerStart?.Invoke(wc);
	}

	void OnTriggerExit(Collider other)
	{
		WorkerController wc = other.GetComponent<WorkerController>();
		if (wc == null)
			return;

		CurrentWorker = null;
		OnTriggerEnd?.Invoke(wc);
	}
}
