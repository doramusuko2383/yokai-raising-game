using UnityEngine;

[System.Serializable]
public class EvolutionRule
{
    [SerializeField]
    public string currentName;

    [SerializeField]
    public string nextName;

    public bool Matches(string name)
    {
        return !string.IsNullOrEmpty(currentName) && currentName == name;
    }
}
