using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Get Player input
/// </summary>
public class InputComponent : MonoBehaviour
{
    public KeyCode runKey = KeyCode.LeftShift;
    private PlayereMove playereMove;

    public static InputComponent Instance{ get; set; }

    void Awake()
    {
        Instance = this;
        playereMove = GetComponent<PlayereMove>();
    }
    private void OnDisable()
    {
        playereMove.moveDir.x = 0;

    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(runKey))
        {
            playereMove.isRun = true;
            playereMove.moveDir.x = Input.GetAxis("Horizontal")*1.5f;
        }
        else
        {
            playereMove.isRun = false;
            playereMove.moveDir.x = Input.GetAxis("Horizontal");
        }

    }
}
