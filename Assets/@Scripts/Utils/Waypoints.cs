using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Waypoints : MonoBehaviour
{
	public int GetPointCount()
	{
		return transform.childCount;
	}

	public Transform GetPoint(int index)
	{
		return transform.GetChild(index);
	}

	public List<Transform> GetPoints()
	{
		List<Transform> points = new List<Transform>();

		foreach (Transform child in transform)
			points.Add(child);

		return points;
	}

	#region Editor
#if UNITY_EDITOR
	[SerializeField]
	private Color _color = Color.yellow;

	[SerializeField]
	private float _pointSize = 0.2f;

	void OnDrawGizmos()
	{
		Gizmos.color = _color;

		foreach (Transform child in transform)
			Gizmos.DrawSphere(child.position, _pointSize);

		for (int i = 0; i < transform.childCount - 1; i++)
		{
			Transform start = transform.GetChild(i);
			Transform end = transform.GetChild(i + 1);
			Gizmos.DrawLine(start.position, end.position);
		}
	}
#endif
	#endregion
}
