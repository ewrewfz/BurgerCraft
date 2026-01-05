using TMPro;
using UnityEngine;
using static Define;
using DG.Tweening;
using UnityEngine.UI;

public class UI_GameScene : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI _moneyCountText;

	[SerializeField]
	TextMeshProUGUI _toastMessageText;

	[SerializeField]
	Button SettingButton;

	[SerializeField]
	private GameObject _soundSettingPrefab;

	[SerializeField]
	private Slider _expSlider;

	[SerializeField]
	private TextMeshProUGUI _levelText;

	private bool _isMoneyAnimating = false;
	private int _previousLevel = 1; // 이전 레벨 추적 (레벨업 감지용)
	private Tween _expSliderTween; // 슬라이더 애니메이션 트윈

    private void Awake()
    {
		SettingButton.onClick.AddListener(OnClickSettingButton);
    }

    private void OnEnable()
	{
		RefreshUI();
		
		// GameManager와 Restaurant가 초기화된 후에만 경험치 UI 업데이트
		if (GameManager.Instance != null && GameManager.Instance.Restaurant != null)
		{
			RefreshExpUI();
			_previousLevel = GameManager.Instance.Level;
		}
		
		GameManager.Instance.AddEventListener(EEventType.MoneyChanged, RefreshUI);
		GameManager.Instance.AddEventListener(EEventType.ExpChanged, RefreshExpUI);
		
		// 초기화가 완료되면 경험치 UI 업데이트
		StartCoroutine(CoWaitForInitialization());
	}

	private System.Collections.IEnumerator CoWaitForInitialization()
	{
		// GameManager와 Restaurant가 초기화될 때까지 대기
		while (GameManager.Instance == null || GameManager.Instance.Restaurant == null)
		{
			yield return null;
		}
		
		// 초기화 완료 후 경험치 UI 업데이트
		RefreshExpUI();
		_previousLevel = GameManager.Instance.Level;
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.MoneyChanged, RefreshUI);
		GameManager.Instance.RemoveEventListener(EEventType.ExpChanged, RefreshExpUI);
		
		// 슬라이더 애니메이션 중지
		_expSliderTween?.Kill();
	}

	public void RefreshUI()
	{
		if (_isMoneyAnimating)
			return;

		long money = GameManager.Instance.Money;
		_moneyCountText.text = Utils.GetMoneyText(money);
	}

	/// <summary>
	/// 경험치/레벨 UI를 업데이트합니다.
	/// </summary>
	public void RefreshExpUI()
	{
		if (GameManager.Instance == null || GameManager.Instance.Restaurant == null)
			return;

		int experience = GameManager.Instance.Experience;
		int level = GameManager.Instance.Level;

		// 레벨 텍스트 업데이트
		if (_levelText != null)
		{
			_levelText.text = level.ToString();
		}

		// 경험치 슬라이더 업데이트
		if (_expSlider != null)
		{
			// 경험치를 0~1 범위로 정규화 (레벨당 필요 경험치가 2이므로)
			float targetValue = Mathf.Clamp01((float)experience / EXP_PER_LEVEL);
			
			// 레벨업 감지 (레벨이 변경되었을 때만 슬라이더 리셋)
			if (level > _previousLevel)
			{
				// 레벨업 시 슬라이더 초기화 (0으로 리셋)
				_expSliderTween?.Kill();
				_expSlider.value = 0f;
				_previousLevel = level;
				
				// 레벨업 후 남은 경험치가 있으면 다시 애니메이션
				if (experience > 0)
				{
					float remainingExp = Mathf.Clamp01((float)experience / EXP_PER_LEVEL);
					AnimateExpSlider(0f, remainingExp);
				}
			}
			else
			{
				// 기존 애니메이션 중지
				_expSliderTween?.Kill();
				
				// 현재 슬라이더 값에서 목표 값으로 부드럽게 애니메이션
				float currentValue = _expSlider.value;
				AnimateExpSlider(currentValue, targetValue);
			}
			
			// 이전 레벨 업데이트 (항상 추적)
			_previousLevel = level;
		}
		else
		{
			// 슬라이더가 null인 경우 디버그 로그
			Debug.LogWarning("[UI_GameScene] _expSlider가 null입니다. Inspector에서 할당되었는지 확인하세요.");
		}
	}

	/// <summary>
	/// 경험치 슬라이더를 부드럽게 애니메이션합니다.
	/// 슬라이더가 1.0에 도달하면 레벨업을 처리합니다.
	/// </summary>
	private void AnimateExpSlider(float fromValue, float toValue)
	{
		if (_expSlider == null)
			return;

		float currentValue = fromValue;
		_expSliderTween = DOTween.To(
			() => currentValue,
			x => 
			{
				currentValue = x;
				_expSlider.value = x;
				
				// 슬라이더가 1.0에 도달하면 레벨업 처리
				if (x >= 1.0f && GameManager.Instance != null)
				{
					// 레벨업 처리 (경험치 소모 및 레벨 증가)
					GameManager.Instance.ProcessLevelUp();
					
					// 슬라이더를 0으로 리셋하고 남은 경험치로 다시 애니메이션
					_expSliderTween?.Kill();
					_expSlider.value = 0f;
					
					// 남은 경험치 확인
					int remainingExp = GameManager.Instance.Experience;
					if (remainingExp > 0)
					{
						float remainingExpNormalized = Mathf.Clamp01((float)remainingExp / EXP_PER_LEVEL);
						AnimateExpSlider(0f, remainingExpNormalized);
					}
				}
			},
			toValue,
			0.3f // 0.3초 동안 부드럽게 증가
		).SetEase(Ease.OutQuad);
	}

	public void PulseMoneyText()
	{
		if (_moneyCountText == null)
			return;

		_moneyCountText.transform.DOKill();
		_moneyCountText.transform.localScale = Vector3.one;
		_moneyCountText.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 6, 0.6f)
			.OnComplete(() => _moneyCountText.transform.localScale = Vector3.one);
	}

	private Tween _moneyTween;

	/// <summary>
	/// 소지금 감소 애니메이션 효과
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <param name="duration"></param>
	public void AnimateMoney(long from, long to, float duration = 1f)
	{
		if (_moneyCountText == null)
			return;

		_moneyTween?.Kill();
		_moneyCountText.DOKill();

		float originalSize = _moneyCountText.fontSize;
		float targetSize = 28f; 

		var seq = DOTween.Sequence();

		_isMoneyAnimating = true;
		_moneyCountText.text = Utils.GetMoneyText(from);

		// 1) 폰트 키우기
		seq.Append(DOVirtual.Float(originalSize, targetSize, 0.15f, v =>
		{
			_moneyCountText.fontSize = v;
		}).SetEase(Ease.OutQuad));

		// 2) 금액 변화
		seq.Append(DOVirtual.Float(from, to, duration, value =>
		{
			_moneyCountText.text = Utils.GetMoneyText((long)value);
		}));

		// 3) 폰트 원복
		seq.Append(DOVirtual.Float(targetSize, originalSize, 0.15f, v =>
		{
			_moneyCountText.fontSize = v;
		}).SetEase(Ease.InQuad));

		_moneyTween = seq.OnComplete(() =>
		{
			RefreshUI();
			PulseMoneyText();
			_isMoneyAnimating = false;
		});
	}

	private Tween _toastFadeTween;

	public void SetToastMessage(string message)
	{
		// 기존 페이드 애니메이션 중지
		_toastFadeTween?.Kill();
		_toastMessageText.DOKill();

		_toastMessageText.text = message;
		_toastMessageText.enabled = (string.IsNullOrEmpty(message) == false);

		if (string.IsNullOrEmpty(message) == false)
		{
			// 페이드 인 효과
			var color = _toastMessageText.color;
			color.a = 0f;
			_toastMessageText.color = color;

			_toastFadeTween = _toastMessageText.DOFade(1f, 0.3f)
				.SetEase(Ease.OutQuad);
		}
	}

	/// <summary>
	/// 튜토리얼 전용: 토스트 메시지 텍스트 컴포넌트를 반환합니다.
	/// </summary>
	public TextMeshProUGUI GetToastMessageText()
	{
		return _toastMessageText;
	}

	private UI_SoundSetting _currentSoundSettingPopup;

	private void OnClickSettingButton()
	{
		// 이미 열려있으면 닫기
		if (_currentSoundSettingPopup != null && _currentSoundSettingPopup.gameObject.activeSelf)
		{
			_currentSoundSettingPopup.Hide();
			return;
		}

		// PoolManager가 null이면 생성 불가
		if (PoolManager.Instance == null)
		{
			Debug.LogWarning("[UI_GameScene] PoolManager.Instance가 null입니다.");
			return;
		}

		// 팝업 생성 (PoolManager 사용)
		GameObject popupObj = PoolManager.Instance.Pop(_soundSettingPrefab);
		if (popupObj == null)
		{
			Debug.LogWarning("[UI_GameScene] PoolManager.Pop()이 null을 반환했습니다.");
			return;
		}

		// UI_SoundSetting 컴포넌트 가져오기
		_currentSoundSettingPopup = popupObj.GetComponent<UI_SoundSetting>();
		if (_currentSoundSettingPopup == null)
		{
			Debug.LogWarning("[UI_GameScene] UI_SoundSetting 컴포넌트를 찾을 수 없습니다.");
			PoolManager.Instance.Push(popupObj);
			return;
		}

		// 팝업 표시
		_currentSoundSettingPopup.Show();
	}
}
