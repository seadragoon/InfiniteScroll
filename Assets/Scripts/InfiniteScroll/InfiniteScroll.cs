﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class InfiniteScroll : UIBehaviour
{
    //! 方向
    public enum Direction
    {
        Vertical,
        Horizontal,
    }

    //! 止まる場所
    public enum FixPlace
    {
        Front,
        Center,
        Rear
    }

    //! 移動タイプ
    public enum DragType
    {
        Normal,
        StepByStep,
    }

    //! 作成するプレハブ
    [SerializeField]
    private RectTransform _itemPrefab = null;
    //! FIXに掛かる力
    [SerializeField]
    private int SPRING_POWER = 10;
    //! 制限スクロール速度
    [SerializeField]
    private int LIMIT_SCROLL_VELOCITY = 200;
    //! スクロールの方向
    [SerializeField]
    private Direction direction = Direction.Vertical;
    //! 止まる場所
    [SerializeField]
    private FixPlace _fixPlace = FixPlace.Center;
    //! ドラッグタイプ
    [SerializeField]
    private DragType _dragType = DragType.Normal;

    //! 初期化済みかどうか
    private bool isInitialized = false;
    //! 内部アイテムリスト
    private ItemData[] _items = null;
    //! 作成されたリストアイテムのRectTransform
    private List<ActualItem> _actualItems = new List<ActualItem>();

    //! スクロール
    private FixedScrollRect _fixedScroll = null;
    //! 実際に用意する生成アイテム数
    private int _actualItemCount = 0;
    //! 前フレームの位置
    private float _preFramePosition = 0;
    //! 自動スクロール対象インデックス
    private int _autoScrollTargetIndex = 0;
    //! 自動スクロール開始位置
    private float _autoScrollStartPosition = 0;
    //! 自動スクロール終了位置
    private float _autoScrollFinishPosition = 0;
    //! 自動スクロール中かどうか
    private bool _isAutoScroll = false;
    //! 固定されたかどうか
    private bool _isFix = false;
    //! ドラッグ開始時のインデックス
    private int _dragStartIndex = 0;

    //! RectTransform
    private RectTransform _rectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null) _rectTransform = _fixedScroll.content;
            return _rectTransform;
        }
    }

    //! アンカー位置
    private float anchoredPosition
    {
        get
        {
            return direction == Direction.Vertical ? -rectTransform.anchoredPosition.y : rectTransform.anchoredPosition.x;
        }
        set
        {
            // 位置を設定
            if (direction == Direction.Vertical)
            {
                _rectTransform.anchoredPosition = new Vector2(0, -value);
            }
            else
            {
                _rectTransform.anchoredPosition = new Vector2(value, 0);
            }
        }
    }

    // スクロール領域のサイズ
    private float scrollAreaSize
    {
        get
        {
            var scrollRect = _fixedScroll.GetComponent<RectTransform>();
            return (direction == Direction.Vertical) ? scrollRect.rect.height : scrollRect.rect.width;
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
    private float basePosition
    {
        get
        {
            switch (_fixPlace)
            {
                case FixPlace.Front:
                    return 0;
                case FixPlace.Center:
                    return (scrollAreaSize / 2) - (itemSize / 2);
                case FixPlace.Rear:
                    return scrollAreaSize - itemSize;
            }
            return 0;
        }
    }

    //! アイテムの大きさを返却
    private float _itemSize = -1;
    public float itemSize
    {
        get
        {
            if (_itemPrefab != null && _itemSize == -1)
            {
                _itemSize = direction == Direction.Vertical ? _itemPrefab.sizeDelta.y : _itemPrefab.sizeDelta.x;
            }
            return _itemSize;
        }
    }

    //! 実際のアイテム
    private class ActualItem
    {
        public int totalIndex;
        public ItemData itemData;
        public RectTransform rectTransform;
    }

    //! アイテム情報クラス
    private class ItemData
    {
        public int index;   //!< インデックス
        public object data; //!< アイテムに設定されるデータ
    }


    // ------------------- 定義ここまで ------------------- //

    //! アイテムリストを作成
    public void Create<T>(IList<T> items)
    {
        if (_itemPrefab == null) return;
        if (items == null) return;

        // 初期化
        Initialize();

        // 内部リストアイテムを設定
        ItemData[] newItems = new ItemData[items.Count];
        for (int i = 0, len = items.Count; i < len; i++)
        {
            ItemData item = new ItemData();
            item.index = i;
            item.data = items[i];
            newItems[i] = item;
        }
        _items = newItems;

        // 最初に表示するリストを作成
        if (_actualItems.Count == 0)
        {
            // 最初に表示するリストを作成
            for (int i = 0; i < _actualItemCount; i++)
            {
                var rect = Instantiate(_itemPrefab) as RectTransform;
                // RectTransformを初期化
                rect.SetParent(_fixedScroll.content, false);
                rect.name = i.ToString();

                // 作成したアイテムに追加
                var item = new ActualItem();
                item.totalIndex = i;
                item.itemData = null;
                item.rectTransform = rect;
                _actualItems.Add(item);

                // 表示
                rect.gameObject.SetActive(true);
            }
        }

        // 初期位置設定
        ForceScroll(0);
    }

    //! 初期化
    private void Initialize()
    {
        // 初期化済みの確認
        if (isInitialized) return;

        // スクロールを初期化
        _fixedScroll = GetComponentInParent<FixedScrollRect>();
        _fixedScroll.horizontal = direction == Direction.Horizontal;
        _fixedScroll.vertical = direction == Direction.Vertical;
        _fixedScroll.movementType = ScrollRect.MovementType.Unrestricted;
        if (_fixedScroll.content == null)
        {
            _fixedScroll.content = rectTransform;
        }

        // スクロールイベント登録
        _fixedScroll.onBeginDrag.RemoveAllListeners();
        _fixedScroll.onBeginDrag.AddListener(OnBeginDrag);
        _fixedScroll.onEndDrag.AddListener(OnEndDrag);

        // スクロールコンテンツ初期化(スクロール領域ピッタリにストレッチ)
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = new Vector2(0, 0);
        rectTransform.offsetMax = new Vector2(0, 0);

        // アイテムを初期化して非表示に
        _itemPrefab.anchorMin = direction == Direction.Vertical ? new Vector2(0, 1) : new Vector2(0, 0);
        _itemPrefab.anchorMax = direction == Direction.Vertical ? new Vector2(1, 1) : new Vector2(0, 1);
        _itemPrefab.pivot = direction == Direction.Vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
        _itemPrefab.offsetMin = direction == Direction.Vertical ? new Vector2(0, -_itemPrefab.rect.height) : new Vector2(0, 0);
        _itemPrefab.offsetMax = direction == Direction.Vertical ? new Vector2(0, 0) : new Vector2(_itemPrefab.rect.width, 0);
        _itemPrefab.gameObject.SetActive(false);

        // 実際に用意する生成アイテム数
        _actualItemCount = Mathf.CeilToInt(scrollAreaSize / itemSize) + 3;

        // 初期化完了
        isInitialized = true;
    }

    //! 実際に表示するアイテムの初期化
    private void InitializeActualItems(int index)
    {
        // 実際のアイテムを初期化
        for (int i = 0; i < _actualItems.Count; i++)
        {
            int totalIndex = index + i;
            int actualIndex = GetActualIndex(totalIndex);
            var actualItem = _actualItems[actualIndex];

            // インデックス
            actualItem.totalIndex = totalIndex;

            // 位置設定
            actualItem.rectTransform.anchoredPosition = (direction == Direction.Vertical) ? new Vector2(0, -itemSize * totalIndex) : new Vector2(itemSize * totalIndex, 0);

            // index設定
            int itemIndex = GetItemIndex(actualIndex);
            actualItem.itemData = _items[itemIndex];

            // 初期化イベントの呼び出し
            OnInitItem(actualIndex);

            // 更新イベントの呼び出し
            OnItemShow(actualIndex);
        }
    }

    //! FixedScrollRectから呼ばれるように登録する関数
    private void OnBeginDrag()
    {
        _isFix = false;
        _isAutoScroll = false;
        _dragStartIndex = GetCurrentIndex();
    }
    private void OnEndDrag()
    {
        if (_dragType == DragType.StepByStep)
        {
            _fixedScroll.StopMovement();
        }
    }

    //! Unity LateUpdate
    private void LateUpdate()
    {
        // アイテム存在確認
        if (_actualItems.Count <= 0)
        {
            return;
        }

        // StepByStepの場合のドラッグ中の制御
        if (_fixedScroll.isDrag && _dragType == DragType.StepByStep)
        {
            // 限界値を隣接する一つとして、その位置を超えないようにする
            var nextPos = GetTargetPosition(_dragStartIndex + 1);
            var prevPos = GetTargetPosition(_dragStartIndex - 1);
            if (nextPos > anchoredPosition)
            {
                anchoredPosition = nextPos;
                _fixedScroll.StopMovement();
            }
            else if (prevPos < anchoredPosition)
            {
                anchoredPosition = prevPos;
                _fixedScroll.StopMovement();
            }
        }

        // ドラッグ終了後、固定するまで
        if (!_fixedScroll.isDrag && !_isFix)
        {
            // 自動スクロール位置を決定する
            if (!_isAutoScroll && Mathf.Abs(scrollVelocity) < LIMIT_SCROLL_VELOCITY)
            {
                // スクロールの動きを止める
                _fixedScroll.StopMovement();

                // 現在位置から次の位置を決定する
                _autoScrollTargetIndex = GetCurrentIndex();
                _autoScrollFinishPosition = GetTargetPosition(_autoScrollTargetIndex);
                _autoScrollStartPosition = anchoredPosition;
                _isAutoScroll = true;
            }

            // 自動スクロールONの場合、ターゲット位置に移動する
            if (_isAutoScroll)
            {
                float velocity = (_autoScrollFinishPosition - _autoScrollStartPosition) * Time.deltaTime * SPRING_POWER;

                // 最大速度を超えないようにする ※速すぎるとバグるので1フレームで進む距離はスクロール範囲と同等くらいまでにしないといけない
                if (Mathf.Abs(velocity) > Mathf.Min(LIMIT_SCROLL_VELOCITY, scrollAreaSize))
                {
                    velocity = (velocity > 0 ? 1 : -1) * Mathf.Min(LIMIT_SCROLL_VELOCITY, scrollAreaSize);
                }

                if (velocity == 0
                 || (velocity > 0 && _autoScrollFinishPosition < anchoredPosition + velocity)
                 || (velocity < 0 && _autoScrollFinishPosition > anchoredPosition + velocity))
                {
                    OnScrollFixed(_autoScrollTargetIndex);
                }
                else
                {
                    anchoredPosition += velocity;
                }
            }
        }

        // 位置が進んだ（番号が戻った）場合
        if (_preFramePosition < anchoredPosition)
        {
            AdjustPositionMoveForward();
        }

        // 位置が戻った（番号が進んだ）場合
        if (_preFramePosition > anchoredPosition)
        {
            AdjustPositionMoveBack();
        }


        // 現在のフレームの位置を保持
        _preFramePosition = anchoredPosition;
    }
    //! 前方に移動した際の位置移動（前から後ろへ）
    public void AdjustPositionMoveForward()
    {
        for (int i = 0; i < _actualItems.Count; i++)
        {
            var actualItem = _actualItems[i];
            var pos = (direction == Direction.Vertical) ? -actualItem.rectTransform.anchoredPosition.y : actualItem.rectTransform.anchoredPosition.x;

            if (anchoredPosition + pos > scrollAreaSize + itemSize)
            {
                // アイテムの位置移動
                var offset = itemSize * _actualItems.Count;
                actualItem.rectTransform.anchoredPosition -= (direction == Direction.Vertical) ? new Vector2(0, -offset) : new Vector2(offset, 0);

                // アイテムデータを更新
                int nextIndex = actualItem.totalIndex - _actualItems.Count;
                int itemIndex = GetItemIndex(nextIndex);
                actualItem.totalIndex = nextIndex;
                actualItem.itemData = _items[itemIndex];

                // 更新通知
                OnItemShow(nextIndex);
            }
        }
    }
    //! 後方に移動した際の奥側の位置設定
    public void AdjustPositionMoveBack()
    {
        for (int i = 0; i < _actualItems.Count; i++)
        {
            var actualItem = _actualItems[i];
            var pos = (direction == Direction.Vertical) ? -actualItem.rectTransform.anchoredPosition.y : actualItem.rectTransform.anchoredPosition.x;

            if (anchoredPosition + pos < -itemSize * 2)
            {
                // アイテムの位置移動
                var offset = itemSize * _actualItems.Count;
                actualItem.rectTransform.anchoredPosition += (direction == Direction.Vertical) ? new Vector2(0, -offset) : new Vector2(offset, 0);

                // アイテムデータを更新
                int nextIndex = actualItem.totalIndex + _actualItems.Count;
                int itemIndex = GetItemIndex(nextIndex);
                actualItem.totalIndex = nextIndex;
                actualItem.itemData = _items[itemIndex];

                // 更新通知
                OnItemShow(nextIndex);
            }
        }
    }

    //! スクロールを指定位置に移動する
    public void Scroll(int index)
    {
        // 既に指定のインデックスにいる場合は無視
        if (GetCurrentIndex() == index)
        {
            return;
        }

        // スクロールの動きを止める
        _fixedScroll.StopMovement();

        // 自動スクロールON
        _isFix = false;
        _isAutoScroll = true;

        // 移動する先を設定する
        _autoScrollTargetIndex = index;
        _autoScrollStartPosition = anchoredPosition;
        _autoScrollFinishPosition = GetTargetPosition(_autoScrollTargetIndex);
    }
    public void ScrollInInfinite(int itemIndex)
    {
        // 既に指定のインデックスにいる場合は無視
        if (GetCurrentItemIndex() == itemIndex)
        {
            return;
        }

        int targetIndex = GetNearItemIndexInfinite(itemIndex);
        Scroll(targetIndex);
    }
    //! スクロールを指定位置に強制移動する
    public void ForceScroll(int index)
    {
        // アイテム初期化
        InitializeActualItems(index);
        // スクロール固定
        OnScrollFixed(index);
    }
    //! スクロール固定
    private void OnScrollFixed(int index)
    {
        // スクロールの動きを止める
        _fixedScroll.StopMovement();
        // FIX
        _isFix = true;
        // 自動スクロールも停止
        _isAutoScroll = false;
        // 目標位置に明示的に移動する
        anchoredPosition = basePosition - (itemSize * index);
        _preFramePosition = anchoredPosition;
        // 表示更新
        AdjustPositionMoveForward();
        AdjustPositionMoveBack();
        // アイテム固定されたイベント発火
        OnItemFixed(GetCurrentIndex());
    }
    //! 現在のインデックスを取得
    public int GetCurrentIndex()
    {
        float rough = (basePosition - anchoredPosition) / itemSize;
        return (int)Mathf.Round(rough);
    }
    //! 現在のアイテム番号を取得
    public int GetCurrentItemIndex()
    {
        return GetCurrentIndex() % _items.Length;
    }
    //! 現在のアイテムを取得する
    public T GetCurrentItem<T>()
    {
        int itemIndex = GetItemIndex(GetCurrentIndex());

        return (T)_items[itemIndex].data;
    }


    private int GetNearItemIndexInfinite(int itemIndex)
    {
        int currentIndex = GetCurrentIndex();
        int startIndex = (currentIndex / _items.Length) * _items.Length;
        int currentTargetIndex = startIndex + itemIndex;

        // 同一ループでの移動か、前後ループへの移動のどれが近いか
        if (MathF.Abs(currentTargetIndex - currentIndex) > MathF.Abs(currentTargetIndex - _items.Length - currentIndex))
        {
            // 前の方が近い
            return currentTargetIndex - _items.Length;
        }
        else if (MathF.Abs(currentTargetIndex - currentIndex) > MathF.Abs(currentTargetIndex + _items.Length - currentIndex))
        {
            // 後の方が近い
            return currentTargetIndex + _items.Length;
        }
        else
        {
            // 同一が近い
            return currentTargetIndex;
        }
    }
    //! 対象インデックスの位置を取得
    private float GetTargetPosition(int index)
    {
        return basePosition - (itemSize * index);
    }
    //! 指定された数値の配列インデックスを返す
    private int GetArrayIndex(int number, int length)
    {
        if (number < 0)
        {
            int remain = Mathf.Abs(number) % length;
            return (remain == 0) ? 0 : length - Mathf.Abs(number) % length;
        }
        else
        {
            return number % length;
        }
    }
    //! 実際の配列の指定番目のインデックスを返す
    private int GetActualIndex(int number)
    {
        return GetArrayIndex(number, _actualItemCount);
    }
    //! 内部配列の指定番目のインデックスを返す
    private int GetItemIndex(int number)
    {
        return GetArrayIndex(number, _items.Length);
    }


    //! アイテムが表示される際のイベント
    private void OnInitItem(int totalIndex)
    {
        // インターフェースの取得
        int itemIndex = GetItemIndex(totalIndex);
        var item = _items[itemIndex];
        IListItem obj = GetListItemInterface(totalIndex);

        if (obj != null)
        {
            obj.OnInitItem(totalIndex, item.index, item.data);
        }
    }
    //! アイテムが表示される際のイベント
    private void OnItemShow(int totalIndex)
    {
        // インターフェースの取得
        int itemIndex = GetItemIndex(totalIndex);
        var item = _items[itemIndex];
        IListItem obj = GetListItemInterface(totalIndex);

        if (obj != null)
        {
            obj.OnUpdateItem(totalIndex, item.index, item.data);
        }
    }
    private void OnItemFixed(int totalIndex)
    {
        // インターフェースの取得
        int itemIndex = GetItemIndex(totalIndex);
        var item = _items[itemIndex];
        IListItem obj = GetListItemInterface(totalIndex);

        if (obj != null)
        {
            obj.OnFixedItem(totalIndex, item.index, item.data);
        }
    }
    //! アイテムが使用しているBehaviourを取得
    private IListItem GetListItemInterface(int totalIndex)
    {
        int actualIndex = GetActualIndex(totalIndex);
        var target = _actualItems[actualIndex];
        if (target != null)
        {
            return target.rectTransform.GetComponent<IListItem>();
        }
        return null;
    }
}
