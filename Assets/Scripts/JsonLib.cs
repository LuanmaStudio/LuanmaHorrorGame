using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;
/// <summary>
/// Json File Acces
/// </summary>
public class JsonLib : MonoBehaviour
{
    /// <summary>
    /// Acces Json File,and return them
    /// </summary>
    /// <param name="FileName">file name</param>
    /// <returns></returns>
    public static List<JonsonData> ConstructData(string FileName)
    {
        List<JonsonData> database = new List<JonsonData>();
        JsonData itemdata = JsonMapper.ToObject(File.ReadAllText(Application.dataPath + "/JonsLibray/Dialog/" + FileName + ".json"));
        for (int i = 0; i < itemdata.Count; i++)
        {
            database.Add(new JonsonData((int)itemdata[i]["id"], (string)itemdata[i]["name"], (string)itemdata[i]["content"], (int)itemdata[i]["duration"]));
        }
        return database;
    }

    public static List<ItemDeciptionData> ConstructItemData(string FileName)
    {
        List<ItemDeciptionData> data = new List<ItemDeciptionData>();
        JsonData itemdata = JsonMapper.ToObject(File.ReadAllText(Application.dataPath + "/JonsLibray/Description/" + FileName + ".json"));
        for (int i = 0; i < itemdata.Count; i++)
        {
            data.Add(new ItemDeciptionData((string)itemdata[i]["name"], (string)itemdata[i]["content"]));
        }
        return data;
    }
}
public struct ItemDeciptionData
{
    public string Name { get; set; }
    public string Content { get; set; }
    public ItemDeciptionData(string name,string content)
    {
        Name = name;
        Content = content;
    }

}
public struct JonsonData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
    public int Duration { get; set; }

    public JonsonData(int id, string name, string content, int duration)
    {
        ID = id;
        Name = name;
        Content = content;
        Duration = duration;
    }
}

