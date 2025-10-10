using System.Collections;
using UnityEngine;

public class TrashPile : PileBase
{
    public void Awake()
    {
        _size = new Vector3(0.5f, 0.1f, 0.5f);
        _objectType = Define.EObjectType.Trash;

    }
}
