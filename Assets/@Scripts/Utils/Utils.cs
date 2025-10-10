using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;


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
}