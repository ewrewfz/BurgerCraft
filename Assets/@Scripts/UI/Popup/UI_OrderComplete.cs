using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;
using DG.Tweening;

public class UI_OrderComplete : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _ReceiptText;
    [SerializeField] private TextMeshProUGUI _PriceText;
    [SerializeField] private TextMeshProUGUI _TipText;
    [SerializeField] private TextMeshProUGUI _TotalPriceText;
    [SerializeField] private Button _closeButton;
    
    private const int TIP_AMOUNT = 2; // 팁 2원 고정
    
    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.AddListener(Hide);
        }
    }
    
    public void Show(Define.BurgerRecipe recipe)
    {
        // 레시피 텍스트 업데이트
        if (_ReceiptText != null)
        {
            _ReceiptText.text = GetReceiptText(recipe);
        }
        
        // 가격 계산
        int basePrice = UI_OrderSystem.CalculatePrice(recipe);
        int tip = TIP_AMOUNT;
        int totalPrice = basePrice + tip;
        
        // 가격 텍스트 업데이트
        if (_PriceText != null)
        {
            _PriceText.text = $"{basePrice}원";
        }
        
        if (_TipText != null)
        {
            _TipText.text = $"{tip}원";
        }
        
        if (_TotalPriceText != null)
        {
            _TotalPriceText.text = $"{totalPrice}원";
        }
        
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
    
    private string GetReceiptText(Define.BurgerRecipe recipe)
    {
        var lines = new List<string>();
        
        // 빵은 항상 포함 (2개 고정)
        lines.Add("빵 X2");
        
        // 패티
        if (recipe.PattyCount > 0)
        {
            string pattyName = recipe.Patty == Define.EPattyType.Beef ? "소고기 패티" : "닭고기 패티";
            if (recipe.PattyCount == 1)
            {
                lines.Add($"{pattyName} X1");
            }
            else
            {
                lines.Add($"{pattyName} X{recipe.PattyCount}");
            }
        }
        
        // 야채
        if (recipe.Veggies != null && recipe.Veggies.Count > 0)
        {
            var veggieGroups = recipe.Veggies
                .Where(v => v != Define.EVeggieType.None)
                .GroupBy(v => v);
            
            foreach (var group in veggieGroups)
            {
                string veggieName = GetVeggieName(group.Key);
                int count = group.Count();
                lines.Add($"{veggieName} X{count}");
            }
        }
        
        // 소스
        if (recipe.Sauce1Count > 0)
        {
            if (recipe.Sauce1Count == 1)
                lines.Add("소스1 X1");
            else
                lines.Add($"소스1 X{recipe.Sauce1Count}");
        }
        
        if (recipe.Sauce2Count > 0)
        {
            if (recipe.Sauce2Count == 1)
                lines.Add("소스2 X1");
            else
                lines.Add($"소스2 X{recipe.Sauce2Count}");
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
}

