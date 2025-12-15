using TMPro;
using UnityEngine;
using static Define;
using DG.Tweening;

public class UI_GameScene : MonoBehaviour
{
	[SerializeField]
	TextMeshProUGUI _moneyCountText;

	[SerializeField]
	TextMeshProUGUI _toastMessageText;

	private bool _isMoneyAnimating = false;

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
	/// 소지금 변경(증가/감소) + 애니메이션을 한번에 처리
	/// </summary>
	/// <param name="delta">증가/감소 값. 감소는 음수.</param>
	/// <param name="duration">애니메이션 시간</param>
	/// <param name="clampZero">0 미만으로 내려가지 않도록 클램프할지 여부</param>
	public void ApplyMoneyChange(long delta, float duration = 1f, bool clampZero = true)
	{
		if (GameManager.Instance == null || _moneyCountText == null)
			return;

		long before = GameManager.Instance.Money;
		long target = before + delta;
		if (clampZero && target < 0)
			target = 0;

		// 실제 금액 반영 (이벤트도 여기서 발생)
		GameManager.Instance.Money = target;

		// UI 애니메이션
		AnimateMoney(before, target, duration);
	}

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

	public void SetToastMessage(string message)
	{
		_toastMessageText.text = message;
		_toastMessageText.enabled = (string.IsNullOrEmpty(message) == false);

		
	}
}
