/// <summary>
/// Fixed scroll rect.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Events;

//! 固定ScrollRect
public class FixedScrollRect : ScrollRect
{
	public UnityEvent onBeginDrag;
	public UnityEvent oEndDrag;

	//! ドラッグしているかどうか
	public bool isDrag
	{
		get;
		private set;
	}

	//! ドラッグ開始
	public override void OnBeginDrag(PointerEventData eventData){
		base.OnBeginDrag (eventData);
		isDrag = true;

		onBeginDrag?.Invoke ();
	}

	//! ドラッグ終了
	public override void OnEndDrag(PointerEventData eventData){
		base.OnEndDrag (eventData);
		isDrag = false;

        oEndDrag?.Invoke();
    }
}
