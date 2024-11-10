using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// InfiniteScrollのItemクラス
/// </summary>
public class TestItem : UIBehaviour, IListItem
{
	public class Data
	{
		public int itemNo;
	}

	[SerializeField]
	private Text _Text = null;

	public void OnUpdateItem(int index, object item)
	{
		Data data = item as Data;

		_Text.text = data.itemNo.ToString ();
	}

    public void OnFixedItem(int index, object item)
    {
        Data data = item as Data;

        Debug.Log($"fix: index {index}, itemNo {data.itemNo}");
    }
}
