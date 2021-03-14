using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
/// <summary>
/// Enter room and teleport player
/// </summary>
public class EnterDoor : TeleportBase
{
    private CursorState cursorState;
    private new Animation animation;
    private static bool isOpening = false;
    private Image blackImage;
    void Start()
    {
        cursorState = GetComponent<CursorState>();
        animation = GetComponent<Animation>();
        blackImage = GameObject.Find("Black").GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (cursorState.canInteract && Input.GetButtonDown("Fire1")&&!isOpening)
        {
            Tweener clear;
            isOpening = true;
            animation.Play();
            blackImage.enabled = true;
            Cursor.visible = false;
            InputComponent.Instance.enabled = false;
            Tweener tweener = blackImage.DOColor(Color.black,animation.clip.length);
            tweener.onComplete += () => 
            {
                Teleprot();
                clear = blackImage.DOColor(Color.clear, animation.clip.length);
                clear.SetDelay(.5f);
                clear.onComplete += () => 
                    {
                        blackImage.enabled = false;
                        isOpening = false;
                        Cursor.visible = true;
                        InputComponent.Instance.enabled = true;
                    };
            };
        }
    }
}
