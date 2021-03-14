using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// 把一个物品在屏幕中央展示点击后回复位置
/// </summary>
public class ShowInScreen : ShowInventoryBase
{

    public System.Action OnBack;
    public System.Action OnStart;
    public System.Action OnComlete;
    public GameObject intruduceLable;
    public Transform parent;

    private Tweener twnner;
    private IDescript descript;
    private new void Start()
    {
        base.Start();
        OnStart += ()=> { };
        OnBack += () => { };
        OnComlete += () => { };
        OnStart();

        descript = GetComponent<IDescript>();

        CursorManager.CanCilick = false;
        InputComponent.Instance.enabled = false;
        transform.parent = GameObject.Find("Canvas").transform;//防止屏幕中央UI受刀物品栏移动影响
        //动画部分
        var y = transform.DOMove(new Vector2(Screen.width/2,Screen.height/2) , .5f);
        y.onComplete += () => 
        {
            var m = GetComponent<SpriteRenderer>();
            var s = GetComponent<RectTransform>();
            //改变文字位置
            intruduceLable.GetComponent<RectTransform>().localPosition = new Vector2( 0, -s.rect.height*s.localScale.x);
        };
        var t = transform.DOScale(3, .5f);
        t.onComplete += () => 
        {
            twnner = intruduceLable.GetComponent<Text>().DOText(descript.Description, .5f);
            twnner.SetAutoKill(false);
        };
    }

    // Update is called once per frame
    void Update()
    {
        //看完了点一下回去
        if(twnner !=null)
        if (Input.anyKey&&twnner.IsComplete())
        {
            Destroy(intruduceLable);
            var t = transform.DOScale(1, .5f);
            needShowInv = true;
            OnBack();
            if (ItemSpaceManager.Instance.items.Contains(gameObject))
            {
                parent = ItemSpaceManager.Instance.transform.GetChild(ItemSpaceManager.Instance.items.IndexOf(gameObject));
            }
            transform.SetParent(parent);
            t.onComplete += () =>
            {
                ShowOver();
                OnComlete();
            };
        }
    }

    private void ShowOver()
    {
        InputComponent.Instance.enabled = true;
        needShowInv = false;

        CursorManager.CanCilick = true;
        Destroy(this);
    }
    /// <summary>
    /// 把一个物品展示再屏幕中央
    /// </summary>
    /// <param name="target">要展示的物体</param>
    public static ShowInScreen ShowItemInScreen(GameObject target)
    {
        GameObject Lable = Instantiate((GameObject)Resources.Load(@"UI/DescriptLable"));
        Lable.transform.parent = ItemSpaceManager.Instance.transform.parent;
        var m = target.AddComponent<ShowInScreen>();
        m.intruduceLable = Lable;
        return m;
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
    }
}
/// <summary>
/// 需要在屏幕中央展示时必须的接口
/// </summary>
interface IDescript
{
    /// <summary>
    /// 屏幕上显示的文字
    /// </summary>
    string Description { get; set; }
}
