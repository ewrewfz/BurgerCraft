using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using DG.Tweening;
using System.Linq.Expressions;

public class UI_OrderPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _RandomOrderText;
    [SerializeField] private TextMeshProUGUI _CurrentorderDetailText;
    [SerializeField] private TextMeshProUGUI _TotalPrice_Text;
    [SerializeField] private Button _paymentButton;
    [SerializeField] private Transform _buttonsParent;
    
    
    // 현재 제작 중인 버거 레시피
    private Define.BurgerRecipe _currentRecipe;
    
    // 요청된 주문 (손님이 원하는 버거)
    private Define.BurgerRecipe _requestedRecipe;
    
    // 대화형 텍스트 표시용
    private Coroutine _textDisplayCoroutine;
    
    public Action<Define.BurgerRecipe> OnOrderComplete;
    public Action OnOrderCancel;
    
    private void Awake()
    {
        if (_paymentButton != null)
        {
            _paymentButton.onClick.AddListener(OnPaymentButtonClick);
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("OrderPopup 활성화");
        
        // Show()가 호출되지 않았을 경우를 대비해 매번 새로운 랜덤 주문 생성
        // Show()가 호출되면 그 레시피를 사용하고, 호출되지 않았으면 랜덤 생성
        if (_requestedRecipe.Bread == Define.EBreadType.None)
        {
            Debug.Log("UI_OrderPopup: 새로운 랜덤 주문을 생성합니다.");
            _requestedRecipe = UI_OrderSystem.GenerateRandomRecipe();
        }
        
        // 활성화될 때 레시피 초기화 및 UI 업데이트
        InitializeRecipe();
        UpdateUI();
        
        // 자연어 주문 텍스트 표시
        UpdateOrderText();
    }
    
    private void OnDisable()
    {
        // 비활성화될 때 요청된 레시피 초기화 (다음 활성화 시 새로운 랜덤 주문 생성)
        _requestedRecipe = new Define.BurgerRecipe { Bread = Define.EBreadType.None };
        
        // 코루틴 중지
        if (_textDisplayCoroutine != null)
        {
            StopCoroutine(_textDisplayCoroutine);
            _textDisplayCoroutine = null;
        }
    }
    
    private void UpdateOrderText()
    {
        Debug.Log($"UpdateOrderText 호출 - _RandomOrderText null: {_RandomOrderText == null}, _requestedRecipe.Bread: {_requestedRecipe.Bread}");
        
        // 자연어 주문 텍스트 표시
        if (_RandomOrderText == null)
        {
            Debug.LogWarning("UI_OrderPopup: _RandomOrderText가 할당되지 않았습니다!");
            return;
        }
        
        // _requestedRecipe가 유효한지 확인
        if (_requestedRecipe.Bread == Define.EBreadType.None)
        {
            Debug.LogWarning($"UI_OrderPopup: _requestedRecipe가 유효하지 않습니다! (Bread: {_requestedRecipe.Bread}, Patty: {_requestedRecipe.Patty})");
            return;
        }
        
        // 기존 코루틴이 실행 중이면 중지
        if (_textDisplayCoroutine != null)
        {
            StopCoroutine(_textDisplayCoroutine);
        }
        
        // 대화형으로 텍스트 표시
        List<string> phrases = UI_OrderSystem.GenerateNaturalOrderPhrases(_requestedRecipe);
        _textDisplayCoroutine = StartCoroutine(DisplayTextSequentially(phrases));
    }
    
    private IEnumerator DisplayTextSequentially(List<string> phrases)
    {
        if (_RandomOrderText == null || phrases == null || phrases.Count == 0)
            yield break;
        
        _RandomOrderText.text = "";
        
        for (int i = 0; i < phrases.Count; i++)
        {
            // 문장 추가
            if (i > 0)
            {
                _RandomOrderText.text += ", ";
            }
            _RandomOrderText.text += phrases[i];
            
            // 다음 문장까지 대기 (0.8초 ~ 1.2초 랜덤)
            float waitTime = UnityEngine.Random.Range(0.8f, 1.2f);
            yield return new WaitForSeconds(waitTime);
        }
        
        Debug.Log($"UI_OrderPopup: 주문 텍스트 표시 완료");
    }
    
    public void Show(Define.BurgerRecipe requestedRecipe)
    {
        Debug.Log($"Show() 호출됨 - Bread: {requestedRecipe.Bread}, Patty: {requestedRecipe.Patty}, PattyCount: {requestedRecipe.PattyCount}");
        
        _requestedRecipe = requestedRecipe;
        
        gameObject.SetActive(true);
        
        // 애니메이션
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }
    
    // Show() 없이 활성화될 때 새로운 랜덤 주문 생성
    public void ShowWithRandomOrder()
    {
        _requestedRecipe = UI_OrderSystem.GenerateRandomRecipe();
        gameObject.SetActive(true);
        
        // 애니메이션
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
            rectTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }
    
    public void Hide()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void InitializeRecipe()
    {
        _currentRecipe = UI_OrderSystem.CreateEmptyRecipe();
    }
    
    public void AddIngredient(OrderButton button)
    {
        if (button == null)
            return;
        
        switch (button.ButtonType)
        {
            case EOrderButtonType.Bread:
                _currentRecipe.Bread = button.BreadType;
                break;
                
            case EOrderButtonType.Patty:
                if (_currentRecipe.Patty == Define.EPattyType.None)
                {
                    _currentRecipe.Patty = button.PattyType;
                }
                
                if (_currentRecipe.Patty == button.PattyType && 
                    _currentRecipe.PattyCount < Define.ORDER_MAX_PATTY_COUNT)
                {
                    _currentRecipe.PattyCount++;
                }
                break;
                 
            case EOrderButtonType.Veggie:
                if (_currentRecipe.Veggies == null)
                {
                    _currentRecipe.Veggies = new List<Define.EVeggieType>();
                }
                
                int veggieCount = _currentRecipe.Veggies.Count(v => v == button.VeggieType);
                int totalVeggies = _currentRecipe.Veggies.Count;
                
                if (totalVeggies < Define.ORDER_MAX_VEGGIES_TOTAL)
                {
                    _currentRecipe.Veggies.Add(button.VeggieType);
                }
                break;
                
            case EOrderButtonType.Sauce:
                if (button.SauceType == Define.ESauceType.Sauce1)
                {
                    if (_currentRecipe.Sauce1Count < Define.ORDER_MAX_SAUCE1_COUNT)
                    {
                        _currentRecipe.Sauce1Count++;
                    }
                }
                else if (button.SauceType == Define.ESauceType.Sauce2)
                {
                    if (_currentRecipe.Sauce2Count < Define.ORDER_MAX_SAUCE2_COUNT)
                    {
                        _currentRecipe.Sauce2Count++;
                    }
                }
                break;
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // 주문 상세 텍스트 업데이트 (영수증 형식)
        if (_CurrentorderDetailText != null)
        {
            string orderText = GetReceiptText();
            _CurrentorderDetailText.text = orderText;
        }
        
        // 가격 업데이트
        if (_TotalPrice_Text != null)
        {
            int totalPrice = UI_OrderSystem.CalculatePrice(_currentRecipe);
            _TotalPrice_Text.text = $"결제 : {totalPrice}원";
        }
    }
    
    private string GetReceiptText()
    {
        var lines = new List<string>();
        
        // 빵 2개 고정
        lines.Add($"빵 {Define.PRICE_BREAD * 2}원");
        
        // 패티
        if (_currentRecipe.PattyCount > 0)
        {
            string pattyName = _currentRecipe.Patty == Define.EPattyType.Beef ? "소고기 패티" : "닭고기 패티";
            int pattyPrice = Define.PRICE_PATTY * _currentRecipe.PattyCount;
            lines.Add($"{pattyName} {pattyPrice}원");
        }
        
        // 야채
        if (_currentRecipe.Veggies != null && _currentRecipe.Veggies.Count > 0)
        {
            var veggieGroups = _currentRecipe.Veggies
                .Where(v => v != Define.EVeggieType.None)
                .GroupBy(v => v);
            
            foreach (var group in veggieGroups)
            {
                string veggieName = GetVeggieName(group.Key);
                int count = group.Count();
                int price = GetVeggiePrice(group.Key) * count;
                lines.Add($"{veggieName} {price}원");
            }
        }
        
        // 소스
        if (_currentRecipe.Sauce1Count > 0)
        {
            int sauce1Price = Define.PRICE_SAUCE1 * _currentRecipe.Sauce1Count;
            lines.Add($"소스1 {sauce1Price}원");
        }
        
        if (_currentRecipe.Sauce2Count > 0)
        {
            int sauce2Price = Define.PRICE_SAUCE2 * _currentRecipe.Sauce2Count;
            lines.Add($"소스2 {sauce2Price}원");
        }
        
        return string.Join("\n", lines);
    }
    
    private string GetVeggieName(Define.EVeggieType veggieType)
    {
        switch (veggieType)
        {
            case Define.EVeggieType.Lettuce: return "양상추";
            case Define.EVeggieType.Tomato: return "토마토";
            default: return "야채";
        }
    }
    
    private int GetVeggiePrice(Define.EVeggieType veggieType)
    {
        switch (veggieType)
        {
            case Define.EVeggieType.Tomato:
                return Define.PRICE_TOMATO;
            case Define.EVeggieType.Lettuce:
                return Define.PRICE_LETTUCE;
            default:
                return 0;
        }
    }
    
    private void OnPaymentButtonClick()
    {
        // 디버그: 직접 입력한 레시피 출력
        Debug.Log("=== 직접 입력한 레시피 ===");
        Debug.Log($"Bread: {_currentRecipe.Bread}");
        Debug.Log($"Patty: {_currentRecipe.Patty}, Count: {_currentRecipe.PattyCount}");
        Debug.Log($"Veggies: {( _currentRecipe.Veggies == null ? "null" : string.Join(", ", _currentRecipe.Veggies))}");
        Debug.Log($"Sauce1Count: {_currentRecipe.Sauce1Count}, Sauce2Count: {_currentRecipe.Sauce2Count}");
        Debug.Log($"레시피 텍스트: {UI_OrderSystem.GetOrderText(_currentRecipe)}");
        
        // 디버그: 요청된 레시피 출력
        Debug.Log("=== 요청된 레시피 (랜덤 주문) ===");
        Debug.Log($"Bread: {_requestedRecipe.Bread}");
        Debug.Log($"Patty: {_requestedRecipe.Patty}, Count: {_requestedRecipe.PattyCount}");
        Debug.Log($"Veggies: {( _requestedRecipe.Veggies == null ? "null" : string.Join(", ", _requestedRecipe.Veggies))}");
        Debug.Log($"Sauce1Count: {_requestedRecipe.Sauce1Count}, Sauce2Count: {_requestedRecipe.Sauce2Count}");
        Debug.Log($"레시피 텍스트: {UI_OrderSystem.GetOrderText(_requestedRecipe)}");
        Debug.Log($"자연어 주문 텍스트: {UI_OrderSystem.GenerateNaturalOrderText(_requestedRecipe)}");
        
        // 주문 일치 여부 확인
        bool isMatch = CheckOrderMatch();
        
        Debug.Log($"=== 주문 일치 결과: {isMatch} ===");
        
        if (isMatch)
        {
            // 주문 완료 팝업 표시
            ShowOrderCompletePopup(_currentRecipe);
            
            // 주문 완료 처리
            if (OnOrderComplete != null)
            {
                OnOrderComplete(_currentRecipe);
            }
        }
        else
        {
            // 주문 불일치 처리 (추후 UI 표출 로직 추가 가능)
            Debug.LogWarning("주문이 일치하지 않습니다!");
            // TODO: 실패 UI 표출 및 실패 카운트 증가
        }
        
        Hide();
    }
    
    [Header("Order Complete Popup")]
    [SerializeField] private GameObject _orderCompletePrefab;
    
    private void ShowOrderCompletePopup(Define.BurgerRecipe recipe)
    {
        GameObject prefab = null;
        
        // SerializeField로 할당된 프리펩이 있으면 사용
        if (_orderCompletePrefab != null)
        {
            prefab = _orderCompletePrefab;
        }
        else
        {
            // Resources에서 프리펩 로드 시도
            prefab = Resources.Load<GameObject>("Prefabs/UI/Popup/UI_OrderComplete");
            if (prefab == null)
            {
                // @Resources 폴더 경로로 시도
                prefab = Resources.Load<GameObject>("@Resources/Prefabs/UI/Popup/UI_OrderComplete");
            }
        }
        
        if (prefab == null)
        {
            Debug.LogError("UI_OrderComplete 프리펩을 찾을 수 없습니다! Inspector에서 _orderCompletePrefab을 할당해주세요.");
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
        
        // UI_OrderComplete 컴포넌트 가져와서 Show 호출
        UI_OrderComplete orderComplete = popupObj.GetComponent<UI_OrderComplete>();
        if (orderComplete == null)
        {
            Debug.LogError("UI_OrderComplete 컴포넌트를 찾을 수 없습니다!");
            Destroy(popupObj);
            return;
        }
        
        orderComplete.Show(recipe);
    }
    
    public Define.BurgerRecipe GetCurrentRecipe()
    {
        return _currentRecipe;
    }
    
    public bool CheckOrderMatch()
    {
        if (_requestedRecipe.Bread == Define.EBreadType.None)
            return false;
            
        return UI_OrderSystem.IsMatch(_currentRecipe, _requestedRecipe);
    }
}

