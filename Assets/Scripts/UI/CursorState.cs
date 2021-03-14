using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum InteractType
{
    对话, 捡起, 查看,进门,未知
}
/// <summary>
/// Cursor move to object change State
/// </summary>
public class CursorState : MonoBehaviour
{
    public InteractType type;
    public bool canInteract = false;
    public float interactDistance = 5;
    private GameObject player;

    private void Start()
    {
        player = InputComponent.Instance.gameObject;
    }
    private void OnMouseOver()
    {
        if (Vector2.Distance(player.transform.position, transform.position) < interactDistance)
        {
            switch (type)
            {
                case InteractType.对话:
                    Cursor.SetCursor(CursorManager.conversation, Vector2.zero, CursorMode.Auto);
                    break;
                case InteractType.捡起:
                    Cursor.SetCursor(CursorManager.Pick, Vector2.zero, CursorMode.Auto);
                    break;
                case InteractType.查看:
                    Cursor.SetCursor(CursorManager.Eye, Vector2.zero, CursorMode.Auto);
                    break;
                case InteractType.进门:
                    Cursor.SetCursor(CursorManager.enterDoor, Vector2.zero, CursorMode.Auto);
                    break;
                case InteractType.未知:
                    Cursor.SetCursor(CursorManager.Unkonw, Vector2.zero, CursorMode.Auto);
                    break;
                default:
                    break;
            }
            canInteract = true;
        }
        else
        {
            Cursor.SetCursor(CursorManager.Unkonw, Vector2.zero, CursorMode.Auto);
            canInteract = false;
        }
    }
    private void OnMouseExit()
    {
        Cursor.SetCursor(CursorManager.defult, Vector2.zero, CursorMode.Auto);
        canInteract = false;
    }
    private void OnDestroy()
    {
        Cursor.SetCursor(CursorManager.defult, Vector2.zero, CursorMode.Auto);
        canInteract = false;
    }
}
