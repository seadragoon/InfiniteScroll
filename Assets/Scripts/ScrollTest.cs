using System.Collections.Generic;
using UnityEngine;

public class ScrollTest : MonoBehaviour
{
    [SerializeField]
    private InfiniteScroll _scroll;

    void Start()
    {
        var list = new List<TestItem.Data>();
        for (int i = 0; i < 12; i++)
        {
            var item = new TestItem.Data();
            item.itemNo = i + 1;
            list.Add(item);
        }
        _scroll.Create(list.ToArray());
    }
}
