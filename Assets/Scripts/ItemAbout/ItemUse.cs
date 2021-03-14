using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 需要使用的物体挂载的组件
/// </summary>
public class ItemUse : ItemUseBase
{
    protected override void Use()
    {
        Destroy(gameObject);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
