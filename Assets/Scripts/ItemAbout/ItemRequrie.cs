using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// The interact needed
/// </summary>
public class ItemRequrie : MonoBehaviour
{
    public ItemData.Item requireItem;
    public float distance = 5;

    private ItemSelect select;
    private Image image;
    private bool isMouseEnter =false;
    private bool isEnter = false;
    private bool canBeUse = false;
    private float currentDistacne;
    void Start()
    {
        select = GameObject.FindObjectOfType<ItemSelect>();
    }

    // Update is called once per frame
    void Update()
    {

        if (select.current!=null && isEnter&&Input.GetMouseButtonUp(0)&&canBeUse)
        {
            select.current.SendMessage("Use");//if match call use function
        }

        if (!Input.GetMouseButton(0))
        {
            isMouseEnter = false;
        }

        if (!isMouseEnter)
        {
            if(image !=null)
            image.color = Color.white;
        }

        currentDistacne = Vector2.Distance(InputComponent.Instance.transform.position, transform.position);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayereMove>() != null)
        {
            isEnter = true;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.GetComponent<PlayereMove>() != null)
        {
            isEnter = false;
        }

    }
    private void OnMouseEnter()
    {
        if(select.current!=null)
            image = select.current.GetComponent<Image>();
    }
    private void OnMouseOver()
    {
        if(select.current != null&&Input.GetMouseButton(0))
        {
            if (select.current.item == requireItem&&currentDistacne<=distance)
            {
                image.color = Color.green;
                canBeUse = true;
            }
            else
            {
                image.color = Color.red;
                canBeUse = false;
            }
            isMouseEnter = true;
        }

        
    }
    private void OnMouseExit()
    {
        isMouseEnter = false;
        canBeUse = false;
    }
}
