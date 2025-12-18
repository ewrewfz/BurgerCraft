using DG.Tweening;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

[RequireComponent(typeof(BoxCollider))]
public class PileBase : MonoBehaviour
{
    #region Fields
    [SerializeField]
    protected int _row = 2;

    [SerializeField]
    protected int _column = 2;

    [SerializeField]
    protected Vector3 _size = new Vector3(0.5f, 0.1f, 0.5f);

    [SerializeField]
    protected float _dropInterval = 0.05f;
    #endregion

    #region Contents
    protected EObjectType _objectType = EObjectType.None;

    public void SpawnObject()
    {
        switch (_objectType)
        {
            case EObjectType.Burger:
                {
                    GameObject go = GameManager.Instance.SpawnBurger();
                    AddToPile(go, false);
                }
                break;
            case EObjectType.Money:
                {
                    GameObject go = GameManager.Instance.SpawnMoney();
                    AddToPile(go, false);
                }
                break;
            case EObjectType.Trash:
                {
                    GameObject go = GameManager.Instance.SpawnTrash();
                    AddToPile(go, false);
                }
                break;
        }
    }

    public void SpawnObjectWithJump(Vector3 spawnPos)
    {
        switch (_objectType)
        {
            case EObjectType.Burger:
                {
                    GameObject go = GameManager.Instance.SpawnBurger();
                    go.transform.position = spawnPos;
                    AddToPile(go, true);
                }
                break;
            case EObjectType.Money:
                {
                    GameObject go = GameManager.Instance.SpawnMoney();
                    go.transform.position = spawnPos;
                    AddToPile(go, true);
                }
                break;
            case EObjectType.Trash:
                {
                    GameObject go = GameManager.Instance.SpawnTrash();
                    go.transform.position = spawnPos;
                    AddToPile(go, true);
                }
                break;
        }
    }

    public void DespawnObject()
    {
        if (ObjectCount == 0)
            return;

        switch (_objectType)
        {
            case EObjectType.Burger:
                {
                    GameObject go = RemoveFromPile();
                    GameManager.Instance.DespawnBurger(go);
                }
                break;
            case EObjectType.Money:
                {
                    GameObject go = RemoveFromPile();
                    GameManager.Instance.DespawnMoney(go);
                }
                break;
            case EObjectType.Trash:
                {
                    GameObject go = RemoveFromPile();
                    GameManager.Instance.DespawnTrash(go);
                }
                break;
        }
    }

    public void DespawnObjectWithJump(Vector3 destPos, Action onDespawnCallback = null)
    {
        if (ObjectCount == 0)
            return;

        switch (_objectType)
        {
            case EObjectType.Burger:
                {
                    GameObject go = RemoveFromPile();
                    go.transform
                        .DOJump(destPos, 3, 1, 0.3f)
                        .OnComplete(() =>
                        {
                            GameManager.Instance.DespawnBurger(go);
                            onDespawnCallback?.Invoke();
                        });
                }
                break;
            case EObjectType.Money:
                {
                    GameObject go = RemoveFromPile();
                    go.transform
                        .DOJump(destPos, 3, 1, 0.3f)
                        .OnComplete(() =>
                        {
                            GameManager.Instance.DespawnMoney(go);
                            onDespawnCallback?.Invoke();
                        });
                }
                break;
            case EObjectType.Trash:
                {
                    GameObject go = RemoveFromPile();
                    go.transform
                        .DOJump(destPos, 3, 1, 0.3f)
                        .OnComplete(() =>
                        {
                            GameManager.Instance.DespawnTrash(go);
                            onDespawnCallback?.Invoke();
                        });
                }
                break;
        }
    }

    // Tray -> Pile
    public void TrayToPile(TrayController tray)
    {
        if (tray.CurrentTrayObjectType == EObjectType.None)
            return;
        if (tray.CurrentTrayObjectType != EObjectType.None && _objectType != tray.CurrentTrayObjectType)
            return;
        Transform t = tray.RemoveFromTray();
        if (t == null)
            return;

        t.rotation = Quaternion.identity;

        AddToPile(t.gameObject, jump: true);
    }

    // Pile -> Tray
    public void PileToTray(TrayController tray)
    {
        if (_objectType == EObjectType.None)
            return;
        if (tray.CurrentTrayObjectType != EObjectType.None && _objectType != tray.CurrentTrayObjectType)
            return;

        GameObject go = RemoveFromPile();
        if (go == null)
            return;

        tray.AddToTray(go.transform);
    }
    #endregion

    #region Pile
    protected Stack<GameObject> _objects = new Stack<GameObject>();

    public int ObjectCount => _objects.Count;
    private void AddToPile(GameObject go, bool jump = false)
    {
        // 스택에 추가한다.
        _objects.Push(go);

        // 부모를 Pile로 설정 (버거는 Pile에서 관리)
        go.transform.SetParent(transform);

        // 위치를 조정한다.
        Vector3 pos = GetPositionAt(_objects.Count - 1);

        if (jump)
            go.transform.DOJump(pos, 3, 1, 0.3f);
        else
            go.transform.position = pos;
    }

    private GameObject RemoveFromPile()
    {
        if (_objects.Count == 0)
            return null;

        // 스택에서 제거한다.
        return _objects.Pop();
    }

    private Vector3 GetPositionAt(int pileIndex)
    {
        Vector3 offset = new Vector3((_row - 1) * _size.x / 2, 0, (_column - 1) * _size.z / 2);
        Vector3 startPos = transform.position - offset;

        int row = (pileIndex / _row) % _column;
        int column = pileIndex % _row;
        int height = pileIndex / (_row * _column);

        float x = startPos.x + column * _size.x;
        float y = startPos.y + height * _size.y;
        float z = startPos.z + row * _size.z;

        return new Vector3(x, y, z);
    }
    #endregion

    #region Editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 offset = new Vector3((_row - 1) * _size.x / 2, 0, (_column - 1) * _size.z / 2);
        Vector3 startPos = transform.position - offset; // 0번 칸의 위치.

        Gizmos.color = Color.yellow;

        for (int r = 0; r < _row; r++)
        {
            for (int c = 0; c < _column; c++)
            {
                Vector3 center = startPos + new Vector3(r * _size.x, _size.y / 2, c * _size.z);
                Gizmos.DrawWireCube(center, _size);
            }
        }
    }
#endif
    #endregion
}
