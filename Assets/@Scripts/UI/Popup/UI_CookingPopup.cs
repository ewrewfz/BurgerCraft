using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버거 조리 UI. 최대 3개 주문을 받고, 재료 버튼/그릴/조립/트래시 연동을 담당.
/// </summary>
public class UI_CookingPopup : MonoBehaviour
{
    public enum EIngredientType
    {
        Bread,
        Patty,
        Lettuce,
        Tomato,
        Sauce1,
        Sauce2,
    }

    [Header("Receipts")]
    [SerializeField] private Transform _receiptParent;
    [SerializeField] private UI_CookingReceipt _receiptPrefab;

    [Header("Buttons")]
    [SerializeField] private Button _breadButton;
    [SerializeField] private Button _pattyButton;
    [SerializeField] private Button _lettuceButton;
    [SerializeField] private Button _tomatoButton;
    [SerializeField] private Button _sauce1Button;
    [SerializeField] private Button _sauce2Button;

    [SerializeField] private Button _exitButton;

    [Header("Transforms")]
    [SerializeField] private Transform _flamePattyPos;
    [SerializeField] private Transform _burgerCraftPos;

    [Header("Sprites")]
    [SerializeField] private Sprite _breadBottomSprite;
    [SerializeField] private Sprite _breadTopSprite;
    [SerializeField] private Sprite _rawPattySprite;
    [SerializeField] private Sprite _cookedPattySprite;
    [SerializeField] private Sprite _lettuceSprite;
    [SerializeField] private Sprite _tomatoSprite;
    [SerializeField] private Sprite _sauce1Sprite;
    [SerializeField] private Sprite _sauce2Sprite;

    [Header("Stack Settings")]
    [SerializeField] private float _burgerStackOffset = 20f;
    [SerializeField] private float _burgerStackAnimationDuration = 0.15f;

    [Header("Trash")]
    [SerializeField] private UI_TrashDropZone _trashZone;
    
    [Header("Fail Popup")]
    [SerializeField] private GameObject _failPopupPrefab;
    
    [Header("Complete Popup")]
    [SerializeField] private GameObject _cookingCompletePrefab;
    
    // 선택/상태
    private readonly List<UI_CookingReceipt> _activeReceipts = new List<UI_CookingReceipt>();
    private UI_CookingReceipt _currentReceipt;
    private Define.BurgerRecipe _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();
    private UI_CookingFailPopup _currentFailPopup;
    private bool _isMaxFailReached = false; // 3회 실패로 인한 종료인지 확인
    
    // 주문 큐 (Grill에서 받은 모든 주문을 저장)
    private readonly Queue<Define.BurgerRecipe> _orderQueue = new Queue<Define.BurgerRecipe>();

    private UI_BurgerStack _currentBurgerStack;
    private readonly List<GameObject> _assembledBurgerParts = new List<GameObject>();
    private bool _hasBottomBread;
    private bool _hasTopBread;

    // 그릴
    private class GrillPattyData
    {
        public GameObject pattyObject;
        public bool isCooked;
        public Coroutine grillRoutine;
        public Button button;
    }
    
    private List<GrillPattyData> _grillPatties = new List<GrillPattyData>();
    private const int MAX_GRILL_PATTY_COUNT = 2;

    private void Awake()
    {
        InitializeButtons();
    }

    private void OnEnable()
    {
        ResetCurrentBurger();
        ResetFailPopupState();
    }

    private void OnDisable()
    {
        // 3회 실패로 인한 종료가 아닐 때만 처리되지 않은 주문을 Grill에 다시 반환
        // _orderQueue는 사용하지 않음 - _deliveredOrderQueue는 Grill에서 직접 관리
        // 팝업이 비활성화될 때 정리 작업
        // 주의: PoolManager에 반환하는 것은 호출하는 쪽에서 처리
    }

    /// <summary>
    /// Grill에서 주문 목록을 받아 _deliveredOrderQueue의 데이터를 직접 사용하여 영수증 생성
    /// </summary>
    public void AddOrders(List<Define.BurgerRecipe> orders)
    {
        if (orders == null || orders.Count == 0)
            return;
        
        // _deliveredOrderQueue의 데이터를 직접 사용하여 영수증 생성 (최대 3개까지)
        // _orderQueue는 사용하지 않음 - _deliveredOrderQueue만 사용
        RefreshReceiptsFromDeliveredQueue(orders);
    }
    
