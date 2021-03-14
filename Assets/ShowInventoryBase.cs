using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 所有需要显示物品的类[继承]或者[挂载]这个脚本,
/// </summary>
public class ShowInventoryBase : MonoBehaviour
{
    public bool needShowInv = false;
    /// <summary>
    /// 继承的话不要忘记在派生类里面手动调用下面同名的方法哦!
    /// </summary>
    protected void Start()
    {
        ItemSpaceManager.Instance.AllShowList.Add(this);
    }

    protected void OnDestroy()
    {
        ItemSpaceManager.Instance.AllShowList.Remove(this);
    }
    void Update()
    {
        
    }
}
