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
	public Transform BurgerPickupPos; // 버거 픽업 전용 위치
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
	
	// 게스트 ID별 주문 매핑 (주문 삭제 시 정확한 손님의 주문만 제거하기 위해)
	// Key: 게스트 인스턴스 ID, Value: 해당 게스트의 주문 리스트
	private Dictionary<int, List<Define.BurgerRecipe>> _guestIdOrderMapping = new Dictionary<int, List<Define.BurgerRecipe>>();
	
	// 주문별 주문 번호 매핑 (레시피를 키로 사용하여 주문 번호 추적)
	// Key: 게스트 인스턴스 ID, Value: 주문 번호 (문자열)
	private Dictionary<int, string> _guestIdOrderNumberMapping = new Dictionary<int, string>();
	
	// 알바생이 현재 조리 중인 주문 추적 (플레이어가 조리하지 못하도록)
	private Define.BurgerRecipe? _workerCookingOrder = null;
	public bool IsWorkerCooking => _workerCookingOrder.HasValue;
	public Define.BurgerRecipe? WorkerCookingOrder => _workerCookingOrder;
	
	// 플레이어가 현재 조리 중인 주문 추적 (알바생이 조리하지 못하도록)
	private Define.BurgerRecipe? _playerCookingOrder = null;
	public bool IsPlayerCooking => _playerCookingOrder.HasValue;
	public Define.BurgerRecipe? PlayerCookingOrder => _playerCookingOrder;
	
	/// <summary>
	/// 플레이어가 조리 중인 주문을 설정합니다.
	/// </summary>
	public void SetPlayerCookingOrder(Define.BurgerRecipe? recipe)
	{
		_playerCookingOrder = recipe;
	}
	
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
		_interaction.OnTriggerEnd = OnGrillTriggerEnd;

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
	/// _orderQueue 또는 _deliveredOrderQueue 중 하나라도 주문이 있으면 점멸
	/// </summary>
	private void CheckPendingOrdersAndBlink()
	{
		if (_orderQueue.Count > 0 || _deliveredOrderQueue.Count > 0)
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
		AddOrder(recipe, null);
	}
	
	/// <summary>
	/// Counter에서 주문을 받아 큐에 추가합니다 (손님 정보 포함)
	/// </summary>
	public void AddOrder(Define.BurgerRecipe recipe, GuestController guest, string orderNumber = null)
	{
		if (recipe.Bread == Define.EBreadType.None)
			return;
		
		_orderQueue.Add(recipe);
		
		// 게스트 ID별 주문 매핑에 추가
		if (guest != null)
		{
			int guestId = guest.GetInstanceID();
			if (!_guestIdOrderMapping.ContainsKey(guestId))
			{
				_guestIdOrderMapping[guestId] = new List<Define.BurgerRecipe>();
			}
			_guestIdOrderMapping[guestId].Add(recipe);
			
			// 주문 번호 저장 (게스트 ID별로 첫 주문일 때만 저장)
			if (!string.IsNullOrEmpty(orderNumber) && !_guestIdOrderNumberMapping.ContainsKey(guestId))
			{
				_guestIdOrderNumberMapping[guestId] = orderNumber;
			}
		}
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
#endif
		// 주문이 추가되었으므로 점멸 효과 시작
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 게스트 ID로 주문 번호를 가져옵니다.
	/// </summary>
	public string GetOrderNumber(int guestId)
	{
		if (_guestIdOrderNumberMapping.ContainsKey(guestId))
		{
			return _guestIdOrderNumberMapping[guestId];
		}
		return null;
	}
	
	/// <summary>
	/// 레시피로 주문 번호를 가져옵니다.
	/// 주문 큐의 순서를 우선적으로 고려하여 정확한 주문 번호를 반환합니다.
	/// </summary>
	public string GetOrderNumberByRecipe(Define.BurgerRecipe recipe)
	{
		// 먼저 _deliveredOrderQueue에서 해당 레시피를 찾고, 그 순서에 맞는 게스트 ID 찾기
		// _deliveredOrderQueue의 순서가 실제 처리 순서이므로 이를 우선적으로 사용
		foreach (var orderInQueue in _deliveredOrderQueue)
		{
			if (UI_OrderSystem.IsMatch(orderInQueue, recipe))
			{
				// 이 레시피를 가진 게스트 ID 찾기
				foreach (var kvp in _guestIdOrderMapping)
				{
					foreach (var order in kvp.Value)
					{
						if (UI_OrderSystem.IsMatch(order, orderInQueue))
						{
							return GetOrderNumber(kvp.Key);
						}
					}
				}
			}
		}
		
		// _deliveredOrderQueue에서 찾지 못했으면 _orderQueue에서 찾기
		foreach (var orderInQueue in _orderQueue)
		{
			if (UI_OrderSystem.IsMatch(orderInQueue, recipe))
			{
				// 이 레시피를 가진 게스트 ID 찾기
				foreach (var kvp in _guestIdOrderMapping)
				{
					foreach (var order in kvp.Value)
					{
						if (UI_OrderSystem.IsMatch(order, orderInQueue))
						{
							return GetOrderNumber(kvp.Key);
						}
					}
				}
			}
		}
		
		// 큐에서 찾지 못했으면 기존 방식으로 폴백 (하위 호환성)
		foreach (var kvp in _guestIdOrderMapping)
		{
			foreach (var order in kvp.Value)
			{
				if (UI_OrderSystem.IsMatch(order, recipe))
				{
					return GetOrderNumber(kvp.Key);
				}
			}
		}
		return null;
	}
	
	/// <summary>
	/// 주문 큐에 주문이 있는지 확인합니다.
	/// _orderQueue 또는 _deliveredOrderQueue 중 하나라도 주문이 있으면 true 반환
	/// </summary>
	public bool HasOrders()
	{
		return _orderQueue.Count > 0 || _deliveredOrderQueue.Count > 0;
	}
	
	/// <summary>
	/// 주문 큐의 모든 주문을 가져옵니다 (CookingPopup에 전달)
	/// _orderQueue와 _deliveredOrderQueue의 모든 주문을 반환합니다.
	/// _orderQueue의 주문은 _deliveredOrderQueue로 이동합니다.
	/// </summary>
	public List<Define.BurgerRecipe> GetOrders()
	{
		// 반환할 주문 리스트 (기존 _deliveredOrderQueue의 주문 포함)
		List<Define.BurgerRecipe> orders = new List<Define.BurgerRecipe>(_deliveredOrderQueue);
		
		// _orderQueue의 주문을 _deliveredOrderQueue로 이동
		// 이미 전달된 주문은 중복 추가하지 않음
		foreach (var order in _orderQueue)
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
			
			// 전달되지 않은 주문만 _deliveredOrderQueue에 추가
			if (!alreadyDelivered)
			{
				_deliveredOrderQueue.Add(order);
				orders.Add(order);
			}
		}
		
		// _orderQueue에서 전달된 주문 제거
		_orderQueue.Clear();
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		
		return orders;
	}
	
	/// <summary>
	/// 처리된 주문을 큐에서 제거합니다 (CookingPopup에서 완료된 주문에 대해 호출)
	/// 같은 레시피가 여러 개 있어도 하나만 제거
	/// </summary>
	public void RemoveOrder(Define.BurgerRecipe recipe)
	{
		// _deliveredOrderQueue에서 첫 번째로 일치하는 주문 하나만 제거
		for (int i = _deliveredOrderQueue.Count - 1; i >= 0; i--)
		{
			if (UI_OrderSystem.IsMatch(_deliveredOrderQueue[i], recipe))
			{
				_deliveredOrderQueue.RemoveAt(i);
				break; // 하나만 제거하고 종료
			}
		}
		
		// _orderQueue에서도 첫 번째로 일치하는 주문 하나만 제거
		for (int i = _orderQueue.Count - 1; i >= 0; i--)
		{
			if (UI_OrderSystem.IsMatch(_orderQueue[i], recipe))
			{
				_orderQueue.RemoveAt(i);
				break; // 하나만 제거하고 종료
			}
		}
		
		// _guestIdOrderMapping에서도 해당 레시피 제거 (중요: 매핑 동기화)
		foreach (var kvp in _guestIdOrderMapping.ToList())
		{
			int guestId = kvp.Key;
			List<Define.BurgerRecipe> guestOrders = kvp.Value;
			
			// 해당 게스트의 주문 리스트에서 일치하는 레시피 제거
			for (int i = guestOrders.Count - 1; i >= 0; i--)
			{
				if (UI_OrderSystem.IsMatch(guestOrders[i], recipe))
				{
					guestOrders.RemoveAt(i);
					break; // 하나만 제거하고 종료
				}
			}
			
			// 게스트의 모든 주문이 제거되었으면 매핑에서도 제거
			if (guestOrders.Count == 0)
			{
				_guestIdOrderMapping.Remove(guestId);
				_guestIdOrderNumberMapping.Remove(guestId);
			}
		}
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		// 주문이 모두 처리되었으면 점멸 효과 해제
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 주문 번호로 주문을 삭제합니다 (3회 실패 시 호출)
	/// 주문 번호를 기준으로 해당 주문 번호의 모든 주문을 정확히 제거
	/// </summary>
	public void RemoveOrdersByOrderNumber(string orderNumber)
	{
		if (string.IsNullOrEmpty(orderNumber))
			return;
		
		// 주문 번호에 해당하는 게스트 ID 찾기
		List<int> guestIdsToRemove = new List<int>();
		foreach (var kvp in _guestIdOrderNumberMapping)
		{
			if (kvp.Value == orderNumber)
			{
				guestIdsToRemove.Add(kvp.Key);
			}
		}
		
		if (guestIdsToRemove.Count == 0)
		{
			Debug.LogWarning($"[Grill] RemoveOrdersByOrderNumber: 주문 번호 '{orderNumber}'에 해당하는 주문을 찾을 수 없습니다.");
			return;
		}
		
		// 해당 주문 번호의 모든 게스트 주문 삭제
		foreach (int guestId in guestIdsToRemove)
		{
			// _guestIdOrderMapping에서 해당 게스트의 주문 리스트 가져오기
			if (!_guestIdOrderMapping.ContainsKey(guestId))
				continue;
			
			List<Define.BurgerRecipe> guestOrders = _guestIdOrderMapping[guestId];
			if (guestOrders == null || guestOrders.Count == 0)
				continue;
			
			// 해당 게스트의 모든 주문을 _deliveredOrderQueue에서 제거
			foreach (var order in guestOrders)
			{
				_deliveredOrderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, order));
			}
			
			// _orderQueue에서도 제거
			if (_orderQueue.Count > 0)
			{
				foreach (var order in guestOrders)
				{
					_orderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, order));
				}
			}
			
			// 매핑에서도 제거
			_guestIdOrderMapping.Remove(guestId);
			_guestIdOrderNumberMapping.Remove(guestId);
		}
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		// 주문이 모두 처리되었으면 점멸 효과 해제
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 특정 손님의 주문을 큐에서 삭제합니다 (3회 실패 시 호출)
	/// 게스트 ID를 기반으로 해당 손님의 주문만 정확히 제거
	/// </summary>
	public void RemoveGuestOrders(int guestId, int orderCount)
	{
		if (guestId == 0 || orderCount <= 0)
			return;
		
		// _guestIdOrderMapping에서 해당 게스트 ID의 주문 리스트 가져오기
		if (!_guestIdOrderMapping.ContainsKey(guestId))
		{
			Debug.LogWarning($"[Grill] RemoveGuestOrders: 게스트 ID {guestId}의 주문 매핑을 찾을 수 없습니다.");
			return;
		}
		
		List<Define.BurgerRecipe> guestOrders = _guestIdOrderMapping[guestId];
		if (guestOrders == null || guestOrders.Count == 0)
		{
			Debug.LogWarning($"[Grill] RemoveGuestOrders: 게스트 ID {guestId}의 주문 리스트가 비어있습니다.");
			return;
		}
		
		// 해당 게스트의 주문을 _deliveredOrderQueue에서 정확히 제거
		int removedCount = 0;
		foreach (var order in guestOrders)
		{
			if (removedCount >= orderCount)
				break;
				
			// _deliveredOrderQueue에서 해당 주문 제거 (IsMatch로 비교)
			_deliveredOrderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, order));
			removedCount++;
		}
		
		// _orderQueue에서도 제거 (혹시 남아있을 수 있으므로)
		if (_orderQueue.Count > 0)
		{
			removedCount = 0;
			foreach (var order in guestOrders)
			{
				if (removedCount >= orderCount)
					break;
					
				_orderQueue.RemoveAll(r => UI_OrderSystem.IsMatch(r, order));
				removedCount++;
			}
		}
		
		// 매핑에서도 제거
		_guestIdOrderMapping.Remove(guestId);
		
		// 주문 번호 매핑도 제거
		if (_guestIdOrderNumberMapping.ContainsKey(guestId))
		{
			_guestIdOrderNumberMapping.Remove(guestId);
		}
		
