using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class BurgerPile : PileBase
{
    public void Awake()
    {
        _size = new Vector3(0.5f, 0.3f, 0.5f);
        _objectType = Define.EObjectType.Burger;
    }
    
    /// <summary>
    /// 주문 번호가 있는 버거를 생성합니다.
    /// </summary>
    public GameObject SpawnObjectWithOrderNumber()
    {
        GameObject go = GameManager.Instance.SpawnBurger();
        // 버거를 Pile 위치에 바로 추가 (점프 없이)
        // SpawnBurger()가 어디에 스폰하든 AddToPile에서 위치를 재설정하므로 문제 없음
        if (go != null)
        {
            AddToPile(go, false);
        }
        return go;
    }
    
    /// <summary>
    /// 주문 번호가 일치하는 버거만 트레이로 이동합니다.
    /// </summary>
    public bool PileToTrayWithOrderNumber(TrayController tray, string orderNumber)
    {
        if (_objectType == EObjectType.None)
            return false;
        if (tray.CurrentTrayObjectType != EObjectType.None && _objectType != tray.CurrentTrayObjectType)
            return false;
        if (ObjectCount == 0)
            return false;
        
        // 스택에서 주문 번호가 일치하는 버거 찾기 (LIFO 순서로 검색)
        GameObject matchingBurger = null;
        Stack<GameObject> tempStack = new Stack<GameObject>();
        
        // 스택을 순회하면서 주문 번호가 일치하는 버거 찾기
        while (_objects.Count > 0)
        {
            GameObject burger = _objects.Pop();
            tempStack.Push(burger);
            
            BurgerOrderNumber orderNumberComponent = burger.GetComponent<BurgerOrderNumber>();
            if (orderNumberComponent != null && orderNumberComponent.MatchesOrderNumber(orderNumber))
            {
                matchingBurger = burger;
                break;
            }
        }
        
        // 나머지 버거들을 다시 스택에 넣기
        while (tempStack.Count > 0)
        {
            GameObject burger = tempStack.Pop();
            if (burger != matchingBurger)
            {
                _objects.Push(burger);
            }
        }
        
        // 일치하는 버거가 없으면 false 반환
        if (matchingBurger == null)
            return false;
        
        // 트레이에 추가
        tray.AddToTray(matchingBurger.transform);
        return true;
    }
}
