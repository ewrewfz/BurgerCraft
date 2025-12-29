using UnityEngine;

/// <summary>
/// 버거에 주문 번호를 저장하는 컴포넌트
/// </summary>
public class BurgerOrderNumber : MonoBehaviour
{
    [SerializeField] private string _orderNumber;
    
    public string OrderNumber 
    { 
        get => _orderNumber; 
        set 
        { 
            _orderNumber = value;
        }
    }
    
    /// <summary>
    /// 주문 번호가 일치하는지 확인
    /// </summary>
    public bool MatchesOrderNumber(string orderNumber)
    {
        return !string.IsNullOrEmpty(_orderNumber) && _orderNumber == orderNumber;
    }
}

