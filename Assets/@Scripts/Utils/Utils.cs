using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using DG.Tweening;
using TMPro;


public static class Utils
{
	public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
	{
		T component = go.GetComponent<T>();
		if (component == null)
			component = go.AddComponent<T>();

		return component;
	}

	public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
	{
		Transform transform = FindChild<Transform>(go, name, recursive);
		if (transform == null)
			return null;

		return transform.gameObject;
	}

	public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Component
	{
		if (go == null)
			return null;

		if (recursive == false)
		{
			for (int i = 0; i < go.transform.childCount; i++)
			{
				Transform transform = go.transform.GetChild(i);
				if (string.IsNullOrEmpty(name) || transform.name == name)
				{
					T component = transform.GetComponent<T>();
					if (component != null)
						return component;
				}
			}
		}
		else
		{
			foreach (T component in go.GetComponentsInChildren<T>())
			{
				if (string.IsNullOrEmpty(name) || component.gameObject.name == name)
					return component;
			}
		}

		return null;
	}

	public static T ParseEnum<T>(string value)
	{
		return (T)Enum.Parse(typeof(T), value, true);
	}

	public static EObjectType GetTrayObjectType(Transform t)
	{
		switch (t.gameObject.tag)
		{
			case "Trash":
				return EObjectType.Trash;
			case "Burger":
				return EObjectType.Burger;
			case "Money":
				return EObjectType.Money;
		}

		return EObjectType.None;
	}

	public static string GetMoneyText(long money)
	{
		if (money < 1000) return money.ToString();
		if (money < 1000000) return (money / 1000f).ToString("0.##") + "k"; // (k)
		if (money < 1000000000) return (money / 1000000f).ToString("0.##") + "m"; // (m)
		if (money < 1000000000000) return (money / 1000000000f).ToString("0.##") + "b"; // (b)
		return (money / 1000000000000f).ToString("0.##") + "t"; // (t)
	}

	/// <summary>
	/// 튜토리얼 전용: 토스트 메시지를 일정 시간 표시한 후 페이드 아웃으로 사라지게 합니다.
	/// </summary>
	/// <param name="message">표시할 메시지</param>
	/// <param name="displayDuration">표시 시간 (초)</param>
	/// <param name="fadeOutDuration">페이드 아웃 시간 (초)</param>
	/// <returns>코루틴 (yield return으로 사용)</returns>
	public static IEnumerator ShowTutorialToastMessage(string message, float displayDuration = 3f, float fadeOutDuration = 0.5f)
	{
		if (GameManager.Instance == null || GameManager.Instance.GameSceneUI == null)
			yield break;

		// 메시지 표시
		GameManager.Instance.GameSceneUI.SetToastMessage(message);

		// 표시 시간 대기
		yield return new WaitForSeconds(displayDuration);

		// 페이드 아웃 효과
		var toastText = GameManager.Instance.GameSceneUI.GetToastMessageText();
		if (toastText != null)
		{
			// DOTween을 사용한 페이드 아웃
			toastText.DOFade(0f, fadeOutDuration)
				.SetEase(Ease.OutQuad)
				.OnComplete(() =>
				{
					// 페이드 아웃 완료 후 메시지 지우기
					GameManager.Instance.GameSceneUI.SetToastMessage("");
					// 알파값 복원 (다음 메시지를 위해)
					var color = toastText.color;
					color.a = 1f;
					toastText.color = color;
				});
		}
		else
		{
			// 페이드 아웃이 불가능하면 그냥 지우기
			GameManager.Instance.GameSceneUI.SetToastMessage("");
		}
	}

	/// <summary>
	/// 금액 증감(증가/감소)과 UI 애니메이션을 한 번에 처리.
	/// delta: 증가/감소값(감소는 음수), clampZero: 0 미만 방지 여부, animate: UI 애니메이션 실행 여부
	/// </summary>
	public static void ApplyMoneyChange(long delta, float duration = 1f, bool clampZero = true, bool animate = true)
	{
		if (GameManager.Instance == null)
			return;

		long before = GameManager.Instance.Money;
		long target = before + delta;
		if (clampZero && target < 0)
			target = 0;

		// 실제 금액 반영 (이벤트도 여기서 발생)
		GameManager.Instance.Money = target;

		// UI 애니메이션
		if (animate && GameManager.Instance.GameSceneUI != null)
		{
			GameManager.Instance.GameSceneUI.AnimateMoney(before, target, duration);
		}
	}
}