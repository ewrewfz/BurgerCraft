using System.Collections;
using UnityEngine;

public class MoneyPile : PileBase
{
    public void Awake()
    {
        _size = new Vector3(0.5f, 0.3f, 0.5f);
        _objectType = Define.EObjectType.Money;
    }

}
