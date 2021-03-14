using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AnimationCurveSet : MonoBehaviour
{
    public AnimationCurve DragCurve;
    public float drag_time = .5f;
    public AnimationCurve InventoryCurve;
    public float invent_time = .5f;
    public AnimationCurve LookDetailCurve;
    public float detail_time = .5f;


    public static AnimationCurveSet Instance {get;set;}

    private void Awake()
    {
        Instance = this;
    }
}
