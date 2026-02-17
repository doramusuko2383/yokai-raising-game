using UnityEngine;

[CreateAssetMenu(fileName = "YokaiData", menuName = "Yokai/Zukan/YokaiData")]
public class YokaiData : ScriptableObject
{
    public int id;
    public string displayName;
    public Sprite icon;
    public Sprite fullImage;

    [TextArea]
    public string description;
}
