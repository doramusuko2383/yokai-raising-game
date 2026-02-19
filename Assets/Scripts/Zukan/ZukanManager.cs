using System.Collections.Generic;
using UnityEngine;

public class ZukanManager : MonoBehaviour
{
    public List<YokaiData> allYokaiList = new List<YokaiData>();

    public YokaiData GetData(string id)
    {
        return allYokaiList.Find(x => x.id.ToString() == id);
    }
}
