/// <summary>
/// I list item.
/// </summary>

using UnityEngine;

public interface IListItem
{
    /// <summary>
    /// アイテムの初期化時に呼ばれる
    /// </summary>
    /// <param name="totalIndex"></param>
    /// <param name="itemIndex"></param>
    /// <param name="item"></param>
    void OnInitItem(int totalIndex, int itemIndex, object item);

    /// <summary>
    /// アイテムの更新時に呼ばれる
    /// </summary>
    /// <param name="totalIndex"></param>
    /// <param name="itemIndex"></param>
    /// <param name="item"></param>
    void OnUpdateItem(int totalIndex, int itemIndex, object item);

    /// <summary>
    /// アイテムが固定された時に呼ばれる
    /// </summary>
    /// <param name="totalIndex"></param>
    /// <param name="itemIndex"></param>
    /// <param name="item"></param>
    void OnFixedItem(int totalIndex, int itemIndex, object item);
}
