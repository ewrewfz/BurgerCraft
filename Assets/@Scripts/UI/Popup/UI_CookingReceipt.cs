using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 단일 주문 영수증 UI. 요청된 레시피 표시와 가격 계산 담당.
/// </summary>
public class UI_CookingReceipt : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI _receiptText;
    [SerializeField] private TextMeshProUGUI _priceText;
    [SerializeField] private TextMeshProUGUI _tipText;
    [SerializeField] private TextMeshProUGUI _totalText;
    [SerializeField] private TextMeshProUGUI _orderNumberText; // 주문 번호 표시용
    [SerializeField] private GameObject _workingAI;
    
    [Header("Outline")]
    [SerializeField] private Image _receiptImage; // Receipt 프리팹의 Image 컴포넌트

    private Define.BurgerRecipe _recipe;
    public Define.BurgerRecipe Recipe => _recipe;
    private string _orderNumber;
    public string OrderNumber => _orderNumber;
    
    private Outline _outline;
    private bool _isWorkingAI = false;
    public bool IsWorkingAI => _isWorkingAI;
    
    /// <summary>
    /// 영수증 클릭 시 호출되는 콜백
    /// </summary>
    public Action<UI_CookingReceipt> OnReceiptClicked;

    private void Awake()
    {
        // Image 컴포넌트 자동 찾기 (없으면)
        if (_receiptImage == null)
        {
            _receiptImage = GetComponent<Image>();
        }
        
        // Image가 raycastTarget이 활성화되어 있는지 확인
        if (_receiptImage != null && !_receiptImage.raycastTarget)
        {
            _receiptImage.raycastTarget = true;
        }
        
        // Outline 컴포넌트 자동 추가 (없으면)
        if (_receiptImage != null)
        {
            _outline = _receiptImage.GetComponent<Outline>();
            if (_outline == null)
            {
                _outline = _receiptImage.gameObject.AddComponent<Outline>();
            }
            
        // 초기 설정
        _outline.enabled = false;
        _outline.effectColor = Color.yellow;
        _outline.effectDistance = new Vector2(5, -5);
        
        // Working_AI 자동 찾기 (없으면)
        if (_workingAI == null)
        {
            _workingAI = Utils.FindChild(gameObject, "Working_AI", true);
        }
        
        // 초기에는 비활성화
        if (_workingAI != null)
        {
            _workingAI.SetActive(false);
        }
    }
    }
    
    /// <summary>
    /// 영수증 클릭 이벤트 처리
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        OnReceiptClicked?.Invoke(this);
    }

    public void Init(Define.BurgerRecipe recipe, string orderNumber = null)
    {
        _recipe = recipe;
        _orderNumber = orderNumber;
        UpdateReceiptText();
    }
    
    /// <summary>
    /// 선택 상태에 따라 아웃라인 표시/숨김
    /// </summary>
    public void SetSelected(bool selected)
    {
        // Outline이 없으면 다시 찾기
        if (_outline == null && _receiptImage != null)
        {
            _outline = _receiptImage.GetComponent<Outline>();
            if (_outline == null)
            {
                _outline = _receiptImage.gameObject.AddComponent<Outline>();
                _outline.effectColor = Color.yellow;
                _outline.effectDistance = new Vector2(3, -3);
            }
        }
        
        if (_outline != null)
        {
            _outline.enabled = selected;
        }
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
        
        // 주문 번호 표시
        if (_orderNumberText != null)
        {
            _orderNumberText.text = !string.IsNullOrEmpty(_orderNumber) ? _orderNumber : "";
        }
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
    
    /// <summary>
    /// 알바생이 작업 중인지 표시합니다.
    /// </summary>
    public void SetWorkingAI(bool isWorking)
    {
        _isWorkingAI = isWorking;
        
        if (_workingAI != null)
        {
            _workingAI.SetActive(isWorking);
        }
    }
}

