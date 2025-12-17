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
        // Show()가 호출되지 않았을 경우를 대비해 매번 새로운 랜덤 주문 생성
        // Show()가 호출되면 그 레시피를 사용하고, 호출되지 않았으면 랜덤 생성
        // 단, 실패 후 다시 열 때는 기존 레시피를 유지해야 하므로 Show()에서 설정된 레시피는 덮어쓰지 않음
        // Show()는 OnEnable() 전에 호출되므로, 여기서는 Show()가 호출되지 않은 경우에만 랜덤 생성
        if (_requestedRecipe.Bread == Define.EBreadType.None)
        {
            _requestedRecipe = UI_OrderSystem.GenerateRandomRecipe();
        }
        
        // 현재 손님이 설정되지 않았으면 Counter에서 첫 번째 손님 찾기
        if (_currentGuest == null)
        {
            FindAndSetFirstGuest();
        }
        
        // 활성화될 때 레시피 초기화 및 UI 업데이트
        InitializeRecipe();
        UpdateUI();
        
        // 자연어 주문 텍스트 표시
        UpdateOrderText();
    }
    
    /// <summary>
    /// Counter에서 첫 번째 손님을 찾아서 설정합니다.
    /// </summary>
    private void FindAndSetFirstGuest()
    {
        // Counter 찾기
        Counter counter = FindObjectOfType<Counter>();
        if (counter != null)
        {
            // Counter의 첫 번째 손님 가져오기
            GuestController firstGuest = counter.GetFirstGuest();
            if (firstGuest != null)
            {
                SetCurrentGuest(firstGuest);
            }
        }
    }
    
    private void OnDisable()
    {
        // 비활성화될 때 요청된 레시피는 유지 (실패 후 다시 열 때 같은 레시피 사용)
        // 새로운 주문이 시작될 때만 초기화됨 (ShowWithRandomOrder 호출 시)
        
        // 코루틴 중지
        if (_textDisplayCoroutine != null)
        {
            StopCoroutine(_textDisplayCoroutine);
            _textDisplayCoroutine = null;
        }
    }
    
    private void UpdateOrderText()
    {
        // 자연어 주문 텍스트 표시
        if (_RandomOrderText == null)
        {
            return;
        }
        
        // _requestedRecipe가 유효한지 확인
        if (_requestedRecipe.Bread == Define.EBreadType.None)
        {
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
    }
    
    public void Show(Define.BurgerRecipe requestedRecipe)
    {
        _requestedRecipe = requestedRecipe;
        
        // 현재 손님이 설정되지 않았으면 Counter에서 첫 번째 손님 찾기
        if (_currentGuest == null)
        {
            FindAndSetFirstGuest();
        }
        
        // 부모 오브젝트부터 활성화
        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
        
        // 게임오브젝트 강제 활성화
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
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
        //// 새로운 랜덤 주문 생성 (기존 레시피 초기화)
        //_requestedRecipe = UI_OrderSystem.GenerateRandomRecipe();
        
        // 현재 손님이 설정되지 않았으면 Counter에서 첫 번째 손님 찾기
        if (_currentGuest == null)
        {
            FindAndSetFirstGuest();
        }
        
        // 게임오브젝트 활성화
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
        
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
                // 패티가 없거나 다른 종류의 패티를 클릭한 경우
                if (_currentRecipe.Patty == Define.EPattyType.None || _currentRecipe.Patty != button.PattyType)
                {
                    // 기존 패티 리셋하고 새 패티로 시작
                    _currentRecipe.Patty = button.PattyType;
                    _currentRecipe.PattyCount = 1;
                }
                else if (_currentRecipe.Patty == button.PattyType)
                {
                    // 같은 종류의 패티를 클릭한 경우 개수 증가
                    if (_currentRecipe.PattyCount < Define.ORDER_MAX_PATTY_COUNT)
                    {
                        _currentRecipe.PattyCount++;
                    }
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
        // 주문 일치 여부 확인
        bool isMatch = CheckOrderMatch();
        
        if (isMatch)
        {
            // 주문 성공 시 실패 카운트 초기화
            if (_currentGuest != null)
            {
                _currentGuest.ResetFailCount();
            }
            
            // 기존 실패 팝업이 있으면 실패 카운트 초기화 후 닫기
            if (_currentFailPopup != null)
            {
                _currentFailPopup.ResetFailCount();
                _currentFailPopup.Hide();
                _currentFailPopup = null;
            }
            
            // 주문 완료 팝업 표시
            ShowOrderCompletePopup(_currentRecipe);
            
            // 주문 완료 처리 - 테이블로 보내기
            if (_currentGuest != null)
            {
                Counter counter = FindObjectOfType<Counter>();
                if (counter != null)
                {
                    counter.ProcessOrderComplete(_currentGuest, false);
                }
            }
            
            // 주문 완료 이벤트 호출
            if (OnOrderComplete != null)
            {
                OnOrderComplete(_currentRecipe);
            }
            
            // OrderPopup 파괴 (성공 시)
            DestroyOrderPopup();
        }
        else
        {
            // 주문 실패 처리 - 실패 팝업 표시 (ShowOrderFailPopup에서 실패 카운트 확인 후 처리)
            ShowOrderFailPopup();
        }
        
        Hide();
    }
    
    [Header("Order Complete Popup")]
    [SerializeField] private GameObject _orderCompletePrefab;
    
    [Header("Order Fail Popup")]
    [SerializeField] private GameObject _orderFailPrefab;
    
    private GuestController _currentGuest;
    
    private UI_FailPopup _currentFailPopup;
    
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
            Destroy(popupObj);
            return;
        }
        
        orderComplete.Show(recipe);
    }
    
    private void ShowOrderFailPopup()
    {
        // 현재 손님이 없으면 실패 팝업을 표시할 수 없음
        if (_currentGuest == null)
        {
            return;
        }
        
        // 실패 카운트 증가 (먼저 증가시켜서 3회인지 확인)
        _currentGuest.AddFailCount();
        int currentFailCount = _currentGuest.FailCount;
        
        // 3회 실패 시 팝업을 표시하지 않고 바로 손님이 스폰 위치로 돌아가도록 처리
        if (currentFailCount >= 3)
        {
            // 기존 실패 팝업이 있으면 풀에 반환
            if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
            {
                _currentFailPopup.gameObject.SetActive(false);
                PoolManager.Instance.Push(_currentFailPopup.gameObject);
                _currentFailPopup = null;
            }
            
            // OrderPopup 파괴 (3회 실패 시)
            DestroyOrderPopup();
            
            // 손님이 스폰 위치로 돌아가도록 처리
            Counter counter = FindObjectOfType<Counter>();
            if (counter != null)
            {
                counter.ProcessOrderComplete(_currentGuest, true);
            }

            Utils.ApplyMoneyChange(-100);
            return;
        }
        
        // 3회 미만이면 팝업 표시
        // 기존 실패 팝업이 있으면 재사용
        if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
        {
            _currentFailPopup.SetAssociatedGuest(_currentGuest);
            _currentFailPopup.Show();
            SetupFailPopupCallbacks();
            return;
        }
        
        // 프리펩 가져오기
        GameObject prefab = _orderFailPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/UI/Popup/UI_FailPopup");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("@Resources/Prefabs/UI/Popup/UI_FailPopup");
            }
        }
        
        if (prefab == null)
        {
            return;
        }
        
        // 팝업 생성 (PoolManager에서 가져오기)
        GameObject popupObj = PoolManager.Instance.Pop(prefab);
        popupObj.transform.SetParent(PoolManager.Instance.GetPopupPool(), false);
        
        // UI_FailPopup 컴포넌트 가져오기
        _currentFailPopup = popupObj.GetComponent<UI_FailPopup>();
        if (_currentFailPopup == null)
        {
            PoolManager.Instance.Push(popupObj);
            return;
        }
        
        // 현재 손님과 연결 및 콜백 설정
        _currentFailPopup.SetAssociatedGuest(_currentGuest);
        SetupFailPopupCallbacks();
        
        // 팝업 표시
        _currentFailPopup.Show();
    }
    
    private void SetupFailPopupCallbacks()
    {
        if (_currentFailPopup == null)
            return;
        
        // 필요한 정보 저장
        Define.BurgerRecipe requestedRecipe = _requestedRecipe;
        GuestController currentGuest = _currentGuest;
        UI_OrderPopup orderPopupInstance = this;
        
        // 3회 실패 시 손님이 떠나도록 콜백 설정
        _currentFailPopup.OnAllFailsReached = () =>
        {
            if (currentGuest != null)
            {
                if (_currentFailPopup != null && _currentFailPopup.gameObject != null)
                {
                    _currentFailPopup.gameObject.SetActive(false);
                    PoolManager.Instance.Push(_currentFailPopup.gameObject);
                }
                _currentFailPopup = null;
                currentGuest.LeaveDueToFailures();
            }
        };
        
        // 다음 단계 버튼 클릭 시 주문 팝업을 다시 열도록 콜백 설정
        _currentFailPopup.OnNextButtonClicked = () =>
        {
            if (orderPopupInstance == null)
            {
                return;
            }
            
            // 부모 오브젝트부터 활성화 (루트까지)
            Transform current = orderPopupInstance.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }
                current = current.parent;
            }
            
            // 게임오브젝트 강제 활성화
            if (!orderPopupInstance.gameObject.activeSelf)
            {
                orderPopupInstance.gameObject.SetActive(true);
            }
            
            // 현재 손님 정보 설정
            if (currentGuest != null)
            {
                orderPopupInstance.SetCurrentGuest(currentGuest);
            }
            
            // 기존 레시피로 주문 팝업 다시 열기
            if (requestedRecipe.Bread != Define.EBreadType.None)
            {
                orderPopupInstance.Show(requestedRecipe);
            }
            else
            {
                orderPopupInstance.ShowWithRandomOrder();
            }
        };
    }
    
    /// <summary>
    /// 현재 주문 중인 손님을 설정합니다.
    /// </summary>
    public void SetCurrentGuest(GuestController guest)
    {
        _currentGuest = guest;
    }
    

    private void DestroyOrderPopup()
    {
        if (_textDisplayCoroutine != null)
        {
            StopCoroutine(_textDisplayCoroutine);
            _textDisplayCoroutine = null;
        }
        
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
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