    /// <summary>
    /// _deliveredOrderQueue의 데이터를 직접 사용하여 영수증 생성
    /// 실제 _deliveredOrderQueue에 있는 데이터만 표시하고, 없는 영수증은 제거
    /// </summary>
    private void RefreshReceiptsFromDeliveredQueue(List<Define.BurgerRecipe> deliveredOrders)
    {
        // 1단계: _deliveredOrderQueue에 없는 영수증 제거
        var receiptsToRemove = new List<UI_CookingReceipt>();
        foreach (var receipt in _activeReceipts)
        {
            if (receipt == null)
            {
                receiptsToRemove.Add(receipt);
                continue;
            }
            
            // deliveredOrders에 해당 영수증의 레시피가 있는지 확인
            bool found = false;
            if (deliveredOrders != null && deliveredOrders.Count > 0)
            {
                foreach (var order in deliveredOrders)
                {
                    if (UI_OrderSystem.IsMatch(receipt.Recipe, order))
                    {
                        found = true;
                        break;
                    }
                }
            }
            
            // _deliveredOrderQueue에 없으면 제거 대상
            if (!found)
            {
                receiptsToRemove.Add(receipt);
            }
        }
        
        // 제거 대상 영수증 삭제
        foreach (var receipt in receiptsToRemove)
        {
            _activeReceipts.Remove(receipt);
            if (_currentReceipt == receipt)
            {
                _currentReceipt = null;
            }
            if (receipt != null)
            {
                Destroy(receipt.gameObject);
            }
        }
        
        // 2단계: _deliveredOrderQueue에 있지만 아직 표시되지 않은 주문 추가 (최대 3개까지)
        if (deliveredOrders == null || deliveredOrders.Count == 0)
            return;
        
        // 각 주문에 대해 이미 표시된 개수를 세어서 추가할지 결정
        foreach (var order in deliveredOrders)
        {
            if (_activeReceipts.Count >= 3)
                break;
            
            if (order.Bread == Define.EBreadType.None)
                continue;
            
            // deliveredOrders에서 같은 레시피의 개수 세기
            int orderCountInDelivered = 0;
            foreach (var o in deliveredOrders)
            {
                if (UI_OrderSystem.IsMatch(o, order))
                {
                    orderCountInDelivered++;
                }
            }
            
            // 이미 표시된 영수증에서 같은 레시피의 개수 세기
            int displayedCount = 0;
            foreach (var receipt in _activeReceipts)
            {
                if (receipt != null && UI_OrderSystem.IsMatch(receipt.Recipe, order))
                {
                    displayedCount++;
                }
            }
            
            // 표시된 개수가 deliveredOrders의 개수보다 적으면 추가
            if (displayedCount < orderCountInDelivered)
            {
                AddReceipt(order);
            }
        }
    }
    
    /// <summary>
    /// 주문 큐에서 최대 3개까지 영수증을 표시합니다. (_deliveredOrderQueue 사용)
    /// </summary>
    private void RefreshReceipts()
    {
        // _orderQueue는 사용하지 않음 - _deliveredOrderQueue만 사용
        // Grill에서 현재 _deliveredOrderQueue 상태를 가져와서 영수증 생성
        Grill grill = FindObjectOfType<Grill>();
        if (grill != null)
        {
            List<Define.BurgerRecipe> currentDeliveredOrders = grill.GetOrders();
            if (currentDeliveredOrders != null && currentDeliveredOrders.Count > 0)
            {
                RefreshReceiptsFromDeliveredQueue(currentDeliveredOrders);
            }
        }
    }
    
    /// <summary>
    /// 단일 영수증을 추가합니다.
    /// </summary>
    private void AddReceipt(Define.BurgerRecipe recipe)
    {
        if (_receiptPrefab == null || _receiptParent == null)
        {
            Debug.LogWarning("Receipt prefab/parent가 설정되어 있지 않습니다.");
            return;
        }

        if (_activeReceipts.Count >= 3)
        {
            return;
        }

        // Grill에서 주문 번호 가져오기
        Grill grill = FindObjectOfType<Grill>();
        string orderNumber = null;
        if (grill != null)
        {
            orderNumber = grill.GetOrderNumberByRecipe(recipe);
        }

        UI_CookingReceipt receipt = Instantiate(_receiptPrefab, _receiptParent);
        receipt.Init(recipe, orderNumber);
        
        // 영수증 클릭 이벤트 등록
        receipt.OnReceiptClicked = OnReceiptClicked;
        
        _activeReceipts.Add(receipt);

        if (_currentReceipt == null)
        {
            SelectReceipt(receipt);
        }
    }
    
    /// <summary>
    /// 단일 주문 추가 (하위 호환성)
    /// </summary>
    public void AddOrder(Define.BurgerRecipe recipe)
    {
        if (recipe.Bread == Define.EBreadType.None)
            return;
        
        // _orderQueue는 사용하지 않음 - Grill에 직접 추가
        Grill grill = FindObjectOfType<Grill>();
        if (grill != null)
        {
            grill.AddOrder(recipe);
            // Grill에서 _deliveredOrderQueue 상태를 가져와서 영수증 생성
            List<Define.BurgerRecipe> currentDeliveredOrders = grill.GetOrders();
            if (currentDeliveredOrders != null && currentDeliveredOrders.Count > 0)
            {
                RefreshReceiptsFromDeliveredQueue(currentDeliveredOrders);
            }
        }
    }
    
    /// <summary>
    /// 영수증 클릭 시 호출되는 콜백
    /// </summary>
    private void OnReceiptClicked(UI_CookingReceipt receipt)
    {
        if (receipt != null && _activeReceipts.Contains(receipt))
        {
            SelectReceipt(receipt);
        }
    }

    private void SelectReceipt(UI_CookingReceipt receipt)
    {
        // null 체크
        if (receipt == null)
            return;
        
        // 이미 선택된 receipt면 무시
        if (_currentReceipt == receipt)
            return;
        
        // 모든 receipt의 선택 상태 해제 (현재 선택된 것 포함)
        foreach (var r in _activeReceipts)
        {
            if (r != null)
            {
                r.SetSelected(false);
            }
        }
        
        // 선택된 receipt 강조
        _currentReceipt = receipt;
        _currentReceipt.SetSelected(true);
        
        _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();
        ResetCurrentBurger();
    }

