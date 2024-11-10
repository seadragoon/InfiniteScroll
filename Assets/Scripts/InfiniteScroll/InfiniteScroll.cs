using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class InfiniteScroll : UIBehaviour
{
	//! 方向
	public enum Direction
	{
		Vertical,
		Horizontal,
	}

	//! 作成するプレハブ
	[SerializeField]
	private RectTransform _itemPrefab = null;
	//! 生成するアイテムの数（このオブジェクトを使いまわしてループさせる）
	[SerializeField, Range(0, 30)]
	public int instantiateItemCount = 9;
	//! FIXに掛かる力
	[SerializeField]
	private int SPRING_POWER = 10;
	//! 制限スクロール速度
	[SerializeField]
	private int LIMIT_SCROLL_VELOCITY = 200;
    //! スクロールの方向
    [SerializeField]
    private Direction direction = Direction.Vertical;

	//! 初期化済みかどうか
	private bool isInitialized = false;
	//! 内部アイテムリスト
	private ItemData[] _innerItems = null;
	//! 作成されたリストアイテムのRectTransform
	private List<RectTransform> _createdItems = new List<RectTransform>();

	//! スクロール
	private FixedScrollRect _fixedScroll = null;
	//! 前フレーム位置との差分
	protected float diffPreFramePosition = 0;
	//! 現在のアイテムNo
	protected int currentItemNo = 0;
    //! 自動スクロール開始位置
    private float _autoScrollStartPosition = 0;
    //! 自動スクロール終了位置
    private float _autoScrollFinishPosition = 0;
    //! 自動スクロール中かどうか
    private bool _isAutoScroll = false;
    //! 固定されたかどうか
    private bool _isFix = false;

	//! RectTransform
	private RectTransform _rectTransform;
	public RectTransform rectTransform {
		get {
			if(_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}

	//! アンカー位置
	private float anchoredPosition
	{
		get {
			return direction == Direction.Vertical ? -rectTransform.anchoredPosition.y : rectTransform.anchoredPosition.x;
		}
		set {
			// 位置を設定
			if (direction == Direction.Vertical) {
				_rectTransform.anchoredPosition = new Vector2 (0, -value);
			} else { 
				_rectTransform.anchoredPosition = new Vector2 (value, 0);
			}
		}
	}

	//! スクロール速度
	private float scrollVelocity
	{
		get
        {
			return (direction == Direction.Vertical) ? -_fixedScroll.velocity.y : _fixedScroll.velocity.x;
        }
	}

	//! スクロールベース位置
	private float basePosition {
		get {
			float length = (direction == Direction.Vertical) ? 
				rectTransform.sizeDelta.y:
				rectTransform.sizeDelta.x;
			// スクロールの中心からアンカーが上なのでアイテムの半分だけ上にずらす
			return (length / 2)/* center */ - (itemSize / 2);
		}
	}

	//! アイテムの大きさを返却
	private float _itemSize = -1;
	public float itemSize {
		get {
			if(_itemPrefab != null && _itemSize == -1) {
				_itemSize = direction == Direction.Vertical ? _itemPrefab.sizeDelta.y : _itemPrefab.sizeDelta.x;
			}
			return _itemSize;
		}
	}

	//! アイテム情報クラス
	private class ItemData
	{
		private int _useItemIndex;	//!< 作成されたアイテムのうち、どのインデックスを使用しているか

		public int index;	//!< インデックス
		public object data;	//!< アイテムに設定されるデータ

		//! 作成されたアイテムのインデックス
		public int useItemIndex
		{
			get { return _useItemIndex; }
			set{
				_useItemIndex = value;
			}
		}
	}


	// ------------------- 定義ここまで ------------------- //

	//! アイテムリストを作成
	public void Create<T>( IList<T> items )
	{
		if (_itemPrefab == null) return;
		if (items == null) return;

		// 初期化
		Initialize();

		// 内部リストアイテムを設定
		ItemData[] newItems = new ItemData[items.Count];
		for (int i = 0, len = items.Count; i < len; i++)
		{
			ItemData item = new ItemData ();
			item.index = i;
			item.data = items [i];
			newItems [i] = item;
		}
		_innerItems = newItems;

		// 最初に表示するリストを作成
		for(int i = 0; i < instantiateItemCount; i++) {
			var obj = Instantiate(_itemPrefab) as RectTransform;
			// RectTransformを初期化
			obj.SetParent(transform, false);
			obj.name = i.ToString();
			obj.anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -itemSize * i) : new Vector2(itemSize * i, 0);

			// アイテムインターフェースを継承したクラスが付いてるオブジェクトを取得
			var item = (MonoBehaviour)obj.GetComponent(typeof(IListItem));
			if (item != null) {
				_innerItems [i].useItemIndex = _createdItems.Count;
			}

			// 作成したアイテムリストに追加
			_createdItems.Add (obj);

			// 表示
			obj.gameObject.SetActive(true);

			// 更新イベントの呼び出し
			OnItemShow(_innerItems[i]);
        }

        // 初期位置設定
        Scroll(0);

        // 最初の更新
        LateUpdate ();
	}

	//! 初期化
	private void Initialize()
	{
		// 初期化済みの確認
		if( isInitialized ) return;

		// スクロールを初期化
		_fixedScroll = GetComponentInParent<FixedScrollRect>();
		_fixedScroll.horizontal = direction == Direction.Horizontal;
		_fixedScroll.vertical = direction == Direction.Vertical;
		_fixedScroll.content = rectTransform;
		_fixedScroll.movementType = ScrollRect.MovementType.Unrestricted;

		// スクロールイベント登録
		_fixedScroll.onBeginDrag.RemoveAllListeners();
        _fixedScroll.onBeginDrag.AddListener(OnBeginDrag);

        // アイテムを非表示に
        _itemPrefab.gameObject.SetActive(false);

		// 初期化完了
		isInitialized = true;
    }

    //! FixedScrollRectから呼ばれるように登録する関数
    private void OnBeginDrag()
    {
        _isFix = false;
        _isAutoScroll = false;
    }

    //! Unity LateUpdate
    private void LateUpdate()
	{
		// アイテム存在確認
		if (_createdItems.Count <= 0) {
			return;
		}

		// 手前に引く（進む）
		while(anchoredPosition - diffPreFramePosition  < -itemSize * 2)
		{
			diffPreFramePosition -= itemSize;

			// 実際のオブジェクト情報を取得
			int objIndex = GetActualIndex(currentItemNo);

			// 位置移動
			var pos = (itemSize * instantiateItemCount) + (itemSize * currentItemNo);
			_createdItems [objIndex].anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -pos) : new Vector2(pos, 0);

			// アイテムデータを更新
			int nextNo = currentItemNo + instantiateItemCount;
            int itemIndex = GetInnerIndex(nextNo);
            _innerItems[itemIndex].useItemIndex = objIndex;
			_innerItems[itemIndex].index = nextNo;

			// 更新通知
			OnItemShow(_innerItems[itemIndex]);

			currentItemNo++;
		}

		// 奥に引く（戻る）
		while(anchoredPosition - diffPreFramePosition > 0)
		{
			diffPreFramePosition += itemSize;

            // 実際のオブジェクト情報を取得
            int objIndex = GetActualIndex(currentItemNo + instantiateItemCount - 1);

			// 位置移動
			var pos = itemSize * currentItemNo;
			_createdItems [objIndex].anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -pos): new Vector2(pos, 0);

			// アイテムデータを更新
            int itemIndex = GetInnerIndex(currentItemNo);
            _innerItems[itemIndex].useItemIndex = objIndex;
			_innerItems[itemIndex].index = currentItemNo;

			// 更新通知
			OnItemShow(_innerItems[itemIndex]);

			currentItemNo--;
		}

		if (!_fixedScroll.isDrag && !_isFix)
        {
            // ドラッグ終了後、自動スクロール位置を決定する
            if (!_isAutoScroll && Mathf.Abs(scrollVelocity) < LIMIT_SCROLL_VELOCITY)
            {
                // スクロールの動きを止める
                _fixedScroll.StopMovement();

                // 現在位置から次の位置を決定する
                int targetIndex = GetCurrentIndex();
                _autoScrollFinishPosition = GetTargetPosition(targetIndex);
                _autoScrollStartPosition = anchoredPosition;
                _isAutoScroll = true;
            }

            // 自動スクロールONの場合、ターゲット位置に移動する
            if (_isAutoScroll)
            {
                float diff = _autoScrollFinishPosition - _autoScrollStartPosition;
				if (diff > 0)
				{
					anchoredPosition += diff * Time.deltaTime * SPRING_POWER;
					if (_autoScrollFinishPosition < anchoredPosition)
					{
						Scroll(GetCurrentIndex());
					}
				}
				else
				{
					anchoredPosition -= -diff * Time.deltaTime * SPRING_POWER;
					if (_autoScrollFinishPosition > anchoredPosition)
					{
						Scroll(GetCurrentIndex());
					}
				}
            }
        }
    }

	//! スクロールを指定位置に移動する
	public void Scroll(int index)
	{
		// スクロールの動きを止める
		_fixedScroll.StopMovement ();
		// 目標位置に移動する
		anchoredPosition = basePosition - (itemSize * index);
		// FIX
		_isFix = true;
		// 自動スクロールも停止
		_isAutoScroll = false;
        // アイテム固定されたイベント発火
        int itemIndex = GetInnerIndex(GetCurrentIndex());
        OnItemFixed(_innerItems[itemIndex]);
	}
	//! インデックスをアンカー位置から求める
	public int GetCurrentIndex()
	{
		float rough = (basePosition - anchoredPosition) / itemSize;
		return (int)Mathf.Round(rough);
	}
	//! 現在のアイテムを取得する
	public T GetCurrentItem<T>()
	{
		int itemIndex = GetInnerIndex(GetCurrentIndex());

		return (T)_innerItems [itemIndex].data;
	}


	//! 対象インデックスの位置を取得
	private float GetTargetPosition(int index)
	{
		return basePosition - (itemSize * index);
    }
	//! 指定された数値の配列インデックスを返す
	private int GetArrayIndex(int number, int length)
	{
		if (number < 0) {
			int remain = Mathf.Abs (number) % length;
			return (remain == 0) ? 0 : length - Mathf.Abs (number) % length;
		} else {
			return number % length;
		}
    }
    //! 実際の配列の指定番目のインデックスを返す
    private int GetActualIndex(int number)
    {
		return GetArrayIndex(number, instantiateItemCount);
    }
    //! 内部配列の指定番目のインデックスを返す
    private int GetInnerIndex(int number)
    {
        return GetArrayIndex(number, _innerItems.Length);
    }
    //! アイテムが表示される際のイベント
    private void OnItemShow(ItemData item)
	{
		// インターフェースの取得
		IListItem obj = GetListItemInterface(item);

		if (obj != null) {
			obj.OnUpdateItem (item.index, item.data);
		}
	}
	private void OnItemFixed(ItemData item)
    {
        // インターフェースの取得
        IListItem obj = GetListItemInterface(item);

        if (obj != null)
        {
            obj.OnFixedItem(item.index, item.data);
        }
    }
	//! アイテムが使用しているBehaviourを取得
	private IListItem GetListItemInterface(ItemData item){
		return _createdItems [item.useItemIndex].GetComponent<IListItem> ();
	}
}
