using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemControllerInfinite : UIBehaviour, IListItem
{
	public class Data
	{
		public int index;
	}

	[SerializeField]
	private Text m_Text = null;

	public void OnUpdateItem(int index, object item)
	{
		Data data = item as Data;

		m_Text.text = data.index.ToString ();
		//m_Text.text = index.ToString ();

		Debug.Log("index : " + data.index);
	}
}
