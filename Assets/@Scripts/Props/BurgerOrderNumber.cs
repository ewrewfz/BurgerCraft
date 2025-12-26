using UnityEngine;
using TMPro;

/// <summary>
/// 버거에 주문 번호를 저장하는 컴포넌트
/// </summary>
public class BurgerOrderNumber : MonoBehaviour
{
    [SerializeField] private string _orderNumber;
    [SerializeField] private TextMeshProUGUI _orderNumberText; // 인스펙터에서 직접 할당
    private Transform _canvasTransform;
    private Camera _mainCamera;
    
    public string OrderNumber 
    { 
        get => _orderNumber; 
        set 
        { 
            _orderNumber = value;
            UpdateOrderNumberText();
        }
    }
    
    private void Awake()
    {
        // Canvas 찾기
        Canvas canvas = Utils.FindChild<Canvas>(gameObject);
        if (canvas != null)
        {
            _canvasTransform = canvas.transform;
        }
        
        // 메인 카메라 찾기
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            _mainCamera = FindObjectOfType<Camera>();
        }
    }
    
    private void OnEnable()
    {
        // 활성화될 때 주문번호 텍스트 업데이트
        UpdateOrderNumberText();
    }
    
    private void LateUpdate()
    {
        // Canvas가 항상 카메라를 바라보도록 회전
        if (_canvasTransform != null && _mainCamera != null)
        {
            Vector3 directionToCamera = _mainCamera.transform.position - _canvasTransform.position;
            if (directionToCamera != Vector3.zero)
            {
                _canvasTransform.rotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(0, 180, 0);
            }
        }
    }
    
    /// <summary>
    /// 주문번호 텍스트를 UI에 표시합니다.
    /// </summary>
    private void UpdateOrderNumberText()
    {
        if (_orderNumberText != null)
        {
            // 주문번호 : (GUID) 형식으로 표시
            _orderNumberText.text = !string.IsNullOrEmpty(_orderNumber) ? $"주문번호 : {_orderNumber}" : "주문번호 :";
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

