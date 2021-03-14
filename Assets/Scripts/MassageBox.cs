using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Create massage box
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MassageBox : MonoBehaviour
{
    public DialogType dialogType;
    public GameObject textBox;
    public TextAsset JsonFile;
    public bool isLockMovement = true;
    private Context context;
    private CursorState cursorState;
    private GameObject go; 

    void Start()
    {
        cursorState = GetComponent<CursorState>();
        if (dialogType == DialogType.Observe)
        {
            Destroy(GetComponent<CursorState>());
        }

    }

    // Update is called once per frame
    private void OnMouseDown()
    {
        if (cursorState.canInteract&&go ==null&&dialogType==DialogType.Interate)
        {
            CheckMove();
            go = Instantiate(textBox,GameObject.Find("MassageCanvas").transform);
            context = go.GetComponent<Context>();
            context.Sender = this;
            context.TextFileInit(JsonFile.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (dialogType == DialogType.Observe)
        {
            CheckMove();
            go = Instantiate(textBox, GameObject.Find("MassageCanvas").transform);
            context = go.GetComponent<Context>();
            context.Sender = this;
            context.TextFileInit(JsonFile.name);

            GetComponent<Collider2D>().enabled = false;
        }
    }   

    private void CheckMove()
    {
        if (isLockMovement)
        {
            InputComponent.Instance.enabled = false;
        }
    }
}
public enum DialogType
{
    Interate,Observe
}
