using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using System;
using UnityEngine.Events;

public class LookItemDetail:MonoBehaviour
{
    public GameObject DetailUI;

    void Start()
    {
    }

    private void OnMouseDown()
    {
        if (Input.GetButton("Fire1") && CursorManager.CanCilick)
        {
            Vector3 backPos = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            GameObject UI = Instantiate(DetailUI);
            UI.GetComponent<RectTransform>().localPosition = Input.mousePosition;
            ShowInScreen.ShowItemInScreen(UI).OnComlete += () =>
            {
                var m = UI.transform.DOLocalMove(backPos, AnimationCurveSet.Instance.detail_time);
                m.SetEase(AnimationCurveSet.Instance.LookDetailCurve);
                m.onComplete += () => Destroy(UI);
            };
        }

    }

    // Update is called once per frame
    void Update()
    {
    }
}
