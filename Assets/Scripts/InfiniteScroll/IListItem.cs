/// <summary>
/// I list item.
/// </summary>

using UnityEngine;

public interface IListItem
{
    /// <summary>
    /// アイテムの更新時に呼ばれる
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    void OnUpdateItem(int index, object item);

    /// <summary>
    /// アイテムが固定された時に呼ばれる
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    void OnFixedItem(int index, object item);
}
