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

	private bool _isMoneyAnimating = false;

    private void Awake()
    {
		SettingButton.onClick.AddListener(OnClickSettingButton);
    }

    private void OnEnable()
	{
		RefreshUI();
		GameManager.Instance.AddEventListener(EEventType.MoneyChanged, RefreshUI);
	}

	private void OnDisable()
	{
		GameManager.Instance.RemoveEventListener(EEventType.MoneyChanged, RefreshUI);
	}

	public void RefreshUI()
	{
		if (_isMoneyAnimating)
			return;

		long money = GameManager.Instance.Money;
		_moneyCountText.text = Utils.GetMoneyText(money);
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
