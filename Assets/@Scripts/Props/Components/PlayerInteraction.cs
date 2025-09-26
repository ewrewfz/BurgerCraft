using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PlayerInteraction : MonoBehaviour
{
	public Action<PlayerController> OnPlayerInteraction;
	public float InteractInterval = 0.5f;
	private PlayerController _player;

	private void Start()
	{
		StartCoroutine(CoPlayerInteraction());
	}

	IEnumerator CoPlayerInteraction()
	{
		while (true)
		{
			yield return new WaitForSeconds(InteractInterval);

			if (_player != null)
				OnPlayerInteraction(_player);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		PlayerController pc = other.GetComponent<PlayerController>();
		if (pc == null)
			return;

		_player = pc;
	}

	void OnTriggerExit(Collider other)
	{
		PlayerController pc = other.GetComponent<PlayerController>();
		if (pc == null)
			return;

		_player = null;
	}
}
