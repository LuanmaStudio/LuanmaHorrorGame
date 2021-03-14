using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 使用道具的基类
/// </summary>
public abstract class ItemUseBase : MonoBehaviour
{
    public ItemData.Item item;

    /// <summary>
    /// Use item actual function
    /// </summary>
    protected abstract void Use();

}
