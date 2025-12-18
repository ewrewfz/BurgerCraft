using System.Collections.Generic;
using System.Linq;
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
	
	// 주문 큐 (Counter에서 받은 주문들을 관리)
	private List<Define.BurgerRecipe> _orderQueue = new List<Define.BurgerRecipe>();
	
	// 인스펙터 표시용 (디버그)
	[SerializeField] private List<string> _orderQueueDisplay = new List<string>();
	
	// 팝업에 전달된 주문 큐 (표시용)
	private List<Define.BurgerRecipe> _deliveredOrderQueue = new List<Define.BurgerRecipe>();
	
	// 인스펙터 표시용 (전달된 주문)
	[SerializeField] private List<string> _deliveredOrderQueueDisplay = new List<string>();
	
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
		_interaction.                                  InteractInterval = 0.2f;
		_interaction.OnInteraction = OnWorkerBurgerInteraction;
		_interaction.OnTriggerStart = OnGrillTriggerStart;

        InitBlinkEffect();
	}

	/// <summary>
	/// 점멸 효과를 위한 Renderer 및 Material 초기화
	/// </summary>
	private void InitBlinkEffect()
	{
	    // 직접 자식이 아니면 재귀적으로 찾기
	    GameObject grillChild = Utils.FindChild(gameObject, "GrillBG", true);
		
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
		if (_orderQueue.Count > 0)
		{
			StartBlinkEffect();
		}
		else
		{
			StopBlinkEffect();
		}
	}
	
	/// <summary>
	/// Counter에서 주문을 받아 큐에 추가합니다.
	/// </summary>
	public void AddOrder(Define.BurgerRecipe recipe)
	{
		if (recipe.Bread == Define.EBreadType.None)
			return;
		
		_orderQueue.Add(recipe);
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
#endif
		// 주문이 추가되었으므로 점멸 효과 시작
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 주문 큐에 주문이 있는지 확인합니다.
	/// </summary>
	public bool HasOrders()
	{
		return _orderQueue.Count > 0;
	}
	
	/// <summary>
	/// 주문 큐의 모든 주문을 가져옵니다 (CookingPopup에 전달)
	/// 주의: 이 메서드는 주문을 큐에서 제거하지 않습니다. 실제로 처리된 주문만 RemoveOrder로 제거해야 합니다.
	/// </summary>
	public List<Define.BurgerRecipe> GetOrders()
	{
		// 주문을 복사해서 반환 (원본 큐는 유지)
		List<Define.BurgerRecipe> orders = new List<Define.BurgerRecipe>(_orderQueue);
		
		// 전달된 주문 큐 업데이트 (표시용)
		// 이미 전달된 주문은 유지하고, 새로운 주문만 추가
		foreach (var order in orders)
		{
			// 이미 전달된 주문 큐에 있는지 확인 (IsMatch로 비교)
			bool alreadyDelivered = false;
			foreach (var delivered in _deliveredOrderQueue)
			{
				if (UI_OrderSystem.IsMatch(delivered, order))
				{
					alreadyDelivered = true;
					break;
				}
			}
			
			// 전달되지 않은 주문만 추가
			if (!alreadyDelivered)
			{
				_deliveredOrderQueue.Add(order);
			}
		}
		
		UpdateDeliveredOrderQueueDisplay();
		
		return orders;
	}
	
	/// <summary>
	/// 처리된 주문을 큐에서 제거합니다 (CookingPopup에서 완료된 주문에 대해 호출)
	/// </summary>
	public void RemoveOrder(Define.BurgerRecipe recipe)
	{
		// struct이므로 직접 비교해서 제거
		_orderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, recipe));
		_deliveredOrderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, recipe));
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		// 주문이 모두 처리되었으면 점멸 효과 해제
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 처리되지 않은 주문들을 다시 큐에 추가합니다 (팝업이 닫힐 때 호출)
	/// </summary>
	public void ReturnOrders(List<Define.BurgerRecipe> orders)
	{
		if (orders == null || orders.Count == 0)
			return;
		
		// 이미 큐에 있는 주문은 추가하지 않음 (중복 방지)
		foreach (var order in orders)
		{
			if (!_orderQueue.Contains(order))
			{
				_orderQueue.Add(order);
			}
		}
		
		// 전달된 주문 큐에서 반환된 주문 제거 (struct이므로 직접 비교)
		foreach (var order in orders)
		{
			_deliveredOrderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, order));
		}
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		// 주문이 다시 추가되었으므로 점멸 효과 시작
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 인스펙터 표시용 주문 큐를 업데이트합니다.
	/// </summary>
	private void UpdateOrderQueueDisplay()
	{
		_orderQueueDisplay.Clear();
		
		foreach (var order in _orderQueue)
		{
			_orderQueueDisplay.Add(FormatOrderToString(order));
		}
	}
	
	/// <summary>
	/// 인스펙터 표시용 전달된 주문 큐를 업데이트합니다.
	/// </summary>
	private void UpdateDeliveredOrderQueueDisplay()
	{
		_deliveredOrderQueueDisplay.Clear();
		
		foreach (var order in _deliveredOrderQueue)
		{
			_deliveredOrderQueueDisplay.Add(FormatOrderToString(order));
		}
	}
	
	/// <summary>
	/// 주문을 문자열로 변환합니다.
	/// </summary>
	private string FormatOrderToString(Define.BurgerRecipe order)
	{
		// 주문을 간단한 문자열로 변환
		string orderText = $"빵:{order.Bread}, 패티:{order.Patty}({order.PattyCount}), ";
		
		if (order.Veggies != null && order.Veggies.Count > 0)
		{
			var veggieGroups = order.Veggies
				.Where(v => v != Define.EVeggieType.None)
				.GroupBy(v => v)
				.ToList();
			
			var veggieList = new List<string>();
			foreach (var group in veggieGroups)
			{
				string veggieName = group.Key == Define.EVeggieType.Lettuce ? "양상추" : "토마토";
				veggieList.Add($"{veggieName}({group.Count()})");
			}
			orderText += $"야채:[{string.Join(", ", veggieList)}], ";
		}
		else
		{
			orderText += "야채:없음, ";
		}
		
		orderText += $"소스1:{order.Sauce1Count}, 소스2:{order.Sauce2Count}";
		
		return orderText;
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

	private void OnGrillTriggerStart(WorkerController wc)
	{
		// 플레이어만 처리
		if (wc == null || wc.GetComponent<PlayerController>() == null)
			return;

		// 버거가 있으면 OnInteraction에서 처리하므로 여기서는 팝업만 처리
		// 버거가 없고 주문이 있을 때만 CookingPopup 열기
		if (_burgers.ObjectCount == 0)
		{
			// 주문 큐에 주문이 없으면 팝업을 열지 않음
			if (!HasOrders())
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
				
				// 주문 큐에서 모든 주문을 가져와서 팝업에 전달 (팝업 내부에서 최대 3개만 표시)
				// 주의: GetOrders()는 주문을 큐에서 제거하지 않음 (복사본 반환)
				List<Define.BurgerRecipe> orders = GetOrders();
				popup.AddOrders(orders);
			}
		}
	}

	public void OnWorkerBurgerInteraction(WorkerController pc)
	{
		if (pc == null) 
			return;

		// 쓰레기 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObjectType == Define.EObjectType.Trash)
			return;

		// 플레이어만 처리
		if (pc.GetComponent<PlayerController>() == null)
			return;

		// 그릴에 버거가 있으면 트레이에 올리기
		if (_burgers.ObjectCount > 0)
		{
			// 트레이가 비어있거나 버거만 있고, 최대 개수 미만이면 받을 수 있음
			if ((pc.Tray.CurrentTrayObjectType == Define.EObjectType.None || 
			     pc.Tray.CurrentTrayObjectType == Define.EObjectType.Burger) &&
			    pc.Tray.TotalItemCount < Define.MAX_BURGER_ADD_COUNT)
			{
				_burgers.PileToTray(pc.Tray);

				// 가져가서 개수가 줄어들었으면 끈다
				if (MaxObject != null && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
					MaxObject.SetActive(false);
			}
		}
	}
}
