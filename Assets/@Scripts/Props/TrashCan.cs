using DG.Tweening;
using NUnit.Framework.Internal;
using UnityEngine;
using static Define;
using static UnityEditor.Progress;

// 1. 쓰레기 버리는 Trigger
public class TrashCan : MonoBehaviour
{
	public Transform WorkerPos;
	public WorkerController CurrentWorker => _interaction.CurrentWorker;
	private WorkerInteraction _interaction;

	void Start()
    {
		_interaction = Utils.FindChild<WorkerInteraction>(gameObject);
		_interaction.InteractInterval = 0.1f;
		_interaction.OnInteraction = OnWorkerInteraction;
	}

	// 햄버거, 쓰레기 둘 다 버릴 수 있음.
	private void OnWorkerInteraction(WorkerController wc)
	{
		EObjectType type = wc.Tray.CurrentTrayObjectType;

		Transform t = wc.Tray.RemoveFromTray();
		if (t == null)
			return;

		t.DOJump(transform.position, 1f, 1, 0.5f)
			.OnComplete(() =>
			{
				if (type == EObjectType.Burger)
					GameManager.Instance.DespawnBurger(t.gameObject);
				else
					GameManager.Instance.DespawnTrash(t.gameObject);
			});
	}
}
