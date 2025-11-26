using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using DG.Tweening;

public class UI_OrderPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _CurrentorderDetailText;
    [SerializeField] private TextMeshProUGUI _TotalPrice_Text;
    [SerializeField] private Button _paymentButton;
    [SerializeField] private Transform _buttonsParent;
    
    // 현재 제작 중인 버거 레시피
    private Define.BurgerRecipe _currentRecipe;
    
    // 요청된 주문 (손님이 원하는 버거)
    private Define.BurgerRecipe _requestedRecipe;
    
    public Action<Define.BurgerRecipe> OnOrderComplete;
    public Action OnOrderCancel;
    
    private void Awake()
    {
        if (_paymentButton != null)
        {
            _paymentButton.onClick.AddListener(OnPaymentButtonClick);
        }
    }
    
    public void Show(Define.BurgerRecipe requestedRecipe)
    {
        _requestedRecipe = requestedRecipe;
        InitializeRecipe();
        UpdateUI();
        
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
            _TotalPrice_Text.text = $"결제\n{totalPrice}원";
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
            string pattyName = _currentRecipe.Patty == Define.EPattyType.Beef ? "소고기" : "닭고기";
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
        // 주문 완료 처리
        if (OnOrderComplete != null)
        {
            OnOrderComplete(_currentRecipe);
        }
        
        Hide();
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