    private void InitializeButtons()
    {
        AddListener(_breadButton, () => OnIngredientClicked(EIngredientType.Bread));
        AddListener(_pattyButton, () => HandlePattyClick());
        AddListener(_lettuceButton, () => OnIngredientClicked(EIngredientType.Lettuce));
        AddListener(_tomatoButton, () => OnIngredientClicked(EIngredientType.Tomato));
        AddListener(_sauce1Button, () => OnIngredientClicked(EIngredientType.Sauce1));
        AddListener(_sauce2Button, () => OnIngredientClicked(EIngredientType.Sauce2));
        AddListener(_exitButton, () => OnEnterExitButton());
    }

    private void AddListener(Button btn, Action action)
    {
        if (btn == null)
        {
            Debug.LogWarning("버튼 참조가 없습니다.");
            return;
        }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action?.Invoke());
    }

    #region Ingredient Handling

    private void OnIngredientClicked(EIngredientType ingredient)
    {
        // 빵이 아니고 하단 빵이 없으면 다른 재료 추가 불가
        if (ingredient != EIngredientType.Bread && !_hasBottomBread)
        {
            Debug.LogWarning("빵을 먼저 추가해주세요.");
            return;
        }

        switch (ingredient)
        {
            case EIngredientType.Bread:
                AddBread();
                break;
            case EIngredientType.Lettuce:
                if (TryAddIngredientWithLimit(EIngredientType.Lettuce))
                    AddIngredientToAssembly(EIngredientType.Lettuce);
                break;
            case EIngredientType.Tomato:
                if (TryAddIngredientWithLimit(EIngredientType.Tomato))
                    AddIngredientToAssembly(EIngredientType.Tomato);
                break;
            case EIngredientType.Sauce1:
                if (TryAddIngredientWithLimit(EIngredientType.Sauce1))
                    AddIngredientToAssembly(EIngredientType.Sauce1);
                break;
            case EIngredientType.Sauce2:
                if (TryAddIngredientWithLimit(EIngredientType.Sauce2))
                    AddIngredientToAssembly(EIngredientType.Sauce2);
                break;
        }
    }

    private void AddBread()
    {
        if (!_hasBottomBread)
        {
            _hasBottomBread = true;
            _currentRecipe.Bread = Define.EBreadType.Plain;
            AddIngredientToAssembly(EIngredientType.Bread, isTopBread: false);
        }
        else if (!_hasTopBread)
        {
            _hasTopBread = true;
            AddIngredientToAssembly(EIngredientType.Bread, isTopBread: true);
            CheckBurgerComplete();
        }
        else
        {
            Debug.LogWarning("빵은 상/하단 각각 한 번씩만 추가 가능합니다.");
        }
    }

    private void OnEnterExitButton()
    {
        gameObject.SetActive(false);
    }

    private bool TryAddIngredientWithLimit(EIngredientType ingredient)
    {
        // 현재 선택된 receipt가 없으면 추가 불가
        if (_currentReceipt == null)
        {
            Debug.LogWarning("선택된 주문이 없습니다.");
            return false;
        }

        Define.BurgerRecipe requestedRecipe = _currentReceipt.Recipe;

        switch (ingredient)
        {
            case EIngredientType.Patty:
                // 최대 개수만 체크 (레시피 개수 제한 해제)
                if (_currentRecipe.PattyCount >= Define.ORDER_MAX_PATTY_COUNT)
                {
                    Debug.LogWarning($"패티는 최대 {Define.ORDER_MAX_PATTY_COUNT}개까지만 추가 가능합니다.");
                    return false;
                }

                _currentRecipe.Patty = requestedRecipe.Patty;
                _currentRecipe.PattyCount++;
                return true;

            case EIngredientType.Lettuce:
            case EIngredientType.Tomato:
                if (_currentRecipe.Veggies == null)
                    _currentRecipe.Veggies = new List<Define.EVeggieType>();

                Define.EVeggieType veggieType = ingredient == EIngredientType.Lettuce 
                    ? Define.EVeggieType.Lettuce 
                    : Define.EVeggieType.Tomato;

                // 최대 개수만 체크 (레시피 개수 제한 해제)
                if (_currentRecipe.Veggies.Count >= Define.ORDER_MAX_VEGGIES_TOTAL)
                {
                    Debug.LogWarning($"야채는 최대 {Define.ORDER_MAX_VEGGIES_TOTAL}개까지만 추가 가능합니다.");
                    return false;
                }

                _currentRecipe.Veggies.Add(veggieType);
                return true;

            case EIngredientType.Sauce1:
                // 최대 개수만 체크 (레시피 개수 제한 해제)
                if (_currentRecipe.Sauce1Count >= Define.ORDER_MAX_SAUCE1_COUNT)
                {
                    Debug.LogWarning($"소스1은 최대 {Define.ORDER_MAX_SAUCE1_COUNT}개까지만 추가 가능합니다.");
                    return false;
                }
                _currentRecipe.Sauce1Count++;
                return true;

            case EIngredientType.Sauce2:
                // 최대 개수만 체크 (레시피 개수 제한 해제)
                if (_currentRecipe.Sauce2Count >= Define.ORDER_MAX_SAUCE2_COUNT)
                {
                    Debug.LogWarning($"소스2는 최대 {Define.ORDER_MAX_SAUCE2_COUNT}개까지만 추가 가능합니다.");
                    return false;
                }
                _currentRecipe.Sauce2Count++;
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Patty / Grill

    private void HandlePattyClick()
    {
        // 하단 빵이 없으면 패티 추가 불가
        if (!_hasBottomBread)
        {
            Debug.LogWarning("빵을 먼저 추가해주세요.");
            return;
        }

        // 최대 개수 체크
        if (_grillPatties.Count >= MAX_GRILL_PATTY_COUNT)
        {
            Debug.LogWarning($"패티는 최대 {MAX_GRILL_PATTY_COUNT}장까지만 동시에 굽을 수 있습니다.");
            return;
        }

        // 굽기 시작
        StartGrilling();
    }

    private void StartGrilling()
    {
        // 패티 데이터 생성
        GrillPattyData pattyData = new GrillPattyData
        {
            pattyObject = CreatePattyVisual(_rawPattySprite, _flamePattyPos),
            isCooked = false,
            grillRoutine = null
        };
        
        if (pattyData.pattyObject == null)
            return;
        
        // 패티 위치 조정 (여러 개일 때 겹치지 않도록)
        RectTransform rt = pattyData.pattyObject.GetComponent<RectTransform>();
        int pattyIndex = _grillPatties.Count;
        float offsetX = (pattyIndex - (_grillPatties.Count > 0 ? 0.5f : 0f)) * 60f; // 패티 간 간격
        rt.anchoredPosition = new Vector2(offsetX, 0);
        
        // 버튼 컴포넌트 추가 (클릭 가능하게)
        Button button = pattyData.pattyObject.AddComponent<Button>();
        button.onClick.AddListener(() => OnGrillPattyClicked(pattyData));
        pattyData.button = button;
        
        _grillPatties.Add(pattyData);
        
        // 굽기 시작
        pattyData.grillRoutine = StartCoroutine(CoGrill(pattyData));
    }

    private IEnumerator CoGrill(GrillPattyData pattyData)
    {
        yield return new WaitForSeconds(5f);
        
        if (pattyData == null || pattyData.pattyObject == null)
            yield break;
        
        pattyData.isCooked = true;
        
        // 완료된 패티 스프라이트로 변경
        Image img = pattyData.pattyObject.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = _cookedPattySprite;
        }
        
        pattyData.grillRoutine = null;
    }

    /// <summary>
    /// 그릴에 있는 패티를 클릭했을 때 호출
    /// </summary>
    private void OnGrillPattyClicked(GrillPattyData pattyData)
    {
        if (pattyData == null || !pattyData.isCooked)
            return;
        
        MovePattyFromGrillToAssembly(pattyData);
    }

    private void MovePattyFromGrillToAssembly(GrillPattyData pattyData)
    {
        if (pattyData == null || !pattyData.isCooked)
            return;

        // 패티 오브젝트 제거
        if (pattyData.pattyObject != null)
        {
            Destroy(pattyData.pattyObject);
        }
        
        // 리스트에서 제거
        _grillPatties.Remove(pattyData);
        
        // 버거 스택에 추가
        if (TryAddIngredientWithLimit(EIngredientType.Patty))
        {
            AddIngredientToAssembly(EIngredientType.Patty);
            CheckBurgerComplete();
        }
        
        // 남은 패티 위치 재조정
        RefreshGrillPattyPositions();
    }

    /// <summary>
    /// 그릴에 있는 패티들의 위치를 재조정
    /// </summary>
    private void RefreshGrillPattyPositions()
    {
        for (int i = 0; i < _grillPatties.Count; i++)
        {
            if (_grillPatties[i].pattyObject == null)
                continue;
            
            RectTransform rt = _grillPatties[i].pattyObject.GetComponent<RectTransform>();
            if (rt != null)
            {
                float offsetX = (i - (_grillPatties.Count > 1 ? 0.5f : 0f)) * 60f;
                rt.anchoredPosition = new Vector2(offsetX, 0);
            }
        }
    }

    private GameObject CreatePattyVisual(Sprite sprite, Transform parent)
    {
        if (sprite == null || parent == null)
            return null;

        GameObject go = new GameObject("Patty", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.SetNativeSize();
        
        return go;
    }

    #endregion

    #region Assembly

    private void AddIngredientToAssembly(EIngredientType ingredientType, bool isTopBread = false)
    {
        if (_currentBurgerStack == null)
            CreateBurgerStack();

        if (_currentBurgerStack == null)
        {
            Debug.LogWarning("버거 스택 생성 실패");
            return;
        }

        GameObject ingredientObj = CreateIngredientVisual(ingredientType, isTopBread);
        if (ingredientObj == null)
            return;

        ingredientObj.transform.SetParent(_currentBurgerStack.transform, false);

        if (ingredientType == EIngredientType.Bread)
        {
            if (isTopBread)
                _assembledBurgerParts.Add(ingredientObj);
            else
                _assembledBurgerParts.Insert(0, ingredientObj);
        }
        else
        {
            _assembledBurgerParts.Add(ingredientObj);
        }

        RefreshBurgerStackPositions();
    }

    private GameObject CreateIngredientVisual(EIngredientType ingredient, bool isTopBread)
    {
        Sprite sprite = GetSpriteForIngredient(ingredient, isTopBread);
        if (sprite == null)
        {
            Debug.LogWarning($"{ingredient} 스프라이트가 없습니다.");
            return null;
        }

        GameObject ingredientObj = new GameObject(ingredient.ToString(), typeof(RectTransform), typeof(Image));
        Image img = ingredientObj.GetComponent<Image>();
        img.sprite = sprite;
        img.SetNativeSize();

        UI_IngredientTag tag = ingredientObj.AddComponent<UI_IngredientTag>();
        tag.Type = ingredient;
        tag.IsTopBread = isTopBread;

        UI_IngredientDragHandler dragHandler = ingredientObj.AddComponent<UI_IngredientDragHandler>();
        dragHandler.SetParentStack(_currentBurgerStack);
        
        return ingredientObj;
    }

    private Sprite GetSpriteForIngredient(EIngredientType ingredient, bool isTopBread)
    {
        switch (ingredient)
        {
            case EIngredientType.Bread:
                return isTopBread ? _breadTopSprite : _breadBottomSprite;
            case EIngredientType.Patty:
                return _cookedPattySprite;
            case EIngredientType.Lettuce:
                return _lettuceSprite;
            case EIngredientType.Tomato:
                return _tomatoSprite;
            case EIngredientType.Sauce1:
                return _sauce1Sprite;
            case EIngredientType.Sauce2:
                return _sauce2Sprite;
            default:
                return null;
        }
    }

    private void RefreshBurgerStackPositions()
    {
        for (int i = 0; i < _assembledBurgerParts.Count; i++)
        {
            GameObject part = _assembledBurgerParts[i];
            if (part == null) continue;

            Vector3 target = new Vector3(0, i * _burgerStackOffset, 0);
            part.transform.DOLocalMove(target, _burgerStackAnimationDuration).SetEase(Ease.OutQuad);
        }
    }

    private void CreateBurgerStack()
    {
        if (_burgerCraftPos == null)
        {
            Debug.LogWarning("버거 스택 생성 실패: 조립 위치가 없습니다.");
            return;
        }

        GameObject stackObj = new GameObject("BurgerStack", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UI_BurgerStack));
        RectTransform rt = stackObj.GetComponent<RectTransform>();
        rt.SetParent(_burgerCraftPos, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 400);
        
        Image image = stackObj.GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
        image.raycastTarget = true;
        
        CanvasGroup canvasGroup = stackObj.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        
        _currentBurgerStack = stackObj.GetComponent<UI_BurgerStack>();
        _currentBurgerStack.OnTrashDropped += OnBurgerTrashed;
    }

    public void OnBurgerTrashed(UI_BurgerStack stack)
    {
        if (stack != null)
        {
            Destroy(stack.gameObject);
            _currentBurgerStack = null;
        }
        
        ResetCurrentBurger();
    }

    #endregion

    #region Reset & Complete

    private void ResetCurrentBurger()
    {
        _hasBottomBread = false;
        _hasTopBread = false;

        ClearGrillArea();

        _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();

        ClearAssemblyArea();
    }

    private void ClearAssemblyArea()
    {
        foreach (var part in _assembledBurgerParts)
        {
            if (part != null)
                Destroy(part);
        }
        _assembledBurgerParts.Clear();

        if (_currentBurgerStack != null)
        {
            _currentBurgerStack.ClearStack();
            _currentBurgerStack.transform.localPosition = Vector3.zero;
        }
    }

    private void ClearGrillArea()
    {
        // 모든 패티 제거
        foreach (var pattyData in _grillPatties)
        {
            if (pattyData.grillRoutine != null)
            {
                StopCoroutine(pattyData.grillRoutine);
            }
            
            if (pattyData.pattyObject != null)
            {
                Destroy(pattyData.pattyObject);
            }
        }
        
        _grillPatties.Clear();
    }

    private void CheckBurgerComplete()
    {
        if (_currentReceipt == null)
            return;

        RecalculateRecipeFromStack();

        if (!_hasBottomBread || !_hasTopBread || _assembledBurgerParts.Count == 0)
            return;

        bool match = UI_OrderSystem.IsMatch(_currentRecipe, _currentReceipt.Recipe);
        
        if (match)
        {
            SpawnBurgerAndComplete();
        }
        else
        {
            ShowFailPopup();
        }
    }

    private void RecalculateRecipeFromStack()
    {
        var recipe = UI_OrderSystem.CreateEmptyRecipe();
        bool hasBottom = false;
        bool hasTop = false;

        foreach (var part in _assembledBurgerParts)
        {
            if (part == null) continue;
            var tag = part.GetComponent<UI_IngredientTag>();
            if (tag == null) continue;

            switch (tag.Type)
            {
                case EIngredientType.Bread:
                    recipe.Bread = Define.EBreadType.Plain;
                    if (tag.IsTopBread) hasTop = true;
                    else hasBottom = true;
                    break;
                case EIngredientType.Patty:
                    if (recipe.PattyCount < Define.ORDER_MAX_PATTY_COUNT)
                    {
                        recipe.Patty = Define.EPattyType.Beef;
                        recipe.PattyCount++;
                    }
                    break;
                case EIngredientType.Lettuce:
                    if (recipe.Veggies == null) recipe.Veggies = new List<Define.EVeggieType>();
                    if (recipe.Veggies.Count < Define.ORDER_MAX_VEGGIES_TOTAL)
                        recipe.Veggies.Add(Define.EVeggieType.Lettuce);
                    break;
                case EIngredientType.Tomato:
                    if (recipe.Veggies == null) recipe.Veggies = new List<Define.EVeggieType>();
                    if (recipe.Veggies.Count < Define.ORDER_MAX_VEGGIES_TOTAL)
                        recipe.Veggies.Add(Define.EVeggieType.Tomato);
                    break;
                case EIngredientType.Sauce1:
                    if (recipe.Sauce1Count < Define.ORDER_MAX_SAUCE1_COUNT)
                        recipe.Sauce1Count++;
                    break;
                case EIngredientType.Sauce2:
                    if (recipe.Sauce2Count < Define.ORDER_MAX_SAUCE2_COUNT)
                        recipe.Sauce2Count++;
                    break;
            }
        }

        _hasBottomBread = hasBottom;
        _hasTopBread = hasTop;
        _currentRecipe = recipe;
    }

    private void ResetFailPopupState()
    {
        // 실패 팝업 정리
        if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
        {
            // 이미 비활성화되어 있으면 풀에 이미 반환된 상태
            if (_currentFailPopup.gameObject.activeSelf)
            {
                _currentFailPopup.ResetFailCount();
                _currentFailPopup.Hide();
            }
            _currentFailPopup = null;
        }
    }

    private void SpawnBurgerAndComplete()
    {
        // 원래대로 Grill의 BurgerPile에 버거 생성
        Grill grill = FindObjectOfType<Grill>();
        if (grill != null)
        {
            grill.StopSpawnBurger = true;
            
            BurgerPile burgerPile = grill.GetComponentInChildren<BurgerPile>();
            if (burgerPile != null && burgerPile.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
            {
                // 버거 생성
                GameObject burger = burgerPile.SpawnObjectWithOrderNumber();
                
                // 주문 번호 부여
                if (burger != null && _currentReceipt != null)
                {
                    string orderNumber = _currentReceipt.OrderNumber;
                    BurgerOrderNumber orderNumberComponent = burger.GetComponent<BurgerOrderNumber>();
                    if (orderNumberComponent == null)
                    {
                        orderNumberComponent = burger.AddComponent<BurgerOrderNumber>();
                    }
                    orderNumberComponent.OrderNumber = orderNumber;
                }
            }
            
            // 완료된 주문을 Grill의 큐에서 제거
            if (_currentReceipt != null)
            {
                grill.RemoveOrder(_currentReceipt.Recipe);
            }
        }

        // 완료된 영수증 제거
        Define.BurgerRecipe completedRecipe = UI_OrderSystem.CreateEmptyRecipe();
        if (_currentReceipt != null)
        {
            completedRecipe = _currentReceipt.Recipe;
            _activeReceipts.Remove(_currentReceipt);
            Destroy(_currentReceipt.gameObject);
            _currentReceipt = null;
        }

        // 완료된 주문은 이미 Grill.RemoveOrder()에서 _deliveredOrderQueue에서 제거됨
        // _orderQueue는 사용하지 않으므로 제거 로직 불필요

        // 다음 주문을 _deliveredOrderQueue에서 가져와서 영수증 리프레시
        // Grill에서 현재 _deliveredOrderQueue 상태를 가져와서 영수증 생성
        Grill grillForRefresh = FindObjectOfType<Grill>();
        if (grillForRefresh != null)
        {
            List<Define.BurgerRecipe> currentDeliveredOrders = grillForRefresh.GetOrders();
            
            // 모든 주문이 완료되었는지 확인 (_deliveredOrderQueue가 0개)
            if (currentDeliveredOrders == null || currentDeliveredOrders.Count == 0)
            {
                // 모든 버거 조리 완료 - CookingComplete 팝업 표시
                ShowCookingCompletePopup();
            }
            else
            {
                // 아직 남은 주문이 있으면 영수증 리프레시
                RefreshReceiptsFromDeliveredQueue(currentDeliveredOrders);
            }
        }

        // 다음 영수증 선택 (있는 경우)
        if (_activeReceipts.Count > 0)
        {
            SelectReceipt(_activeReceipts[0]);
        }

        ResetCurrentBurger();

        if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
        {
            // Hide()를 통해 풀에 반환 (중복 방지)
            if (_currentFailPopup.gameObject.activeSelf)
            {
                _currentFailPopup.Hide();
            }
            _currentFailPopup = null;
        }
    }

    private void ShowFailPopup()
    {
        // UI_CookingPopup 자체가 비활성화되어 있으면 아무것도 하지 않음
        if (gameObject == null || !gameObject.activeSelf)
        {
            return;
        }
        
        // 팝업이 이미 비활성화되어 있으면 새로 생성
        if (_currentFailPopup != null)
        {
            // gameObject가 null이거나 비활성화되어 있으면 null로 설정하고 새로 생성
            if (_currentFailPopup.gameObject == null || !_currentFailPopup.gameObject.activeSelf)
            {
                _currentFailPopup = null;
            }
            else
            {
                // 유효한 팝업이 있으면 재사용
                try
                {
                    // 손님 정보 설정 (첫 번째 픽업 큐 손님)
                    Counter counter = FindObjectOfType<Counter>();
                    if (counter != null)
                    {
                        GuestController firstGuest = counter.GetFirstPickupQueueGuest();
                        if (firstGuest != null)
                        {
                            _currentFailPopup.SetAssociatedGuest(firstGuest);
                        }
                    }
                    
                    _currentFailPopup.AddFailCount();
                    _currentFailPopup.Show();
                    return;
                }
                catch (System.NullReferenceException)
                {
                    // 예외 발생 시 null로 설정하고 새로 생성
                    _currentFailPopup = null;
                }
            }
        }

        // 팝업이 없거나 비활성화되어 있으면 새로 생성
        GameObject prefab = _failPopupPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/UI/Popup/UI_CookingFailPopup");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("@Resources/Prefabs/UI/Popup/UI_CookingFailPopup");
            }
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("실패 팝업 프리팹을 찾을 수 없습니다.");
            return;
        }
        
        // PoolManager가 null이면 생성 불가
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager.Instance가 null입니다.");
            return;
        }
        
        GameObject popupObj = PoolManager.Instance.Pop(prefab);
        if (popupObj == null)
        {
            Debug.LogWarning("PoolManager.Pop()이 null을 반환했습니다.");
            return;
        }
        
        Transform popupPool = PoolManager.Instance.GetPopupPool();
        if (popupPool != null)
        {
            popupObj.transform.SetParent(popupPool, false);
        }
        
        _currentFailPopup = popupObj.GetComponent<UI_CookingFailPopup>();
        if (_currentFailPopup == null)
        {
            PoolManager.Instance.Push(popupObj);
            return;
        }
        
        try
        {
            // 손님 정보 설정 (첫 번째 픽업 큐 손님)
            Counter counter = FindObjectOfType<Counter>();
            if (counter != null)
            {
                GuestController firstGuest = counter.GetFirstPickupQueueGuest();
                if (firstGuest != null)
                {
                    _currentFailPopup.SetAssociatedGuest(firstGuest);
                }
            }
            
            _currentFailPopup.OnNextButtonClicked = () =>
            {
                if (_currentFailPopup != null && _currentFailPopup.gameObject != null && _currentFailPopup.gameObject.activeSelf)
                {
                    _currentFailPopup.Hide();
                }
            };

            _currentFailPopup.OnMaxFailReached = () =>
            {
                HandleMaxFailReached();
            };
            
            // Show() 먼저 호출하여 손님의 FailCount를 가져온 후, AddFailCount() 호출
            _currentFailPopup.Show();
            _currentFailPopup.AddFailCount();
        }
        catch (System.NullReferenceException)
        {
            // 예외 발생 시 정리
            if (popupObj != null)
            {
                PoolManager.Instance.Push(popupObj);
            }
            _currentFailPopup = null;
        }
    }

    /// <summary>
    /// 3회 실패 시 처리: 실패한 영수증의 주문 번호에 해당하는 손님을 leavepos로 보내고, _deliveredOrderQueue에서 주문 삭제
    /// </summary>
    private void HandleMaxFailReached()
    {
        // Counter 가져오기
        Counter counter = FindObjectOfType<Counter>();
        if (counter == null)
        {
            Debug.LogWarning("[UI_CookingPopup] Counter를 찾을 수 없습니다.");
            return;
        }

        // 실패한 영수증의 주문 번호 가져오기
        string failedOrderNumber = null;
        if (_currentReceipt != null)
        {
            failedOrderNumber = _currentReceipt.OrderNumber;
        }
        
        // 주문 번호로 해당 손님 찾기
        GuestController failedGuest = null;
        if (!string.IsNullOrEmpty(failedOrderNumber))
        {
            failedGuest = counter.GetGuestByOrderNumber(failedOrderNumber);
        }
        
        // 주문 번호로 손님을 찾지 못했으면 첫 번째 픽업 큐 손님으로 폴백 (하위 호환성)
        if (failedGuest == null)
        {
            failedGuest = counter.GetFirstPickupQueueGuest();
            if (failedGuest == null)
            {
                Debug.LogWarning("[UI_CookingPopup] 실패한 주문 번호에 해당하는 손님을 찾을 수 없습니다.");
                return;
            }
        }
        
        // 주문 번호가 있으면 주문 번호로 삭제, 없으면 게스트 ID로 삭제
        Grill grill = FindObjectOfType<Grill>();
        if (grill != null)
        {
            if (!string.IsNullOrEmpty(failedOrderNumber))
            {
                // 주문 번호를 기준으로 주문 삭제
                grill.RemoveOrdersByOrderNumber(failedOrderNumber);
            }
            else
            {
                // 게스트 ID를 기반으로 주문 삭제 (폴백)
                int orderCount = counter.GetGuestOrderCount(failedGuest);
                int guestId = failedGuest.GetInstanceID();
                if (orderCount > 0)
                {
                    grill.RemoveGuestOrders(guestId, orderCount);
                }
            }
        }

        // 실패한 영수증 제거 (3회 실패한 영수증)
        if (_currentReceipt != null)
        {
            _activeReceipts.Remove(_currentReceipt);
            Destroy(_currentReceipt.gameObject);
            _currentReceipt = null;
        }

        // 실패한 주문 번호에 해당하는 손님을 leavepos로 보내기
        counter.ProcessOrderComplete(failedGuest, true);

        // CookingFail 팝업 완전히 초기화 및 정리 (fail count 리셋)
        if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
        {
            if (_currentFailPopup.gameObject.activeSelf)
            {
                _currentFailPopup.ResetFailCount(); // fail count 리셋
                _currentFailPopup.Hide();
            }
            _currentFailPopup = null;
        }

        // 영수증 리프레시 (삭제된 주문을 제외한 나머지 주문만 표시)
        RefreshReceiptsAfterFail();

        // 다음 영수증 선택 (있는 경우)
        if (_activeReceipts.Count > 0)
        {
            SelectReceipt(_activeReceipts[0]);
            ResetCurrentBurger();
        }
        else
        {
            // 영수증이 없으면 버거 리셋만
            ResetCurrentBurger();
        }

        // 3회 실패 플래그 설정 (OnDisable에서 ReturnOrders를 호출하지 않도록)
        _isMaxFailReached = true;

        // 팝업 정리
        gameObject.SetActive(false);
        PoolManager.Instance.Push(gameObject);
    }
    
    /// <summary>
    /// 실패 후 영수증 리프레시 (삭제된 주문을 제외한 나머지 주문만 표시)
    /// </summary>
    private void RefreshReceiptsAfterFail()
    {
        // Grill에서 현재 _deliveredOrderQueue 상태를 가져와서 영수증 재생성
        Grill grill = FindObjectOfType<Grill>();
        if (grill == null)
            return;
        
        List<Define.BurgerRecipe> currentDeliveredOrders = grill.GetOrders();
        if (currentDeliveredOrders == null || currentDeliveredOrders.Count == 0)
        {
            // 주문이 없으면 모든 영수증 제거
            foreach (var receipt in _activeReceipts)
            {
                if (receipt != null)
                {
                    Destroy(receipt.gameObject);
                }
            }
            _activeReceipts.Clear();
            _currentReceipt = null;
            return;
        }
        
        // 현재 표시된 영수증 중에서 _deliveredOrderQueue에 없는 것 제거
        var receiptsToRemove = new List<UI_CookingReceipt>();
        foreach (var receipt in _activeReceipts)
        {
            if (receipt == null)
                continue;
                
            bool found = false;
            foreach (var order in currentDeliveredOrders)
            {
                if (UI_OrderSystem.IsMatch(receipt.Recipe, order))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                receiptsToRemove.Add(receipt);
            }
        }
        
        // 찾지 못한 영수증 제거
        foreach (var receipt in receiptsToRemove)
        {
            _activeReceipts.Remove(receipt);
            if (_currentReceipt == receipt)
            {
                _currentReceipt = null;
            }
            if (receipt != null)
            {
                Destroy(receipt.gameObject);
            }
        }
        
        // _deliveredOrderQueue에 있지만 아직 표시되지 않은 주문 추가 (최대 3개까지)
        // 각 주문에 대해 이미 표시된 개수를 세어서 추가할지 결정
        foreach (var order in currentDeliveredOrders)
        {
            if (_activeReceipts.Count >= 3)
                break;
                
            if (order.Bread == Define.EBreadType.None)
                continue;
            
            // currentDeliveredOrders에서 같은 레시피의 개수 세기
            int orderCountInDelivered = 0;
            foreach (var o in currentDeliveredOrders)
            {
                if (UI_OrderSystem.IsMatch(o, order))
                {
                    orderCountInDelivered++;
                }
            }
            
            // 이미 표시된 영수증에서 같은 레시피의 개수 세기
            int displayedCount = 0;
            foreach (var receipt in _activeReceipts)
            {
                if (receipt != null && UI_OrderSystem.IsMatch(receipt.Recipe, order))
                {
                    displayedCount++;
                }
            }
            
            // 표시된 개수가 currentDeliveredOrders의 개수보다 적으면 추가
            if (displayedCount < orderCountInDelivered)
            {
                AddReceipt(order);
            }
        }
    }

    /// <summary>
    /// 모든 버거 조리가 완료되었을 때 CookingComplete 팝업을 표시합니다.
    /// </summary>
    private void ShowCookingCompletePopup()
    {
        // 프리팹 찾기
        GameObject prefab = _cookingCompletePrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/UI/Popup/UI_CookingComplete");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("@Resources/Prefabs/UI/Popup/UI_CookingComplete");
            }
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("CookingComplete 팝업 프리팹을 찾을 수 없습니다.");
            return;
        }
        
        // PoolManager가 null이면 생성 불가
        if (PoolManager.Instance == null)
        {
            Debug.LogWarning("PoolManager.Instance가 null입니다.");
            return;
        }
        
        // 프리팹 이름에 "Popup"이 없으면 임시로 변경하여 PopupPool에 들어가도록 함
        string originalName = prefab.name;
        bool nameChanged = false;
        if (!originalName.Contains("Popup"))
        {
            prefab.name = originalName + "Popup";
            nameChanged = true;
        }
        
        // PoolManager에서 팝업 가져오기
        GameObject popupObj = PoolManager.Instance.Pop(prefab);
        
        // 프리팹 이름 복원
        if (nameChanged)
        {
            prefab.name = originalName;
        }
        
        if (popupObj == null)
        {
            Debug.LogWarning("PoolManager.Pop()이 null을 반환했습니다.");
            return;
        }
        
        // PopupPool에 부모 설정 (확실하게 PopupPool에 들어가도록)
        Transform popupPool = PoolManager.Instance.GetPopupPool();
        if (popupPool != null)
        {
            popupObj.transform.SetParent(popupPool, false);
        }
        
        // Canvas의 sortOrder를 최상단으로 설정 (다른 팝업들보다 위에 표시)
        Canvas canvas = popupObj.GetComponent<Canvas>();
        if (canvas != null)
        {
            // PopupPool의 모든 Canvas 중 최대 sortOrder 찾기
            int maxSortOrder = 0;
            if (popupPool != null)
            {
                Canvas[] canvases = popupPool.GetComponentsInChildren<Canvas>(true);
                foreach (Canvas c in canvases)
                {
                    if (c != canvas && c.sortingOrder > maxSortOrder)
                    {
                        maxSortOrder = c.sortingOrder;
                    }
                }
            }
            canvas.sortingOrder = maxSortOrder + 1;
        }
        
        // UI_CookingComplete 컴포넌트 가져오기
        UI_CookingComplete completePopup = popupObj.GetComponent<UI_CookingComplete>();
        if (completePopup == null)
        {
            Debug.LogWarning("UI_CookingComplete 컴포넌트를 찾을 수 없습니다.");
            PoolManager.Instance.Push(popupObj);
            return;
        }
        
        // 팝업이 닫힐 때 CookingPopup도 닫기
        completePopup.OnCompletePopupClosed = () =>
        {
            // CookingPopup 비활성화 및 풀에 반환
            gameObject.SetActive(false);
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Push(gameObject);
            }
        };
        
        // 팝업 표시
        completePopup.Show();
    }

    #endregion

}

