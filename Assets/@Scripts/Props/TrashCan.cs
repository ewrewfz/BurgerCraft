using DG.Tweening;
using NUnit.Framework.Internal;
using UnityEngine;
using static Define;
using static UnityEditor.Progress;

// 1. 쓰레기 버리는 Trigger
public class TrashCan : MonoBehaviour
{
    void Start()
    {
		PlayerInteraction interaction = Utils.FindChild<PlayerInteraction>(gameObject);
		interaction.InteractInterval = 0.1f;
		interaction.OnPlayerInteraction = OnPlayerInteraction;
	}

	// 햄버거, 쓰레기 둘 다 버릴 수 있음.
	private void OnPlayerInteraction(PlayerController pc)
	{
		Transform t = pc.Tray.RemoveFromTray();
		if (t == null)
			return;

		ETrayObject type = pc.Tray.CurrentTrayObject;

		t.DOJump(transform.position, 1f, 1, 0.5f)
			.OnComplete(() =>
			{
				if (type == ETrayObject.Burger)
					GameManager.Instance.DespawnBurger(t.gameObject);
				else
					GameManager.Instance.DespawnTrash(t.gameObject);
			});
	}
}
