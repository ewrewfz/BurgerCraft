using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrayController : MonoBehaviour
{
    [SerializeField]
    private Vector2 _shakeRange = new Vector2(0.8f, 0.4f);

    [SerializeField]
    private float _bendFactor = 0.1f;

    [SerializeField]
    private float _itemHeight = 0.5f;

    public int ItemCount=> _items.Count; // 쟁반 위에 들고 있는 아이템 개수.
    public int ReservedCount => _reserved.Count; // 쟁반 위로 이동 중
    public int TotalItemCount => _reserved.Count + _items.Count; // 쟁반 위로 이동중인 아이템을 포함한 전체 개수

    private HashSet<Transform> _reserved = new HashSet<Transform>();
    private List<Transform> _items = new List<Transform>();


    public void AddToTray(Transform child)
    {
        _reserved.Add(child);

        Vector3 dest = transform.position + Vector3.up * TotalItemCount * _itemHeight;

        child.DOJump(dest, 5, 1, 0.3f).OnComplete(() =>
        {
            _reserved.Remove(child);
            _items.Add(child);
        });
    }

    public Transform RemoveFromTray()
    {
        if(ItemCount == 0) 
            return null;
        Transform item = _items.Last();

        return null;
    }
}
