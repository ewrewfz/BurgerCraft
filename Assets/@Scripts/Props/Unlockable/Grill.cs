using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 1. 패티 에니메이션 (OK)
// 2. 햄버거 주기적으로 생성 (OK)
// 3. [Collider] 길찾기 막기 (OK)
// 4. Burger Pile (OK)
// 5. [Trigger] 햄버거 영역 안으로 들어오면, 플레이어가 갖고감.
public class Grill : UnlockableBase
{
	[SerializeField] GameObject cookingPopup;
	public GameObject CookingPopupPrefab => cookingPopup;

	private BurgerPile _burgers;
	private WorkerInteraction _interaction;

	public int BurgerCount => _burgers.ObjectCount;
	public WorkerController CurrentWorker => _interaction.CurrentWorker;
	public Transform WorkerPos;
	public GameObject MaxObject;
	public bool StopSpawnBurger = true;
	
	// 점멸 효과 관련
	private Renderer _grillRenderer;
	private Material _originalMaterial;
	private Material _blinkMaterial;
	private Color _originalColor;
	private Tweener _blinkTweener;
	private bool _isBlinking = false;

	protected void Awake()
	{
		_burgers = Utils.FindChild<BurgerPile>(gameObject);

		// 햄버거 인터랙션.
		_interaction = _burgers.GetComponent<WorkerInteraction>();
		_interaction.InteractInterval = 0.2f;
		_interaction.OnInteraction = OnWorkerBurgerInteraction;
		_interaction.OnTriggerStart = OnGrillTriggerStart;
		
		// 점멸 효과를 위한 Renderer 및 Material 초기화
		InitializeBlinkEffect();
	}
	
	private void OnEnable()
	{
		// Counter 이벤트 구독
		Counter.OnPendingOrderAdded += StartBlinkEffect;
		Counter.OnPendingOrdersCleared += StopBlinkEffect;
		
		// 현재 큐 상태 확인하여 점멸 시작
		CheckPendingOrdersAndBlink();
	}
	
	private void OnDisable()
	{
		// Counter 이벤트 구독 해제
		Counter.OnPendingOrderAdded -= StartBlinkEffect;
		Counter.OnPendingOrdersCleared -= StopBlinkEffect;
		
		// 점멸 효과 중지
		StopBlinkEffect();
	}
	
	/// <summary>
	/// 점멸 효과를 위한 Renderer 및 Material 초기화
	/// </summary>
	private void InitializeBlinkEffect()
	{
		// Grill 오브젝트의 Renderer 찾기 (자식 중 "Grill" 이름을 가진 오브젝트)
		Transform grillChild = transform.Find("Grill");
		if (grillChild == null)
		{
			// 직접 자식이 아니면 재귀적으로 찾기
			GameObject grillChildObj = Utils.FindChild(gameObject, "Grill", true);
			if (grillChildObj != null)
			{
				grillChild = grillChildObj.transform;
			}
		}
		
		if (grillChild != null)
		{
			_grillRenderer = grillChild.GetComponent<Renderer>();
			if (_grillRenderer != null && _grillRenderer.material != null)
			{
				// 원본 Material 복사
				_originalMaterial = _grillRenderer.material;
				_blinkMaterial = new Material(_originalMaterial);
				_originalColor = _originalMaterial.color;
				_grillRenderer.material = _blinkMaterial;
			}
		}
	}
	
	/// <summary>
	/// 현재 대기 중인 주문이 있는지 확인하고 점멸 효과 시작
	/// </summary>
	private void CheckPendingOrdersAndBlink()
	{
		Counter counter = FindObjectOfType<Counter>();
		if (counter != null && counter.HasPendingOrders())
		{
			StartBlinkEffect();
		}
	}
	
	/// <summary>
	/// 초록색 점멸 효과 시작
	/// </summary>
	private void StartBlinkEffect()
	{
		if (_isBlinking || _blinkMaterial == null)
			return;
		
		_isBlinking = true;
		
		// 기존 트위너가 있으면 정리
		if (_blinkTweener != null && _blinkTweener.IsActive())
		{
			_blinkTweener.Kill();
		}
		
		// 초록색으로 점멸 (0.5초마다 깜빡임)
		Color greenColor = Color.green;
		_blinkTweener = _blinkMaterial.DOColor(greenColor, 0.5f)
			.SetLoops(-1, LoopType.Yoyo)
			.SetEase(Ease.InOutSine);
	}
	
