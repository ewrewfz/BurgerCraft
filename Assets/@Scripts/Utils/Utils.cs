using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Utils
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();
        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false, bool includeInactive = false)
    {
        Transform transform = FindChild<Transform>(go, name, recursive, includeInactive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false, bool includeInactive = false) where T : UnityEngine.Object
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
            foreach (T component in go.GetComponentsInChildren<T>(includeInactive))
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    /// <summary>
    /// 해당타입의 Componet를 가지고 있는 자식 찾아서 배열로 반환
    /// !!! 배열요소중 null이 존재할 수 있다. !!!
    /// </summary>
    /// <typeparam name="T">찾을 자식타입</typeparam>
    /// <param name="go">부모</param>
    /// <returns></returns>
    public static T[] FindChildren<T>(GameObject go) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        T[] tempArray = new T[go.transform.childCount];
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform transform = go.transform.GetChild(i);
            {
                T component = transform.GetComponent<T>();
                if (component != null)
                {
                    tempArray[i] = component;
                }
            }
        }
        return tempArray;
    }

    /// <summary>
    /// 해당타입의 Componet를 가지고 있는 자식 찾아서 List로 반환
    /// </summary>
    /// <typeparam name="T">찾을 자식타입</typeparam>
    /// <param name="go"></param>
    /// <returns></returns>
    public static List<T> FindChildrenList<T>(GameObject go) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        List<T> tempList = new List<T>();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            Transform transform = go.transform.GetChild(i);
            {
                T component = transform.GetComponent<T>();
                if (component != null)
                {
                    tempList.Add(component);
                }
            }
        }
        return tempList;
    }

    /// <summary>
    /// 해당 컴포넌트를 가진 모든 자식을 모아 리스트로 반환
    /// GetComponentsInChildren 이 자신의 컴포넌트도 포함시키셔 따로 만듬
    /// </summary>
    /// <typeparam name="T">해당 컴포넌트</typeparam>
    /// <param name="go">부모</param>
    /// <returns></returns>
    public static List<T> FindChildrenListExceptSelf<T>(GameObject go) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        T[] array = go.GetComponentsInChildren<T>();
        List<T> tempList = new List<T>(array);
        T selfComoponent = go.GetComponent<T>();
        tempList.Remove(selfComoponent);
        return tempList;
    }

    /// <summary>
    /// 자식만(손자이하제외) 찾아 GameObject배열로 반환하기
    /// </summary>
    /// <param name="parent">부모</param>
    /// <returns></returns>
    public static GameObject[] FindChildren(GameObject parent)
    {
        if (parent == null)
            return null;

        GameObject[] tempArray = new GameObject[parent.transform.childCount];
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            tempArray[i] = parent.transform.GetChild(i).gameObject;
        }
        return tempArray;
    }

    /// <summary>
    /// 자식만(손자이하제외) 찾아 GameObjec리스토 반환하기
    /// </summary>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static List<GameObject> FindChildrenList(GameObject parent)
    {
        if (parent == null)
            return null;

        List<GameObject> tempList = new List<GameObject>();
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform transform = parent.transform.GetChild(i);
            {
                tempList.Add(transform.gameObject);
            }
        }
        return tempList;
    }

    public static string GetCurrentDate()
    {
        return DateTime.Now.ToString(("yyyy.MM.dd"));
    }

    public static string GetCurrentTime()
    {
        //return DateTime.Now.ToString(("HH:mm:ss"));
        return DateTime.Now.ToString(("HH:mm"));
    }
}
