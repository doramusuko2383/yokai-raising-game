using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ZukanManager : MonoBehaviour
{
    public List<YokaiData> allYokaiList = new List<YokaiData>();

    public YokaiData GetData(string id)
    {
        if (allYokaiList == null)
        {
            Debug.LogWarning($"[ZukanManager] GetData() miss: allYokaiList is null, requestedId='{id}'");
            return null;
        }

        bool requestedIdAsInt = int.TryParse(id, out int parsedId);

        foreach (var data in allYokaiList)
        {
            if (data.id.ToString() == id)
                return data;

            if (requestedIdAsInt && data.id == parsedId)
                return data;
        }

        StringBuilder sampleIds = new StringBuilder();
        int sampleCount = Mathf.Min(5, allYokaiList.Count);
        for (int i = 0; i < sampleCount; i++)
        {
            if (i > 0)
                sampleIds.Append(", ");

            sampleIds.Append(allYokaiList[i] != null ? allYokaiList[i].id.ToString() : "null");
        }

        Debug.LogWarning($"[ZukanManager] GetData() miss: requestedId='{id}', parsedId={(requestedIdAsInt ? parsedId.ToString() : "N/A")}, allYokaiCount={allYokaiList.Count}, sampleIds=[{sampleIds}]");

        return null;
    }
}
