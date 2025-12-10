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
    private enum EIngredientType
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
    [Header("Dev/Test")]
    [SerializeField] private bool _autoCreateTestReceipt = true;

    // 선택/상태
    private readonly List<UI_CookingReceipt> _activeReceipts = new List<UI_CookingReceipt>();
    private UI_CookingReceipt _currentReceipt;
    private Define.BurgerRecipe _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();

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
        CheckBurgerComplete();
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
                if (_currentRecipe.PattyCount >= Define.ORDER_MAX_PATTY_COUNT)
                {
                    Debug.LogWarning("패티는 최대 2장까지 가능합니다.");
                    return false;
                }
                _currentRecipe.Patty = Define.EPattyType.Beef;
                _currentRecipe.PattyCount++;
                return true;

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

        if (prefab != null)
        {
            return Instantiate(prefab);
        }

        if (sprite != null)
        {
            GameObject go = new GameObject(ingredient.ToString(), typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.SetNativeSize();
            return go;
        }

        Debug.LogWarning($"{ingredient} 시각화 자원이 없습니다.");
        return null;
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

            GameObject fallback = new GameObject("BurgerStack", typeof(RectTransform), typeof(UI_BurgerStack));
            var rt = fallback.GetComponent<RectTransform>();
            rt.SetParent(_burgerCraftPos, false);
            _currentBurgerStack = fallback.GetComponent<UI_BurgerStack>();
            _currentBurgerStack.OnTrashDropped += OnBurgerTrashed;
            Debug.LogWarning("버거 스택 프리팹이 지정되지 않아 임시 컨테이너를 생성했습니다. 프리팹을 연결하면 이 경고는 사라집니다.");
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

        Define.BurgerRecipe requested = _currentReceipt.Recipe;
        bool match = UI_OrderSystem.IsMatch(_currentRecipe, requested);
        if (match)
            Debug.Log("주문과 일치하는 버거 완성!");
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

