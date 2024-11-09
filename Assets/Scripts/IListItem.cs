/// <summary>
/// I list item.
/// </summary>

using UnityEngine;

public interface IListItem
{
	/**
	 * アイテムの更新時に呼ばれる
	 */
	void OnUpdateItem(int index, object item);
}
