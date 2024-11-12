using UnityEngine;
using UnityEngine.Events;
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
        public UnityAction<int> onClickItem;
    }

    [SerializeField]
    private Text _text;
    [SerializeField]
    private Button _button;

    private Data _data;

    protected override void Awake()
    {
        base.Awake();

        _button.onClick.AddListener(OnClick);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _button.onClick.RemoveAllListeners();
    }

    private void OnClick()
    {
        if (_data != null)
        {
            _data.onClickItem.Invoke(_data.itemNo);
        }
    }

    public void OnUpdateItem(int index, object item)
    {
        _data = item as Data;

        _text.text = _data.itemNo.ToString();
    }

    public void OnFixedItem(int index, object item)
    {
        Data data = item as Data;

        Debug.Log($"fix: index {index}, itemNo {data.itemNo}");
    }
}