#if UNITY_EDITOR
		// 인스펙터 표시용 업데이트
		UpdateOrderQueueDisplay();
		UpdateDeliveredOrderQueueDisplay();
#endif
		// 주문이 모두 처리되었으면 점멸 효과 해제
		CheckPendingOrdersAndBlink();
	}
	
	/// <summary>
	/// 특정 손님의 주문을 큐에서 삭제합니다 (GuestController 오버로드)
	/// </summary>
	public void RemoveGuestOrders(GuestController guest, int orderCount)
	{
		if (guest == null)
			return;
		
		RemoveGuestOrders(guest.GetInstanceID(), orderCount);
	}
	
	/// <summary>
	/// 처리되지 않은 주문들을 다시 큐에 추가합니다 (팝업이 닫힐 때 호출)
	/// _deliveredOrderQueue에서 처리되지 않은 주문을 _orderQueue로 다시 이동
	/// </summary>
	public void ReturnOrders(List<Define.BurgerRecipe> orders)
	{
		if (orders == null || orders.Count == 0)
			return;
		
		// 처리되지 않은 주문을 _orderQueue에 추가 (중복 방지)
		foreach (var order in orders)
		{
			// 이미 _orderQueue에 있는지 확인 (IsMatch로 비교)
			bool alreadyInQueue = false;
			foreach (var existingOrder in _orderQueue)
			{
				if (UI_OrderSystem.IsMatch(existingOrder, order))
				{
					alreadyInQueue = true;
					break;
				}
			}
			
			if (!alreadyInQueue)
			{
				_orderQueue.Add(order);
			}
		}
		
		// 전달된 주문 큐에서 반환된 주문 제거 (struct이므로 IsMatch로 비교)
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
		if (wc == null)
			return;
		
		// 플레이어인 경우 기존 로직 실행
		if (wc.GetComponent<PlayerController>() != null)
		{
			// 알바생이 이미 작업 중이면 플레이어는 나가게 함
			if (CurrentWorker != null && CurrentWorker.GetComponent<PlayerController>() == null)
			{
				// 알바생이 작업 중이므로 플레이어를 나가게 함
				Vector3 exitPos = WorkerPos.position - WorkerPos.forward * 1.5f;
				wc.SetDestination(exitPos);
				return;
			}
			
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
		// 알바생인 경우 진행바 표시 및 자동 조리 완료
		else
		{
			// 주문이 있고 버거가 최대 개수 미만이면 자동 조리 시작
			if (HasOrders() && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
			{
				StartWorkerAutoCooking(wc);
			}
		}
	}
	
	/// <summary>
	/// Worker가 Grill 존에서 나갈 때 호출
	/// </summary>
	private void OnGrillTriggerEnd(WorkerController wc)
	{
		if (wc == null)
			return;
		
		// 알바생인 경우 진행바 비활성화
		if (wc.GetComponent<PlayerController>() == null)
		{
			UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
			if (progressbar != null)
			{
				progressbar.StopProgress();
				progressbar.gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// 알바생이 자동으로 조리를 완료하는 로직
	/// </summary>
	public void StartWorkerAutoCooking(WorkerController wc)
	{
		// Worker의 진행바 찾기
		UI_Progressbar progressbar = wc.GetComponentInChildren<UI_Progressbar>(true);
		if (progressbar == null)
		{
			Debug.LogWarning("[Grill] Worker의 UI_Progressbar를 찾을 수 없습니다.");
			return;
		}
		
		// 주문 큐에서 플레이어가 조리하지 않는 첫 번째 주문 가져오기
		Define.BurgerRecipe? orderNullable = null;
		
		// _deliveredOrderQueue에서 플레이어가 조리하지 않는 주문 찾기
		if (_deliveredOrderQueue.Count > 0)
		{
			foreach (var orderItem in _deliveredOrderQueue)
			{
				// 플레이어가 조리 중인 주문이면 건너뛰기
				if (_playerCookingOrder.HasValue && UI_OrderSystem.IsMatch(orderItem, _playerCookingOrder.Value))
				{
					continue;
				}
				orderNullable = orderItem;
				break;
			}
		}
		
		// _deliveredOrderQueue에서 찾지 못했으면 _orderQueue에서 찾기
		if (!orderNullable.HasValue && _orderQueue.Count > 0)
		{
			foreach (var orderItem in _orderQueue)
			{
				// 플레이어가 조리 중인 주문이면 건너뛰기
				if (_playerCookingOrder.HasValue && UI_OrderSystem.IsMatch(orderItem, _playerCookingOrder.Value))
				{
					continue;
				}
				orderNullable = orderItem;
				break;
			}
		}
		
		// 주문이 없으면 진행바 비활성화
		if (!orderNullable.HasValue)
		{
			progressbar.gameObject.SetActive(false);
			return;
		}
		
		Define.BurgerRecipe order = orderNullable.Value;
		
		if (order.Bread == Define.EBreadType.None)
		{
			progressbar.gameObject.SetActive(false);
			return;
		}
		
		// 알바생이 조리 중인 주문으로 설정 (플레이어가 조리하지 못하도록)
		_workerCookingOrder = order;
		
		// UI_CookingPopup에 알림 (Working_AI 활성화)
		UI_CookingPopup cookingPopup = FindObjectOfType<UI_CookingPopup>();
		if (cookingPopup != null && cookingPopup.gameObject.activeInHierarchy)
		{
			cookingPopup.SetReceiptWorkingAI(order, true);
		}
		
		// 진행바 활성화
		progressbar.gameObject.SetActive(true);
		
		// 진행바 완료 콜백 설정
		progressbar.OnProgressComplete = () =>
		{
			// 주문 번호 가져오기 (레시피로 조회)
			string orderNumber = GetOrderNumberByRecipe(order);
			
			// 주문 제거
			RemoveOrder(order);
			
			// 알바생 조리 중인 주문 해제
			_workerCookingOrder = null;
			
			// UI_CookingPopup에 알림 (Working_AI 비활성화)
			if (cookingPopup != null && cookingPopup.gameObject.activeInHierarchy)
			{
				cookingPopup.SetReceiptWorkingAI(order, false);
			}
			
			// 버거 생성 (조리 완료) - BurgerPile의 SpawnObjectWithOrderNumber 사용
			if (_burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
			{
				GameObject burger = _burgers.SpawnObjectWithOrderNumber();
				
				// 주문 번호 설정 (손님과 버거 연결)
				if (burger != null && !string.IsNullOrEmpty(orderNumber))
				{
					BurgerOrderNumber orderNumberComponent = burger.GetComponent<BurgerOrderNumber>();
					if (orderNumberComponent == null)
					{
						orderNumberComponent = burger.AddComponent<BurgerOrderNumber>();
					}
					orderNumberComponent.OrderNumber = orderNumber;
				}
			}
			
			// 주문이 더 있고 버거가 최대 개수 미만이면 다시 시작, 없으면 진행바 비활성화
			if (HasOrders() && _burgers.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
			{
				// 다음 주문 조리 시작 (재귀 호출)
				StartWorkerAutoCooking(wc);
			}
			else
			{
				progressbar.gameObject.SetActive(false);
			}
		};
		
		// 진행바 시작 (부스터 레벨에 따라 시간 조정)
		float workDuration = Define.BASE_WORKER_WORK_DURATION;
		if (GameManager.Instance != null && GameManager.Instance.Restaurant != null)
		{
			workDuration = GameManager.Instance.Restaurant.GetWorkerWorkDuration();
		}
		progressbar.StartProgress(workDuration);
	}

	public void OnWorkerBurgerInteraction(WorkerController pc)
	{
		if (pc == null) 
			return;

		// 쓰레기 운반 상태에선 안 됨.
		if (pc.Tray.CurrentTrayObjectType == Define.EObjectType.Trash)
			return;

		// 플레이어와 알바생 모두 처리
		// if (pc.GetComponent<PlayerController>() == null)
		//     return;

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
