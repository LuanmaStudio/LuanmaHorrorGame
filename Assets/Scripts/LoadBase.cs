using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scene
{

}
/// <summary>
/// Load and teleport base class
/// </summary>
public class LoadBase : MonoBehaviour
{
    public Scene loadScene;

    protected void Load()
    {
        SceneManager.LoadScene(loadScene.ToString());
    }
}

public class TeleportBase : MonoBehaviour
{
    public Transform target;
    /// <summary>
    /// Teleport Player to target
    /// </summary>
    protected void Teleprot()
    {
        InputComponent.Instance.gameObject.transform.position = target.position;
    }
}
