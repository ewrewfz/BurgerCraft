using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Define;

public enum EOrderButtonType
{
    Bread,
    Patty,
    Cheese,
    Veggie,
    Sauce,
}

public class OrderButton : MonoBehaviour
{
    [Header("재료 설정")]
    [SerializeField] private EOrderButtonType _buttonType;
    
    // Bread 설정
    [SerializeField] private EBreadType _breadType = EBreadType.Sesame;
    
    // Patty 설정
    [SerializeField] private EPattyType _pattyType = EPattyType.Beef;
    
    // Veggie 설정
    [SerializeField] private EVeggieType _veggieType = EVeggieType.Lettuce;
    
    // Sauce 설정
    [SerializeField] private ESauceType _sauceType = ESauceType.Sauce1;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;
    
    private Button _button;
    private UI_OrderPopup _orderPopup;
    
    private int _price = 0;
    
    void Start()
    {
        _button = GetComponent<Button>();
        _orderPopup = GetComponentInParent<UI_OrderPopup>();
        
        if (_button != null)
        {
            _button.onClick.AddListener(OnButtonClick);
        }
        
        CalculatePrice();
        UpdateUI();
    }
    
    private void CalculatePrice()
    {
        switch (_buttonType)
        {
            case EOrderButtonType.Bread:
                _price = PRICE_BREAD;
                break;
            case EOrderButtonType.Patty:
                _price = PRICE_PATTY;
                break;
            case EOrderButtonType.Veggie:
                switch (_veggieType)
                {
                    case EVeggieType.Tomato:
                        _price = PRICE_TOMATO;
                        break;
                    case EVeggieType.Lettuce:
                        _price = PRICE_LETTUCE;
                        break;
                }
                break;
            case EOrderButtonType.Sauce:
                _price = (_sauceType == ESauceType.Sauce1) ? PRICE_SAUCE1 : PRICE_SAUCE2;
                break;
        }
    }
    
    private void UpdateUI()
    {
        string name = GetIngredientName();
        
        if (_nameText != null)
        {
            _nameText.text = name;
        }
        
        if (_priceText != null)
        {
            _priceText.text = _price > 0 ? $"{_price}원" : "무료";
        }
    }
    
    private string GetIngredientName()
    {
        switch (_buttonType)
        {
            case EOrderButtonType.Bread:
                return _breadType == EBreadType.Sesame ? "참깨빵" : "빵";
            case EOrderButtonType.Patty:
                return _pattyType == EPattyType.Beef ? "소고기패티" : "닭고기패티";
            case EOrderButtonType.Veggie:
                switch (_veggieType)
                {
                    case EVeggieType.Lettuce: return "양상추";
                    case EVeggieType.Tomato: return "토마토";
                    default: return "야채";
                }
            case EOrderButtonType.Sauce:
                return _sauceType == ESauceType.Sauce1 ? "소스1" : "소스2";
            default:
                return "재료";
        }
    }
    
    private void OnButtonClick()
    {
        if (_orderPopup == null)
            return;
        
        _orderPopup.AddIngredient(this);
    }
    
    public EOrderButtonType ButtonType => _buttonType;
    public EBreadType BreadType => _breadType;
    public EPattyType PattyType => _pattyType;
    public EVeggieType VeggieType => _veggieType;
    public ESauceType SauceType => _sauceType;
    public int Price => _price;
}
