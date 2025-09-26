using DG.Tweening;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(BoxCollider))]
public class PileBase : MonoBehaviour
{
	#region Fields
	[SerializeField]
	private int _row = 2;

	[SerializeField]
	private int _column = 2;

	[SerializeField]
	private Vector3 _size = new Vector3(0.5f, 0.1f, 0.5f);

	[SerializeField]
	protected float _dropInterval = 0.05f;
	#endregion

	protected Stack<GameObject> _objects = new Stack<GameObject>();

	public int ObjectCount => _objects.Count;
	public void AddToPile(GameObject go, bool jump = false)
	{
		// 스택에 추가한다.
		_objects.Push(go);

		// 위치를 조정한다.
		Vector3 pos = GetPositionAt(_objects.Count - 1);

		if (jump)
			go.transform.DOJump(pos, 5, 1, 0.3f);
		else
			go.transform.position = pos;
	}

	public GameObject RemoveFromPile()
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
