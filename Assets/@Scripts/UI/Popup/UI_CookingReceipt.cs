using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 단일 주문 영수증 UI. 요청된 레시피 표시와 가격 계산 담당.
/// </summary>
public class UI_CookingReceipt : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _receiptText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private TextMeshProUGUI _totalText;

    private Define.BurgerRecipe _recipe;
    public Define.BurgerRecipe Recipe => _recipe;

    public void Init(Define.BurgerRecipe recipe)
    {
        _recipe = recipe;
        UpdateReceiptText();
    }

    private void OnValidate()
    {
        if (_recipe.Veggies == null)
            _recipe.Veggies = new System.Collections.Generic.List<Define.EVeggieType>();
    }

    private void UpdateReceiptText()
    {
        if (_receiptText != null)
        {
            _receiptText.text = GetReceiptText(_recipe);
        }

        int basePrice = UI_OrderSystem.CalculatePrice(_recipe);
        int tip = 2;
        int total = basePrice + tip;

        if (_priceText != null) _priceText.text = $"{basePrice}원";
        if (_tipText != null) _tipText.text = $"{tip}원";
        if (_totalText != null) _totalText.text = $"{total}원";
    }

    private string GetReceiptText(Define.BurgerRecipe recipe)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"빵 x2 : {recipe.Bread}");
        sb.AppendLine($"패티 : {(recipe.PattyCount > 0 ? $"{recipe.Patty} x{recipe.PattyCount}" : "없음")}");

        if (recipe.Veggies != null && recipe.Veggies.Count > 0)
        {
            var grouped = recipe.Veggies
                .Where(v => v != Define.EVeggieType.None)
                .GroupBy(v => v)
                .Select(g => $"{g.Key} x{g.Count()}");
            sb.AppendLine($"야채 : {string.Join(", ", grouped)}");
        }
        else
        {
            sb.AppendLine("야채 : 없음");
        }

        sb.AppendLine($"소스1 x{recipe.Sauce1Count}");
        sb.AppendLine($"소스2 x{recipe.Sauce2Count}");

        return sb.ToString();
    }
}

