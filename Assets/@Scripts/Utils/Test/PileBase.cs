using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PileBase : MonoBehaviour
{
    [SerializeField]
    private int _row = 2;

    [SerializeField]
    private int _column = 2;

    [SerializeField]
    private Vector3 _size = new Vector3(0.5f, 0.1f, 0.5f);

    [SerializeField]
    private float _dropInterval = 0.05f;

    protected Stack<GameObject> _objects = new Stack<GameObject>();

    public int ObjectCount => _objects.Count;

    //TEST
    public GameObject prefab;

    private void Start()
    {
        StartCoroutine(CoSpawnBurger());
    }

    public void AddToPile(GameObject go)
    {
        // 스택에 추가한다
        _objects.Push(go);

        // 위치를 조정한다
        go.transform.position = GetPositionAt(_objects.Count - 1);
    }

    public GameObject RemoveFromPile()
    {
        if (_objects.Count == 0)
            return  null;

        return _objects.Pop();
    }

    // TEST
    IEnumerator CoSpawnBurger()
    {
        for(int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.2f);

            GameObject go = GameObject.Instantiate(prefab);
            AddToPile(go);
        }
        for (int i =0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.1f);
            GameObject go = RemoveFromPile();
            if(go != null)
            {
                GameObject.Destroy(go);
            }
        }
    }


    private Vector3 GetPositionAt(int pileidx)
    {
        Vector3 offset = new Vector3((_row - 1) * _size.x / 2, 0, (_column - 1) * _size.z / 2);
        Vector3 startPos = transform.position- offset;

        int row = (pileidx / _row) % _column;
        int column = pileidx % _row;
        int height = pileidx/(_row * _column);

        float x = startPos.x + column * _size.x;
        float y = startPos.y + height * _size.y;
        float z = startPos.z + row * _size.z;

        return new Vector3(x, y, z);
    }

    #region Editor
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 offset = new Vector3((_row - 1) * _size.x / 2, 0, (_column - 1) * _size.z / 2);
        Vector3 startPos = transform.position - offset; // 0번 칸의 위치

        Gizmos.color = Color.yellow;

        for (int r = 0; r < _row; r++)
        {
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
}
