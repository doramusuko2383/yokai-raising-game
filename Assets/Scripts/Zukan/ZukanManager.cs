using System.Collections.Generic;
using UnityEngine;

public class ZukanManager : MonoBehaviour
{
    public List<YokaiData> allYokaiList = new List<YokaiData>();

    public YokaiData GetData(string id)
    {
        foreach (var data in allYokaiList)
        {
            if (data.id.ToString() == id)
                return data;
        }

        return null;
    }
}
