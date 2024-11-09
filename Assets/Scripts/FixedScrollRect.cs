/// <summary>
/// Fixed scroll rect.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

//! 固定ScrollRect
public class FixedScrollRect : ScrollRect
{
	//! 無限スクロール
	private InfiniteScroll m_InfinityScroll;
	private InfiniteScroll infinityScroll {
		get {
			if (m_InfinityScroll == null)
				m_InfinityScroll = GetComponentInChildren<InfiniteScroll> ();
			return m_InfinityScroll;
		}
	}

	//! ドラッグしているかどうか
	public bool isDrag
	{
		get;
		private set;
	}

	//! スクロール速度
	public float Velocity {
		get {
			return  (infinityScroll.direction == InfiniteScroll.Direction.Vertical) ? 
				-velocity.y :
				velocity.x;
		}
	}

	//! ドラッグ開始
	public override void OnBeginDrag(PointerEventData eventData){
		base.OnBeginDrag (eventData);
		isDrag = true;
	}

	//! ドラッグ終了
	public override void OnEndDrag(PointerEventData eventData){
		base.OnEndDrag (eventData);
		isDrag = false;

		// 固定を解除
		infinityScroll.IsFix = false;
	}
}
