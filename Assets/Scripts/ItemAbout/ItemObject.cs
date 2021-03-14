using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 在场景中的物品挂载的脚本
/// </summary>
public class ItemObject : MonoBehaviour
{
    public ItemData.Item Item;

    private ItemSpaceManager spaceManager;
    private CursorState cursorState;
    private bool isClick = false;

    void Start()
    {
        cursorState = GetComponent<CursorState>();
        spaceManager = ItemSpaceManager.Instance;
    }

    void Update()
    {
        //点击道具,判断物品栏有没有满,道具加入物品栏
        if (cursorState.canInteract && isClick&&!spaceManager.isFull&&CursorManager.CanCilick)
        {
            GameObject ItemUI = (GameObject)Instantiate(Resources.Load(@"UI/" +Item.ToString()));
            spaceManager.items.Add(ItemUI);
            ItemUI.AddComponent<RemoveListWhenDestory>().Target = ItemUI;

            DragItem drag = ItemUI.GetComponent<DragItem>();
            var m = ShowInScreen.ShowItemInScreen(ItemUI);
            m.OnStart += () => drag.enabled = false;
            m.OnBack += () => drag.enabled = true;
            ItemUI.transform.SetParent(spaceManager.transform.GetChild(spaceManager.items.IndexOf(ItemUI)));
            ItemUI.transform.position = Input.mousePosition;//生成位置在鼠标位置
            Destroy(gameObject);
        }
    }
    /// <summary>
    /// 把道具展示到屏幕中央
    /// </summary>
    /// <param name="target">要展示的物品UI</param>


    private void OnMouseDown()
    {
        isClick = true;
    }
}
/// <summary>
/// 所有道具在销毁时移除物品List
/// </summary>
public class RemoveListWhenDestory : MonoBehaviour
{
    public GameObject Target;
    private void OnDestroy()
    {
       var spaceManager = GameObject.FindObjectOfType<ItemSpaceManager>();
        if(spaceManager !=null)
            spaceManager.items.Remove(Target);
    }
}
