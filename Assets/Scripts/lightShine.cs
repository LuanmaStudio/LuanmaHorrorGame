using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;
/// <summary>
/// Light shinning
/// </summary>
public class lightShine : MonoBehaviour
{

    public float ShinePossiable = .001f;
    public int shineCount = 5;
    public Vector2 inteval;

    private bool isShinning = false;
    private UnityEngine.Experimental.Rendering.Universal.Light2D light;
    void Start()
    {
        light = GetComponent<UnityEngine.Experimental.Rendering.Universal.Light2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isShinning && Random.Range(0, 1f) < ShinePossiable)
        {
            StartCoroutine(Shining());
        }
    }
    /// <summary>
    /// Light shinning times and interval
    /// </summary>
    /// <returns></returns>
    IEnumerator Shining()
    {
        for (int i = 0; i < shineCount; i++)
        {
            light.enabled = false;
            isShinning = true;
            yield return new WaitForSeconds(Random.Range(inteval.x, inteval.y));
            light.enabled = true;
        }
        isShinning = false;
    }
}
