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

	//! イベントクラス
	[System.Serializable]
	public class OnItemPositionChange : UnityEngine.Events.UnityEvent<int, GameObject> {}

	//! 作成するプレハブ
	[SerializeField]
	private RectTransform m_ItemPrefab = null;
	//! 生成するアイテムの数（このオブジェクトを使いまわしてループさせる）
	[SerializeField, Range(0, 30)]
	public int instantateItemCount = 9;
	//! FIXに掛かる力
	[SerializeField]
	private int SPRING_POWER = 10;
	//! 制限スクロール速度
	[SerializeField]
	private int LIMIT_SCROLL_VELOCITY = 200;
	//! 決まったことにする誤差
	//[SerializeField]
	//private float FIX_TOLERANCE = 0.1f;

	//! スクロールの方向
	public Direction direction = Direction.Vertical;
	//! イベント登録用
	public OnItemPositionChange onUpdateItem = new OnItemPositionChange();

	//! 初期化済みかどうか
	private bool m_IsInitialized = false;
	//! 内部アイテムリスト
	private ItemData[] m_InnerItems = null;
	//! 作成されたリストアイテムのRectTransform
	private List<RectTransform> m_CreatedItems = new List<RectTransform>();

	//! スクロール
	private FixedScrollRect m_FixedScroll = null;
	//! 前フレーム位置との差分
	protected float diffPreFramePosition = 0;
	//! 現在のアイテムNo
	protected int currentItemNo = 0;

	//! 固定したかどうか
	private bool m_IsFix = false;
	public bool IsFix{
		get{ return m_IsFix; }
		set{ m_IsFix = value; }
	}

	//! RectTransform
	private RectTransform m_RectTransform;
	public RectTransform rectTransform {
		get {
			if(m_RectTransform == null) m_RectTransform = GetComponent<RectTransform>();
			return m_RectTransform;
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
			if (direction == InfiniteScroll.Direction.Vertical) {
				m_RectTransform.anchoredPosition = new Vector2 (0, -value);
			} else { 
				m_RectTransform.anchoredPosition = new Vector2 (value, 0);
			}
		}
	}
	//! 中央にFIXするようにするための差分
	private float scrollSize {
		get {
			return (direction == InfiniteScroll.Direction.Vertical ) ? 
				rectTransform.sizeDelta.y:
				rectTransform.sizeDelta.x;
		}
	}
	//! スクロールベース位置
	private float basePosition {
		get {
			float length = (direction == InfiniteScroll.Direction.Vertical ) ? 
				rectTransform.sizeDelta.y:
				rectTransform.sizeDelta.x;
			// スクロールの中心からアンカーが上なのでアイテムの半分だけ上にずらす
			return (length / 2)/* center */ - (itemScale / 2);
		}
	}

	//! アイテムの大きさを返却
	private float m_ItemScale = -1;
	public float itemScale {
		get {
			if(m_ItemPrefab != null && m_ItemScale == -1) {
				m_ItemScale = direction == Direction.Vertical ? m_ItemPrefab.sizeDelta.y : m_ItemPrefab.sizeDelta.x;
			}
			return m_ItemScale;
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
			get { return this._useItemIndex; }
			set{
				this._useItemIndex = value;
			}
		}
	}


	// ------------------- 定義ここまで ------------------- //

	//! Unity Start
	protected override void Start ()
	{
		var list = new List<ItemControllerInfinite.Data> ();
		for (int i = 0; i < 12; i++) {
			var item = new ItemControllerInfinite.Data ();
			item.index = i + 1;
			list.Add (item);
		}
		this.Create<ItemControllerInfinite.Data> (list.ToArray ());
	}

	//! アイテムリストを作成
	public void Create<T>( IList<T> items )
	{
		if (this.m_ItemPrefab == null) return;
		if (items == null) return;

		// 初期化
		this.Initialize();

		// 内部リストアイテムを設定
		ItemData[] newItems = new ItemData[items.Count];
		for (int i = 0, len = items.Count; i < len; i++)
		{
			ItemData item = new ItemData ();
			item.index = i;
			item.data = items [i];
			newItems [i] = item;
		}
		m_InnerItems = newItems;

		// 最初に表示するリストを作成
		for(int i = 0; i < instantateItemCount; i++) {
			var obj = GameObject.Instantiate(m_ItemPrefab) as RectTransform;
			// RectTransformを初期化
			obj.SetParent(transform, false);
			obj.name = i.ToString();
			obj.anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -itemScale * i) : new Vector2(itemScale * i, 0);

			// アイテムインターフェースを継承したクラスが付いてるオブジェクトを取得
			var item = (MonoBehaviour)obj.GetComponent(typeof(IListItem));
			if (item != null) {
				m_InnerItems [i].useItemIndex = m_CreatedItems.Count;
			}

			// 作成したアイテムリストに追加
			m_CreatedItems.Add (obj);

			// 表示
			obj.gameObject.SetActive(true);

			// 更新イベントの呼び出し
			this.OnItemShow(m_InnerItems[i]);
		}

		// 最初の更新
		this.LateUpdate ();
	}

	//! 初期化
	private void Initialize()
	{
		// 初期化済みの確認
		if( m_IsInitialized ) return;

		// スクロールを初期化
		m_FixedScroll = GetComponentInParent<FixedScrollRect>();
		m_FixedScroll.horizontal = direction == Direction.Horizontal;
		m_FixedScroll.vertical = direction == Direction.Vertical;
		m_FixedScroll.content = rectTransform;
		m_FixedScroll.movementType = ScrollRect.MovementType.Unrestricted;

		// アイテムを非表示に？
		m_ItemPrefab.gameObject.SetActive(false);

		// 初期化完了
		m_IsInitialized = true;

		// 初期位置設定
		this.Scroll (0);
	}

	//! Unity LateUpdate
	private void LateUpdate()
	{
		// アイテム存在確認
		if (m_CreatedItems.Count <= 0) {
			return;
		}

		// 手前に引く（進む）
		while(anchoredPosition - diffPreFramePosition  < -itemScale * 2)
		{
			diffPreFramePosition -= itemScale;

			// 実際のオブジェクト情報を取得
			int objIndex = this.GetArrayIndex(currentItemNo, instantateItemCount);

			// 位置移動
			var pos = (itemScale * instantateItemCount) + (itemScale * currentItemNo);
			m_CreatedItems [objIndex].anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -pos) : new Vector2(pos, 0);

			// アイテムデータを更新
			int nextNo = currentItemNo + instantateItemCount;
			int itemIndex = this.GetArrayIndex(nextNo, m_InnerItems.Length);
			m_InnerItems[itemIndex].useItemIndex = objIndex;
			m_InnerItems[itemIndex].index = nextNo;

			// 更新通知
			this.OnItemShow(m_InnerItems[itemIndex]);

			currentItemNo++;
		}

		// 奥に引く（戻る）
		while(anchoredPosition - diffPreFramePosition > 0)
		{
			diffPreFramePosition += itemScale;

			// 実際のオブジェクト情報を取得
			int objIndex = this.GetArrayIndex(currentItemNo + instantateItemCount - 1, instantateItemCount);

			// 位置移動
			var pos = itemScale * currentItemNo;
			m_CreatedItems [objIndex].anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -pos): new Vector2(pos, 0);

			// アイテムデータを更新
			int itemIndex = this.GetArrayIndex(currentItemNo, m_InnerItems.Length);
			m_InnerItems[itemIndex].useItemIndex = objIndex;
			m_InnerItems[itemIndex].index = currentItemNo;

			// 更新通知
			this.OnItemShow(m_InnerItems[itemIndex]);

			currentItemNo--;
		}

		// フィットするように位置を補正
		if ( !m_IsFix && !m_FixedScroll.isDrag && Mathf.Abs (m_FixedScroll.Velocity) < LIMIT_SCROLL_VELOCITY)
		{
			// 移動場所のアイテム区切りでの差分
			float centerDiff = (this.scrollSize / 2) % itemScale;
			float diff = (anchoredPosition + centerDiff) % itemScale;

			if (Mathf.Abs (diff) > itemScale / 2)
			{
				// 半分より奥の場合（進ませる）
				var adjust = itemScale * ((anchoredPosition > 0f) ? 1 : -1);
				anchoredPosition += (adjust - diff) * Time.deltaTime * SPRING_POWER;
			}
			else
			{
				// 半分より手前の場合（戻す）
				anchoredPosition -= diff * Time.deltaTime * SPRING_POWER;
			}
		}
	}

	//! スクロールを指定位置に移動する
	public void Scroll(int index)
	{
		// スクロールの動きを止める
		m_FixedScroll.StopMovement ();
		// 目標位置に移動する
		anchoredPosition = this.basePosition - (itemScale * index);
		// FIX
		m_IsFix = true;
	}
	//! インデックスをアンカー位置から求める
	public int GetIndex()
	{
		float rough = (this.basePosition - anchoredPosition) / itemScale;
		return (int)Mathf.Round(rough);
	}
	//! 現在のアイテムを取得する
	public T GetCurrentItem<T>()
	{
		int itemIndex = this.GetArrayIndex(this.GetIndex(), m_InnerItems.Length);

		return (T)m_InnerItems [itemIndex].data;
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
	//! アイテムが表示される際のイベント
	private void OnItemShow( ItemData item )
	{
		// インターフェースの取得
		IListItem obj = this.GetListItemInterface(item);

		if (obj != null) {
			obj.OnUpdateItem (item.index, item.data);
		}
	}
	//! アイテムが使用しているBehaviourを取得
	private IListItem GetListItemInterface(ItemData item){
		return m_CreatedItems [item.useItemIndex].GetComponent<IListItem> ();
	}
}
