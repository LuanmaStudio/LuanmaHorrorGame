using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
/// <summary>
/// 拖动物品栏里面的物品,以及点击
/// </summary>
public class DragItem : ShowInventoryBase,IDescript
{
    public bool isSelect = false;
    public TextAsset descptFile;

    private ItemSpaceManager spaceManager;
    private ItemSelect SelectManager;
    private int currentCount;
    private bool canSelect = true;


    public string Description { get; set; }

    void Start()
    {
        base.Start();

        Description =JsonLib.ConstructItemData(descptFile.name)[0].Content;

        spaceManager = FindObjectOfType<ItemSpaceManager>();
        SelectManager = spaceManager.GetComponent<ItemSelect>();
    }

    void Update()
    {
        if (currentCount != spaceManager.items.Count)
        {
            transform.SetParent(spaceManager.transform.GetChild(spaceManager.items.IndexOf(gameObject)));
            currentCount = spaceManager.items.Count;
            MoveToLocalPos();
        }


        if (isSelect)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void MouseClick()
    {
        if (Input.GetButton("Fire1")&&canSelect&&CursorManager.CanCilick)
        {
            isSelect = true;
            needShowInv = true;
            SelectManager.current = GetComponent<ItemUse>();
            canSelect = false;
        }
        if(Input.GetButton("Fire2") && canSelect && CursorManager.CanCilick)
        {
            ShowInScreen.ShowItemInScreen(gameObject).OnBack += ()=> { MoveToLocalPos(); };
        }
    }
    public void MouseUP()
    {
        if (CursorManager.CanCilick)
        {
            isSelect = false;
            MoveToLocalPos().onComplete += () =>
            {
                canSelect = true;
                needShowInv = false;
            };
        }
    }
    /// <summary>
    /// 把道具UI移动会他的栏位
    /// </summary>
    /// <returns>移动动画的Twnner</returns>
    private Tweener MoveToLocalPos()
    {
        var go = transform.DOLocalMove(Vector3.zero, AnimationCurveSet.Instance.drag_time);
        go.SetEase(AnimationCurveSet.Instance.DragCurve);
        return go;
    }
    private void OnDestroy()
    {
        needShowInv = false;
        base.OnDestroy();
    }

}
