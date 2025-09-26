using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_OrderBubble : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI _countText;

	private int _count = 0;
	public int Count 
	{
		get { return _count; }
		set
		{
			_count = value;
			RefreshUI();
		}
	}

	public void RefreshUI()
	{
		_countText.text = _count.ToString();
	}
}
