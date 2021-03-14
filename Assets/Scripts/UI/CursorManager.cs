using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// 管理光标的外貌和点击
/// </summary>
public class CursorManager : MonoBehaviour
{
    public static bool CanCilick = true;




    public static Texture2D defult;
    public static Texture2D inavalable;
    public static Texture2D Eye;
    public static Texture2D Pick;
    public static Texture2D conversation;
    public static Texture2D enterDoor;
    public static Texture2D Unkonw;

    public Texture2D defultAssset;
    public Texture2D inavalableAssset;
    public Texture2D EyeAssset;
    public Texture2D PickAssset;
    public Texture2D conversationAssset;
    public Texture2D enterDoorAssset;
    public Texture2D UnkownAssat;
    void Start()
    {
        defult = defultAssset;
        inavalable = inavalableAssset;
        Eye = EyeAssset;
        Pick = PickAssset;
        conversation = conversationAssset;
        enterDoor = enterDoorAssset;
        Unkonw = UnkownAssat;


        CanCilick = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
