using System.Collections;
using UnityEngine;

public class MoneyPile : PileBase
{
    public void Awake()
    {
        _size = new Vector3(0.2f, 0.1f, 0.2f);
        _objectType = Define.EObjectType.Money;

    }
}
