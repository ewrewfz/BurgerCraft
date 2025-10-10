using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class TrayController : MonoBehaviour
{
	[SerializeField]
	private Vector2 _shakeRange = new Vector2(0.8f, 0.4f);

	[SerializeField]
	private float _bendFactor = 0.1f;

	[SerializeField]
	private float _itemHeight = 0.5f;

    private EObjectType _objectType = EObjectType.None;
    public EObjectType CurrentTrayObjectType
    {
        get { return _objectType; }
        set
        {
            _objectType = value;
            switch (value)
            {
                case EObjectType.Trash:
                    _itemHeight = 0.2f;
                    break;
                case EObjectType.Burger:
                    _itemHeight = 0.5f;
                    break;
            }
        }
    }

    public int ItemCount => _items.Count; // 쟁반 위에 들고 있는 아이템 개수.
	public int ReservedCount => _reserved.Count; // 쟁반 위로 이동중.
	public int TotalItemCount => _reserved.Count + _items.Count; // 쟁반 위로 이동중인 아이템을 포함한 전체 개수.

	private HashSet<Transform> _reserved = new HashSet<Transform>();
	private List<Transform> _items = new List<Transform>();

	private MeshRenderer _meshRenderer;
    private StickmanController _owner;
    public bool IsPlayer = false;

    public bool Visible
    {
        set { if (_meshRenderer != null) _meshRenderer.enabled = value; _owner?.UpdateAnimation(); }
        get { return (_meshRenderer != null) ? _meshRenderer.enabled : false; }
    }

    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _owner = transform.root.GetComponent<StickmanController>();
        Visible = false;
    }

    // 휘는거 조정.
    private void Update()
    {
        Visible = (_items.Count > 0);

        if (_items.Count == 0)
            return;

        Vector3 moveDir = Vector3.zero;

        if (IsPlayer)
        {
            Vector3 dir = GameManager.Instance.JoystickDir;
            moveDir = new Vector3(dir.x, 0, dir.y);
            moveDir = (Quaternion.Euler(0, 45, 0) * moveDir).normalized;
        }

        _items[0].position = transform.position;
        _items[0].rotation = transform.rotation;

        for (int i = 1; i < _items.Count; i++)
        {
            float rate = Mathf.Lerp(_shakeRange.x, _shakeRange.y, i / (float)_items.Count);

            _items[i].position = Vector3.Lerp(_items[i].position, _items[i - 1].position + (_items[i - 1].up * _itemHeight), rate);
            _items[i].rotation = Quaternion.Lerp(_items[i].rotation, _items[i - 1].rotation, rate);

            if (moveDir != Vector3.zero)
                _items[i].rotation *= Quaternion.Euler(-i * _bendFactor * rate, 0, 0);
        }
    }

    public void AddToTray(Transform child)
    {
        // 운반하는 물체 종류 추적을 위해.
        EObjectType objectType = Utils.GetTrayObjectType(child);
        if (objectType == EObjectType.None)
            return;

        // 다른 종류의 아이템이 있으면 수집 불가.
        if (CurrentTrayObjectType != EObjectType.None && CurrentTrayObjectType != objectType)
            return;

        CurrentTrayObjectType = objectType;

        _reserved.Add(child);

        Vector3 dest = transform.position + Vector3.up * TotalItemCount * _itemHeight;

        child.DOJump(dest, 5, 1, 0.3f)
            .OnComplete(() =>
            {
                _reserved.Remove(child);
                _items.Add(child);
            });
    }

    public Transform RemoveFromTray()
    {
        if (ItemCount == 0 || ReservedCount > 0)
            return null;

        Transform item = _items.Last();
        if (item == null)
            return null;

        _items.RemoveAt(_items.Count - 1);

        // 운반하는 물체 종류 추적을 위해.
        if (TotalItemCount == 0)
            CurrentTrayObjectType = EObjectType.None;

        return item;
    }

}