	/// <summary>
	/// 점멸 효과 중지
	/// </summary>
	private void StopBlinkEffect()
	{
		if (!_isBlinking)
			return;
		
		_isBlinking = false;
		
		// 트위너 중지
		if (_blinkTweener != null && _blinkTweener.IsActive())
		{
			_blinkTweener.Kill();
			_blinkTweener = null;
		}
		
		// 원본 색상으로 복원
		if (_blinkMaterial != null)
		{
			_blinkMaterial.DOColor(_originalColor, 0.3f)
				.SetEase(Ease.OutQuad);
		}
	}

	Coroutine _coSpawnBurger;

	//IEnumerator CoSpawnBurgers()
	//{
	//	while(true)
	//	{
	//		// 최대치 미만이 될 때까지 대기 (여기 도달했다는 것은 미만 상태)
 //           yield return new WaitUntil(() => _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT);

	//		// 미만이면 꺼준다
	//		if (MaxObject != null && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
	//			MaxObject.SetActive(false);

	//		if (StopSpawnBurger == false)
	//		{
	//			_burgers.SpawnObject();

	//			// 스폰 후 최대치 도달 시 켠다
	//			if (MaxObject != null && _burgers.ObjectCount == Define.GRILL_MAX_BURGER_COUNT)
	//				MaxObject.SetActive(true);
	//		}

	//		yield return new WaitForSeconds(Define.GRILL_SPAWN_BURGER_INTERVAL);
	//	}
	//}

	private void OnGrillTriggerStart(WorkerController wc)
	{
		// 플레이어만 팝업 오픈
		if (wc == null || wc.GetComponent<PlayerController>() == null)
			return;

		if (cookingPopup == null)
			return;

		// 이미 활성화된 팝업이 있으면 재사용
		UI_CookingPopup existingPopup = FindObjectOfType<UI_CookingPopup>();
		if (existingPopup != null && existingPopup.gameObject.activeInHierarchy)
		{
			// 이미 열려있으면 아무것도 하지 않음
			return;
		}

		// 풀매니저의 PopupPool에서 비활성화된 팝업 찾기
		UI_CookingPopup popup = null;
		Transform popupPool = PoolManager.Instance.GetPopupPool();
		
		if (popupPool != null)
		{
			// PopupPool의 자식 중에서 비활성화된 UI_CookingPopup 찾기
			for (int i = 0; i < popupPool.childCount; i++)
			{
				Transform child = popupPool.GetChild(i);
				if (child.name == cookingPopup.name)
				{
					UI_CookingPopup foundPopup = child.GetComponent<UI_CookingPopup>();
					if (foundPopup != null && !foundPopup.gameObject.activeSelf)
					{
						popup = foundPopup;
						break;
					}
				}
			}
		}

		// 풀에서 찾지 못했으면 PoolManager를 통해 가져오기 (새로 생성 또는 풀에서 가져오기)
		if (popup == null)
		{
			GameObject instance = PoolManager.Instance.Pop(cookingPopup);
			popup = instance.GetComponent<UI_CookingPopup>();
		}
		
		if (popup != null)
		{
			// 팝업 활성화
			popup.gameObject.SetActive(true);
			
			// Counter에서 대기 중인 주문들을 가져와서 추가
			Counter counter = FindObjectOfType<Counter>();
			if (counter != null)
			{
				List<Define.BurgerRecipe> pendingOrders = counter.GetPendingOrders();
				foreach (Define.BurgerRecipe order in pendingOrders)
				{
					popup.AddOrder(order);
				}
				
				// 주문을 가져갔으므로 점멸 효과 해제 (GetPendingOrders 내부에서 이미 호출되지만 안전을 위해)
				StopBlinkEffect();
			}
		}
	}

	public void OnWorkerBurgerInteraction(WorkerController pc)
	{
		// 쓰레기 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObjectType == Define.EObjectType.Trash)
			return;

		_burgers.PileToTray(pc.Tray);

		// 가져가서 개수가 줄어들었으면 끈다
		if (MaxObject != null && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
			MaxObject.SetActive(false);
	}
}
