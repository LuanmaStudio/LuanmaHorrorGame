using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
/// <summary>
/// 管理物品栏的脚本
/// </summary>
public class ItemSpaceManager : MonoBehaviour
{
    public int Space = 11;
    /// <summary>
    /// 所有的物品在这个List里
    /// </summary>
    public List<GameObject> items;
    /// <summary>
    /// 所有需要显示物品栏的脚本List
    /// </summary>
    public List<ShowInventoryBase> AllShowList;
    public float waitTime = 2f;
    private float bottom;
    [HideInInspector]public bool isFull = false;
    private bool showInventory = false;

    private Tweener tweener;
    public static ItemSpaceManager Instance { get; set; }
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {

        bottom = -GetComponent<RectTransform>().sizeDelta.y;
        tweener = transform.DOMove(new Vector3(0, bottom), AnimationCurveSet.Instance.invent_time);
        tweener.SetAutoKill(false);
        tweener.SetEase(AnimationCurveSet.Instance.InventoryCurve);
    }

    // Update is called once per frame
    void Update()
    {
        //遍历所有展示物品栏的类,康康有没有需要物品栏出现的闸总
        showInventory = false;
        foreach (var item in AllShowList)
        {
            if (item.needShowInv)
            {
                showInventory = true;
                break;
            }
        }

        //Inventory auto show up
        if (Input.mousePosition.y < Screen.height / 5||showInventory)
        {
            ShowUP();
            CancelInvoke();
        }
        else
        {
            Invoke("ShowDown", waitTime);
        }

    }

    private void ShowUP()
    {
        showInventory = true;
        tweener.PlayBackwards();
    }

    private void ShowDown()
    {
        showInventory = false;
        tweener.PlayForward();
    }
}


public interface IShowInventory
{
    /// <summary>
    /// 是否需要显示物品栏
    /// </summary>
    bool NeedShowInv { get; set; }
    void ShowInvIniti();
}
