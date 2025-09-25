using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Waypoints : MonoBehaviour
{
    public int GetPointCount()
    {
        return transform.childCount;
    }

    public Transform  GetPoint(int idx)
    {
        return transform.GetChild(idx);
    }

    public List<Vector3> GetPoints()
    {
        List<Vector3> points = new List<Vector3>();


        foreach(Transform child in transform)
        {
            points.Add(child.position);
        }

        return points;
    }

    #region Editor
#if UNITY_EDITOR
    [SerializeField]
    private Color _color = Color.yellow;

    [SerializeField]
    private float _pointSize = 0.2f;

    private void OnDrawGizmos()
    {
        Gizmos.color = _color;

        foreach(Transform child in transform)
            Gizmos.DrawSphere(child.position, _pointSize);

        for(int i = 0; i < transform.childCount - 1; i++)
        {
            Transform start = transform.GetChild(i);
            Transform end = transform.GetChild(i + 1);
            Gizmos.DrawLine(start.position, end.position);
        }

    }

#endif
    #endregion
}