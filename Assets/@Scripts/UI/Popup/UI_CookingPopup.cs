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
    
    // 선택/상태
    private readonly List<UI_CookingReceipt> _activeReceipts = new List<UI_CookingReceipt>();
    private UI_CookingReceipt _currentReceipt;
    private Define.BurgerRecipe _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();
    private UI_CookingFailPopup _currentFailPopup;
    private bool _resetFailOnOpen = false;
    
    // 주문 큐 (Grill에서 받은 모든 주문을 저장)
    private readonly Queue<Define.BurgerRecipe> _orderQueue = new Queue<Define.BurgerRecipe>();

    private UI_BurgerStack _currentBurgerStack;
    private readonly List<GameObject> _assembledBurgerParts = new List<GameObject>();
    private bool _hasBottomBread;
    private bool _hasTopBread;

    // 그릴
    private GameObject _grillPattyObject;
    private bool _isGrilling;
    private bool _pattyCooked;
    private Coroutine _grillRoutine;

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
        // 팝업이 비활성화될 때 정리 작업
        // 주의: PoolManager에 반환하는 것은 호출하는 쪽에서 처리
    }

    /// <summary>
    /// Grill에서 주문 목록을 받아 큐에 저장하고, 최대 3개까지 영수증 표시
    /// </summary>
    public void AddOrders(List<Define.BurgerRecipe> orders)
    {
        if (orders == null || orders.Count == 0)
            return;
        
        // 모든 주문을 큐에 추가
        foreach (var order in orders)
        {
            if (order.Bread != Define.EBreadType.None)
            {
                _orderQueue.Enqueue(order);
            }
        }
        
        // 영수증 리프레시 (최대 3개까지 표시)
        RefreshReceipts();
    }
    
    /// <summary>
    /// 주문 큐에서 최대 3개까지 영수증을 표시합니다.
    /// </summary>
    private void RefreshReceipts()
    {
        // 현재 표시된 영수증이 3개 미만이면 큐에서 가져와서 추가
        while (_activeReceipts.Count < 3 && _orderQueue.Count > 0)
        {
            Define.BurgerRecipe recipe = _orderQueue.Dequeue();
            AddReceipt(recipe);
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

        UI_CookingReceipt receipt = Instantiate(_receiptPrefab, _receiptParent);
        receipt.Init(recipe);
        
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
        
        _orderQueue.Enqueue(recipe);
        RefreshReceipts();
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
                // 선택된 receipt의 패티 개수까지만 추가 가능
                if (_currentRecipe.PattyCount >= requestedRecipe.PattyCount)
                {
                    Debug.LogWarning($"패티는 최대 {requestedRecipe.PattyCount}개까지만 추가 가능합니다.");
                    return false;
                }

                _currentRecipe.Patty = requestedRecipe.Patty;
                _currentRecipe.PattyCount++;
                return true;

            case EIngredientType.Lettuce:
            case EIngredientType.Tomato:
                if (_currentRecipe.Veggies == null)
                    _currentRecipe.Veggies = new List<Define.EVeggieType>();

                // 선택된 receipt의 야채 개수까지만 추가 가능
                int requestedVeggieCount = requestedRecipe.Veggies != null ? requestedRecipe.Veggies.Count : 0;
                if (_currentRecipe.Veggies.Count >= requestedVeggieCount)
                {
                    Debug.LogWarning($"야채는 최대 {requestedVeggieCount}개까지만 추가 가능합니다.");
                    return false;
                }

                Define.EVeggieType veggieType = ingredient == EIngredientType.Lettuce 
                    ? Define.EVeggieType.Lettuce 
                    : Define.EVeggieType.Tomato;

                // 선택된 receipt에서 해당 야채의 개수 확인
                int requestedVeggieTypeCount = requestedRecipe.Veggies != null 
                    ? requestedRecipe.Veggies.Count(v => v == veggieType) 
                    : 0;
                
                if (_currentRecipe.Veggies.Count(v => v == veggieType) >= requestedVeggieTypeCount)
                {
                    Debug.LogWarning($"{veggieType}는 최대 {requestedVeggieTypeCount}개까지만 추가 가능합니다.");
                    return false;
                }

                _currentRecipe.Veggies.Add(veggieType);
                return true;

            case EIngredientType.Sauce1:
                // 선택된 receipt의 소스1 개수까지만 추가 가능
                if (_currentRecipe.Sauce1Count >= requestedRecipe.Sauce1Count)
                {
                    Debug.LogWarning($"소스1은 최대 {requestedRecipe.Sauce1Count}개까지만 추가 가능합니다.");
                    return false;
                }
                _currentRecipe.Sauce1Count++;
                return true;

            case EIngredientType.Sauce2:
                // 선택된 receipt의 소스2 개수까지만 추가 가능
                if (_currentRecipe.Sauce2Count >= requestedRecipe.Sauce2Count)
                {
                    Debug.LogWarning($"소스2는 최대 {requestedRecipe.Sauce2Count}개까지만 추가 가능합니다.");
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

        if (_isGrilling)
        {
            Debug.Log("패티가 아직 굽는 중입니다.");
            return;
        }

        if (_pattyCooked)
        {
            MovePattyFromGrillToAssembly();
            return;
        }

        StartGrilling();
    }

    private void StartGrilling()
    {
        ClearGrillArea();
        _grillPattyObject = CreatePattyVisual(_rawPattySprite, _flamePattyPos);
        _isGrilling = true;
        _pattyCooked = false;
        _grillRoutine = StartCoroutine(CoGrill());
    }

    private IEnumerator CoGrill()
    {
        yield return new WaitForSeconds(5f);
        _isGrilling = false;
        _pattyCooked = true;

        if (_grillPattyObject != null)
            Destroy(_grillPattyObject);

        _grillPattyObject = CreatePattyVisual(_cookedPattySprite, _flamePattyPos);
    }

    private void MovePattyFromGrillToAssembly()
    {
        if (!_pattyCooked)
            return;

        if (_grillPattyObject != null)
        {
            Destroy(_grillPattyObject);
            _grillPattyObject = null;
        }

        _pattyCooked = false;
        if (TryAddIngredientWithLimit(EIngredientType.Patty))
        {
            AddIngredientToAssembly(EIngredientType.Patty);
            CheckBurgerComplete();
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

        if (_grillRoutine != null)
        {
            StopCoroutine(_grillRoutine);
            _grillRoutine = null;
        }
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
        if (_grillPattyObject != null)
        {
            Destroy(_grillPattyObject);
            _grillPattyObject = null;
        }
        _isGrilling = false;
        _pattyCooked = false;
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
        if (_resetFailOnOpen)
        {
            _resetFailOnOpen = false;
            if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
            {
                // Hide()를 통해 풀에 반환 (중복 방지)
                if (_currentFailPopup.gameObject.activeSelf)
                {
                    _currentFailPopup.Hide();
                }
            }
            _currentFailPopup = null;
        }

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
                burgerPile.SpawnObject();
            }
        }

        // 완료된 영수증 제거
        if (_currentReceipt != null)
        {
            _activeReceipts.Remove(_currentReceipt);
            Destroy(_currentReceipt.gameObject);
            _currentReceipt = null;
        }

        // 다음 주문을 큐에서 가져와서 영수증 리프레시
        RefreshReceipts();

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
        if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
        {
            _currentFailPopup.AddFailCount();
            _currentFailPopup.Show();
            return;
        }

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
        
        GameObject popupObj = PoolManager.Instance.Pop(prefab);
        popupObj.transform.SetParent(PoolManager.Instance.GetPopupPool(), false);
        
        _currentFailPopup = popupObj.GetComponent<UI_CookingFailPopup>();
        if (_currentFailPopup == null)
        {
            PoolManager.Instance.Push(popupObj);
            return;
        }
        
        _currentFailPopup.AddFailCount();
        
        _currentFailPopup.OnNextButtonClicked = () =>
        {
            if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
            {
                _currentFailPopup.Hide();
            }
        };

        _currentFailPopup.OnMaxFailReached = () =>
        {
            _resetFailOnOpen = true;
            // PoolManager에 반환
            gameObject.SetActive(false);
            PoolManager.Instance.Push(gameObject);
        };
        
        _currentFailPopup.Show();
    }

    #endregion

}

