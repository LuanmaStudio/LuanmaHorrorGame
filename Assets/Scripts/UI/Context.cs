using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
/// <summary>
/// Manager massage box
/// </summary>
public class Context : MonoBehaviour
{
    public float showSpeed;
    private List<JonsonData> contract = new List<JonsonData>();
    private Text textBox;
    private bool isShowFinished = true;
    private InputComponent playerInput;
    public MassageBox Sender;

    private int Index;

    private void OnEnable()
    {
        textBox = GetComponentInChildren<Text>();
        playerInput = InputComponent.Instance;
    }
    private void OnDisable()
    {
        playerInput.enabled = true;
    }

    void Start()
    {
        textBox = GetComponentInChildren<Text>();

        if (Sender != null)
        {
            if (Sender.dialogType == DialogType.Observe)
            {
                StartCoroutine(ShowObseveText());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && isShowFinished&&Sender.dialogType==DialogType.Interate)
        {
            ShowEachText();
        }
    }
    private Tweener ShowEachText()
    {
        textBox.text = "";
        Tweener tweener;
        if (Index >= contract.Count)
        {
            Destroy(gameObject);
            return null;
        }
        else
        {
            isShowFinished = false;
            if (contract[Index].Name == "Player")
            {
                textBox.transform.parent.parent.GetComponent<RectTransform>().localPosition = InputComponent.Instance.transform.position;
            }
            else
            {
                textBox.transform.parent.parent.GetComponent<RectTransform>().localPosition = GameObject.Find(contract[Index].Name).transform.position;
            }
            tweener = textBox.DOText(contract[Index].Content, contract[Index].Content.Length*showSpeed);
            tweener.SetEase(Ease.Linear);
            tweener.onComplete += () =>
            {
                isShowFinished = true;
                Index++;
            };
            return tweener;
        }
    }

    IEnumerator ShowObseveText()
    {
        int tempIndex;
        foreach (var item in contract)
        {
            var twnner = ShowEachText();
            tempIndex = Index;
            yield return new WaitForSeconds(twnner.Duration());
            yield return new WaitForSeconds(contract[tempIndex].Duration / (float)100);
        }
        Destroy(gameObject);
    }

    public void TextFileInit(string name)
    {
        contract = JsonLib.ConstructData(name);
    }

}
