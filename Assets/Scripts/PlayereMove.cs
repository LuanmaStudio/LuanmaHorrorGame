using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Player movement
/// </summary>
public class PlayereMove : MonoBehaviour
{
    public LayerMask groundLayer;
    public float moveSpeed = .2f;
    public float runSpeed = .5f;
    public bool isRun = false;
    [HideInInspector]public Vector2 moveDir;

    private RaycastHit2D hit;
    [SerializeField]private bool isGrounded = false;
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("xSpeed",Mathf.Abs(moveDir.x));
        if (moveDir.x < 0) { transform.localScale = new Vector3(-2, 2, 2); }
        else if(moveDir.x>0) { transform.localScale = new Vector3(2, 2, 2); }

        hit = Physics2D.Raycast(transform.position, Vector2.down, 5, groundLayer);

        if(hit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
        if (isGrounded)
        {
            if (isRun)
            {
                transform.Translate(moveDir * runSpeed * Time.deltaTime);
            }
            else
            {
                transform.Translate(moveDir * moveSpeed * Time.deltaTime);
            }
        }
    }

}
