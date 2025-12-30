using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

#region Pool
internal class Pool
{
	private GameObject _prefab;
	private IObjectPool<GameObject> _pool;
	private Transform _root;

	public Pool(GameObject prefab, Transform root = null)
	{
		_prefab = prefab;
		_root = root;
		_pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy);
	}

	private Transform Root
	{
		get
		{
			if (_root == null)
			{
				GameObject go = new GameObject() { name = $"@{_prefab.name}Pool" };
				_root = go.transform;
			}

			return _root;
		}
	}

	public void Push(GameObject go)
	{
		// null 체크
		if (go == null)
			return;
			
		// 활성화되어 있으면 비활성화 후 풀에 반환
		if (go.activeSelf)
		{
			go.SetActive(false);
		}
		
		// 이미 풀에 반환된 오브젝트인지 확인 (비활성화되어 있고 부모가 풀 루트인 경우)
		// ObjectPool의 내부 상태를 직접 확인할 수 없으므로, 
		// Release 호출 시 예외가 발생하면 무시
		try
		{
			_pool.Release(go);
		}
		catch (System.InvalidOperationException)
		{
			// 이미 풀에 반환된 경우 무시
		}
	}

	public GameObject Pop()
	{
		return _pool.Get();
	}

	#region Funcs
	private GameObject OnCreate()
	{
		GameObject go = GameObject.Instantiate(_prefab);
		go.transform.SetParent(Root);
		go.name = _prefab.name;
		
		// 팝업인 경우 Canvas의 sortOrder 설정
		if (_root != null) // 팝업은 _root가 PopupPool
		{
			Canvas canvas = go.GetComponent<Canvas>();
			if (canvas != null)
			{
				// 다른 팝업들의 최대 sortOrder를 찾아서 +1
				int maxSortOrder = GetMaxPopupSortOrder();
				canvas.sortingOrder = maxSortOrder + 1;
			}
		}
		
		return go;
	}
	
	private int GetMaxPopupSortOrder()
	{
		if (_root == null)
			return 0;
		
		int maxOrder = 0;
		Canvas[] canvases = _root.GetComponentsInChildren<Canvas>(true);
		foreach (Canvas canvas in canvases)
		{
			if (canvas.sortingOrder > maxOrder)
				maxOrder = canvas.sortingOrder;
		}
		return maxOrder;
	}

	private void OnGet(GameObject go)
	{
		go.SetActive(true);
		
		// 팝업인 경우 Canvas의 sortOrder를 최상단으로 설정
		if (_root != null) // 팝업은 _root가 PopupPool
		{
			Canvas canvas = go.GetComponent<Canvas>();
			if (canvas != null)
			{
				// 다른 팝업들의 최대 sortOrder를 찾아서 +1
				int maxSortOrder = GetMaxPopupSortOrder();
				canvas.sortingOrder = maxSortOrder + 1;
			}
		}
	}

	private void OnRelease(GameObject go)
	{
		go.SetActive(false);
	}

	private void OnDestroy(GameObject go)
	{
		GameObject.Destroy(go);
	}
	#endregion
}
#endregion

public class PoolManager : Singleton<PoolManager>
{
	private Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

	public GameObject Pop(GameObject prefab)
	{
		if (_pools.ContainsKey(prefab.name) == false)
			CreatePool(prefab);

		return _pools[prefab.name].Pop();
	}

	public bool Push(GameObject go)
	{
		if (_pools.ContainsKey(go.name) == false)
			return false;

		_pools[go.name].Push(go);
		return true;
	}

	public void Clear()
	{
		_pools.Clear();
	}

	private void CreatePool(GameObject original)
	{
		// 팝업인지 확인 (이름에 "Popup"이 포함되어 있거나 UI_SoundSetting 같은 팝업 UI면 팝업으로 간주)
		bool isPopup = original.name.Contains("Popup") || original.name == "UI_SoundSetting";
		
		// 팝업이면 PopupPool에 직접 배치
		Transform root = isPopup ? GetPopupPool() : null;
		Pool pool = new Pool(original, root);
		_pools.Add(original.name, pool);
	}

	/// <summary>
	/// Popup을 관리하는 Pool
	/// </summary>
	public Transform GetPopupPool()
	{
		GameObject popupPool = GameObject.Find("@PopupPool");
		if (popupPool == null)
		{
			popupPool = new GameObject("@PopupPool");
		}
		return popupPool.transform;
	}
}
