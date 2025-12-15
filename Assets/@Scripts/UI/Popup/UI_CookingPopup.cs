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

    [Header("Transforms")]
    [SerializeField] private Transform _grillRoot;
    [SerializeField] private Transform _flamePattyPos;
    [SerializeField] private Transform _burgerCraftPos;

    [Header("2D Prefabs (UI)")]
    [SerializeField] private GameObject _burgerStackPrefab;
    [SerializeField] private GameObject _breadBottomPrefab;
    [SerializeField] private GameObject _breadTopPrefab;
    [SerializeField] private GameObject _rawPattyPrefab;
    [SerializeField] private GameObject _cookedPattyPrefab;
    [SerializeField] private GameObject _lettucePrefab;
    [SerializeField] private GameObject _tomatoPrefab;
    [SerializeField] private GameObject _sauce1Prefab;
    [SerializeField] private GameObject _sauce2Prefab;

    [Header("Sprites (fallback)")]
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
    
    [Header("Dev/Test")]
    [SerializeField] private bool _autoCreateTestReceipt = true;

    // 선택/상태
    private readonly List<UI_CookingReceipt> _activeReceipts = new List<UI_CookingReceipt>();
    private UI_CookingReceipt _currentReceipt;
    private Define.BurgerRecipe _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();
    private UI_CookingFailPopup _currentFailPopup;
    private bool _resetFailOnOpen = false;

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
        EnsureReceiptExistsForTest();
        ResetFailPopupState();
    }

    // 주문 추가(외부에서 호출)
    public void AddOrder(Define.BurgerRecipe recipe)
    {
        if (_receiptPrefab == null || _receiptParent == null)
        {
            Debug.LogWarning("Receipt prefab/parent가 설정되어 있지 않습니다.");
            return;
        }

        if (_activeReceipts.Count >= 3)
        {
            Debug.LogWarning("최대 3개 주문까지만 표시합니다.");
            return;
        }

        UI_CookingReceipt receipt = Instantiate(_receiptPrefab, _receiptParent);
        receipt.Init(recipe);
        _activeReceipts.Add(receipt);

        // 첫 주문 자동 선택
        if (_currentReceipt == null)
        {
            SelectReceipt(receipt);
        }
    }

    private void SelectReceipt(UI_CookingReceipt receipt)
    {
        _currentReceipt = receipt;
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
        if (_currentReceipt == null)
        {
            Debug.LogWarning("선택된 주문이 없습니다. 테스트 모드로 진행.");
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
            // 상단 빵 추가 시 레시피 완료 체크
            CheckBurgerComplete();
        }
        else
        {
            Debug.LogWarning("빵은 상/하단 각각 한 번씩만 추가 가능합니다.");
        }
    }

    /// <summary>
    /// 번을 제외한 재료(패티, 야채, 소스)의 상한을 통합 관리하여 추가.
    /// </summary>
    private bool TryAddIngredientWithLimit(EIngredientType ingredient)
    {
        switch (ingredient)
        {
            case EIngredientType.Patty:
                {
                    int requestedPattyMax = Define.ORDER_MAX_PATTY_COUNT;
                    if (_currentReceipt != null)
                    {
                        requestedPattyMax = Mathf.Min(Define.ORDER_MAX_PATTY_COUNT, _currentReceipt.Recipe.PattyCount);
                    }

                    if (_currentRecipe.PattyCount >= requestedPattyMax)
                    {
                        // 주문 수량을 초과하는 패티는 무시
                        return false;
                    }

                    _currentRecipe.Patty = Define.EPattyType.Beef;
                    _currentRecipe.PattyCount++;
                    return true;
                }

            case EIngredientType.Lettuce:
            case EIngredientType.Tomato:
                if (_currentRecipe.Veggies == null)
                    _currentRecipe.Veggies = new List<Define.EVeggieType>();

                if (_currentRecipe.Veggies.Count >= Define.ORDER_MAX_VEGGIES_TOTAL)
                {
                    Debug.LogWarning("야채 총합은 최대 4개까지 가능합니다.");
                    return false;
                }

                Define.EVeggieType veggieType = ingredient == EIngredientType.Lettuce
                    ? Define.EVeggieType.Lettuce
                    : Define.EVeggieType.Tomato;

                int sameCount = _currentRecipe.Veggies.Count(v => v == veggieType);
                if (sameCount >= 2)
                {
                    Debug.LogWarning($"{veggieType}는 최대 2개까지만 추가 가능합니다.");
                    return false;
                }

                _currentRecipe.Veggies.Add(veggieType);
                return true;

            case EIngredientType.Sauce1:
                if (_currentRecipe.Sauce1Count >= Define.ORDER_MAX_SAUCE1_COUNT)
                {
                    Debug.LogWarning("소스1은 최대 2회까지 가능합니다.");
                    return false;
                }
                _currentRecipe.Sauce1Count++;
                return true;

            case EIngredientType.Sauce2:
                if (_currentRecipe.Sauce2Count >= Define.ORDER_MAX_SAUCE2_COUNT)
                {
                    Debug.LogWarning("소스2는 최대 2회까지 가능합니다.");
                    return false;
                }
                _currentRecipe.Sauce2Count++;
                return true;

            default:
                // Bread 등은 여기서 처리하지 않음
                return false;
        }
    }

    #endregion

    #region Patty / Grill

    private void HandlePattyClick()
    {
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

        GameObject prefab = _rawPattyPrefab;
        _grillPattyObject = CreatePattyVisual(prefab, _rawPattySprite, _flamePattyPos);
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

        _grillPattyObject = CreatePattyVisual(_cookedPattyPrefab, _cookedPattySprite, _flamePattyPos);
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

    private GameObject CreatePattyVisual(GameObject prefab, Sprite sprite, Transform parent)
    {
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.transform.localPosition = Vector3.zero;
            return obj;
        }

        if (sprite != null)
        {
            GameObject go = new GameObject("Patty", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.SetNativeSize();
            return go;
        }

        return null;
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
        GameObject prefab = GetPrefabForIngredient(ingredient, isTopBread);
        Sprite sprite = GetSpriteForIngredient(ingredient, isTopBread);

        GameObject ingredientObj = null;
        
        if (prefab != null)
        {
            ingredientObj = Instantiate(prefab);
        }
        else if (sprite != null)
        {
            ingredientObj = new GameObject(ingredient.ToString(), typeof(RectTransform), typeof(Image));
            var img = ingredientObj.GetComponent<Image>();
            img.sprite = sprite;
            img.SetNativeSize();
        }
        else
        {
            Debug.LogWarning($"{ingredient} 시각화 자원이 없습니다.");
            return null;
        }

        // 재료 태그 저장
        var tag = ingredientObj.AddComponent<UI_IngredientTag>();
        tag.Type = ingredient;
        tag.IsTopBread = isTopBread;

        // 재료에 드래그 핸들러 추가하여 부모의 드래그를 전달
        var ingredientDragHandler = ingredientObj.AddComponent<UI_IngredientDragHandler>();
        ingredientDragHandler.SetParentStack(_currentBurgerStack);
        
        return ingredientObj;
    }

    private void SetRaycastTargetRecursive(GameObject obj, bool value)
    {
        if (obj == null)
            return;

        // Image 컴포넌트의 raycastTarget 설정
        var image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = value;
        }

        // 모든 자식 오브젝트에도 재귀적으로 적용
        foreach (Transform child in obj.transform)
        {
            SetRaycastTargetRecursive(child.gameObject, value);
        }
    }

    private GameObject GetPrefabForIngredient(EIngredientType ingredient, bool isTopBread)
    {
        switch (ingredient)
        {
            case EIngredientType.Bread:
                return isTopBread ? _breadTopPrefab : _breadBottomPrefab;
            case EIngredientType.Patty:
                return _cookedPattyPrefab;
            case EIngredientType.Lettuce:
                return _lettucePrefab;
            case EIngredientType.Tomato:
                return _tomatoPrefab;
            case EIngredientType.Sauce1:
                return _sauce1Prefab;
            case EIngredientType.Sauce2:
                return _sauce2Prefab;
            default:
                return null;
        }
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

    private static bool _hasShownBurgerStackWarning = false;

    private void CreateBurgerStack()
    {
        if (_burgerStackPrefab == null || _burgerCraftPos == null)
        {
            // 프리팹 미지정 시 런타임에 최소 컨테이너 생성 (테스트용)
            if (_burgerCraftPos == null)
            {
                Debug.LogWarning("버거 스택 생성 실패: 조립 위치(_burgerCraftPos)가 없습니다.");
                return;
            }

            // 경고 메시지는 한 번만 표시
            if (!_hasShownBurgerStackWarning)
            {
                Debug.LogWarning("버거 스택 프리팹이 지정되지 않아 임시 컨테이너를 생성했습니다. 프리팹을 연결하면 이 경고는 사라집니다.");
                _hasShownBurgerStackWarning = true;
            }

            GameObject fallback = new GameObject("BurgerStack", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UI_BurgerStack));
            var rt = fallback.GetComponent<RectTransform>();
            rt.SetParent(_burgerCraftPos, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 400); // 충분히 크게 설정하여 재료들을 포함할 수 있도록
            
            // Image 컴포넌트 설정 (레이캐스트를 위해 필요)
            var image = fallback.GetComponent<Image>();
            image.color = new Color(1, 1, 1, 0); // 투명하지만 레이캐스트는 받음
            image.raycastTarget = true;
            
            // CanvasGroup 설정 (자식들의 레이캐스트를 제어하지 않음)
            var canvasGroup = fallback.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            
            _currentBurgerStack = fallback.GetComponent<UI_BurgerStack>();
            _currentBurgerStack.OnTrashDropped += OnBurgerTrashed;
            return;
        }

        GameObject go = Instantiate(_burgerStackPrefab, _burgerCraftPos);
        _currentBurgerStack = go.GetComponent<UI_BurgerStack>();
        if (_currentBurgerStack != null)
        {
            _currentBurgerStack.OnTrashDropped += OnBurgerTrashed;
        }
    }

    public void OnBurgerTrashed(UI_BurgerStack stack)
    {
        if (stack != null)
        {
            // 버거 스택 오브젝트 삭제
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

        // 스택 기준으로 레시피를 재계산해 동기화
        RecalculateRecipeFromStack();

        // 빵 상/하단이 모두 없으면 아직 제작 중이므로 판정하지 않음
        if (!_hasBottomBread || !_hasTopBread)
            return;

        // 스택에 아무것도 없으면 판정하지 않음
        if (_assembledBurgerParts.Count == 0)
            return;

        Define.BurgerRecipe requested = _currentReceipt.Recipe;
        bool match = UI_OrderSystem.IsMatch(_currentRecipe, requested);
        
        if (match)
        {
            // 레시피 일치 → 버거 생성 및 팝업 닫기
            SpawnBurgerAndComplete();
        }
        else
        {
            // 레시피 불일치 → 실패 팝업 표시
            ShowFailPopup();
        }
    }

    /// <summary>
    /// 쌓인 재료 스택을 기준으로 현재 레시피를 다시 계산하여 동기화한다.
    /// </summary>
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
                Destroy(_currentFailPopup.gameObject);
            }
            _currentFailPopup = null;
        }

        if (_currentFailPopup != null)
        {
            if (_currentFailPopup.gameObject != null)
            {
                _currentFailPopup.ResetFailCount();
                _currentFailPopup.Hide();
            }
            else
            {
                _currentFailPopup = null;
            }
        }
    }

    private void SpawnBurgerAndComplete()
    {
        // 1. Grill 찾아서 자동 생성 비활성화
        Grill grill = FindObjectOfType<Grill>();
        if (grill != null)
        {
            grill.StopSpawnBurger = true;
        }

        // 2. BurgerPile에 버거 생성 (CoSpawnBurgers 로직 참고)
        if (grill != null)
        {
            BurgerPile burgerPile = grill.GetComponentInChildren<BurgerPile>();
            if (burgerPile != null)
            {
                // 최대치 확인
                if (burgerPile.ObjectCount < Define.GRILL_MAX_BURGER_COUNT)
                {
                    // 버거 생성 (SpawnObject는 GameManager.Instance.SpawnBurger()를 호출하고 AddToPile을 호출)
                    burgerPile.SpawnObject();
                }
            }
        }

        // 3. 현재 영수증 제거
        if (_currentReceipt != null)
        {
            _activeReceipts.Remove(_currentReceipt);
            if (_currentReceipt.gameObject != null)
            {
                Destroy(_currentReceipt.gameObject);
            }
            _currentReceipt = null;
        }

        // 4. 다음 영수증 선택 또는 팝업 닫기
        if (_activeReceipts.Count > 0)
        {
            SelectReceipt(_activeReceipts[0]);
        }
        else
        {
            // 모든 주문 완료 시 팝업 닫기
            gameObject.SetActive(false);
        }

        // 5. 버거 초기화
        ResetCurrentBurger();

        // 6. 성공 시 실패 팝업이 열려있다면 정리
        if (_currentFailPopup != null)
        {
            Destroy(_currentFailPopup.gameObject);
            _currentFailPopup = null;
        }

        // 7. 팝업 비활성화 (다음에 다시 열었을 때 새 주문을 받을 수 있도록)
        gameObject.SetActive(false);
    }

    private void ShowFailPopup()
    {
        // 이미 떠 있는 실패 팝업이 있으면 재사용 (파괴 여부도 확인)
        if (_currentFailPopup != null)
        {
            if (_currentFailPopup.gameObject != null)
            {
                _currentFailPopup.AddFailCount();
                _currentFailPopup.Show();
                return;
            }
            else
            {
                _currentFailPopup = null;
            }
        }

        GameObject prefab = null;
        
        // SerializeField로 할당된 프리펩이 있으면 사용
        if (_failPopupPrefab != null)
        {
            prefab = _failPopupPrefab;
        }
        else
        {
            // Resources에서 프리팹 로드 시도
            prefab = Resources.Load<GameObject>("Prefabs/UI/Popup/UI_CookingFailPopup");
            if (prefab == null)
            {
                // @Resources 폴더 경로로 시도
                prefab = Resources.Load<GameObject>("@Resources/Prefabs/UI/Popup/UI_CookingFailPopup");
            }
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("실패 팝업 프리팹을 찾을 수 없습니다.");
            return;
        }
        
        // 팝업 생성
        GameObject popupObj = Instantiate(prefab);
        
        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            popupObj.transform.SetParent(canvas.transform, false);
        }
        else
        {
            popupObj.transform.SetParent(transform.root, false);
        }
        
        // UI_CookingFailPopup 컴포넌트 가져오기
        _currentFailPopup = popupObj.GetComponent<UI_CookingFailPopup>();
        if (_currentFailPopup == null)
        {
            Destroy(popupObj);
            return;
        }
        
        // 실패 카운트 증가
        _currentFailPopup.AddFailCount();
        
        // 다음 버튼 클릭 시 CookingPopup 다시 열기
        _currentFailPopup.OnNextButtonClicked = () =>
        {
            if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
            {
                _currentFailPopup.Hide();
            }
            // CookingPopup은 이미 열려있으므로 그대로 유지
        };

        // 최대 실패 시 팝업 닫고 CookingPopup 비활성화
        _currentFailPopup.OnMaxFailReached = () =>
        {
            // FailPopup은 AddFailCount() 내에서 이미 Hide/비활성화 처리됨
            // 여기서는 CookingPopup만 닫고, 다음 열 때 초기화 플래그만 설정
            gameObject.SetActive(false);
            _resetFailOnOpen = true;
        };
        
        // 팝업 표시
        _currentFailPopup.Show();
    }

    #endregion

    private void EnsureReceiptExistsForTest()
    {
        // 개발/테스트 모드에서 주문이 없는 상태로 버튼을 누를 때 대비
        if (!_autoCreateTestReceipt)
            return;

        if (_currentReceipt == null)
        {
            if (_activeReceipts.Count > 0)
            {
                SelectReceipt(_activeReceipts[0]);
                return;
            }

            if (_receiptParent != null && _receiptPrefab != null)
            {
                var dummy = Instantiate(_receiptPrefab, _receiptParent);
                var recipe = UI_OrderSystem.GenerateRandomRecipe();
                dummy.Init(recipe);
                _activeReceipts.Add(dummy);
                SelectReceipt(dummy);
            }
        }
    }
}

