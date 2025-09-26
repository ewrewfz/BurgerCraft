using System.Collections;
using UnityEngine;

// 1. 패티 에니메이션 (OK)
// 2. 햄버거 주기적으로 생성 (OK)
// 3. [Collider] 길찾기 막기 (OK)
// 4. Burger Pile (OK)
// 5. [Trigger] 햄버거 영역 안으로 들어오면, 플레이어가 갖고감.
public class Grill : MonoBehaviour
{
	private BurgerPile _burgers;

	void Start()
	{
		_burgers = Utils.FindChild<BurgerPile>(gameObject);

		// 햄버거 인터랙션.
		PlayerInteraction interaction = _burgers.GetComponent<PlayerInteraction>();
		interaction.InteractInterval = 0.2f;
		interaction.OnPlayerInteraction = OnPlayerBurgerInteraction;

		// 햄버거 스폰.
		StartCoroutine(CoSpawnBurgers());
	}

	IEnumerator CoSpawnBurgers()
	{
		while (true)
		{
			yield return new WaitUntil(() => _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT);

			GameObject go = GameManager.Instance.SpawnBurger();
			_burgers.AddToPile(go);

			yield return new WaitForSeconds(Define.GRILL_SPAWN_BURGER_INTERVAL);
		}
	}

	void OnPlayerBurgerInteraction(PlayerController pc)
	{
		// 쓰레기 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObject == Define.ETrayObject.Trash)
			return;

		GameObject go = _burgers.RemoveFromPile();
		if (go == null)
			return;

		pc.Tray.AddToTray(go.transform);
	}
}
