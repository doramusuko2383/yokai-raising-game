using UnityEngine;

public class CharacterRespawnGuard : MonoBehaviour
{
    [SerializeField] RectTransform characterRoot;

    public void EnsureCharacterExists()
    {
        if (characterRoot == null)
            characterRoot = GetComponent<RectTransform>();

        if (characterRoot == null)
        {
            Debug.LogWarning("[STATE] CharacterRoot is not assigned.");
            return;
        }

        if (CurrentYokaiContext.Current != null && CurrentYokaiContext.Current.activeInHierarchy)
            return;

        if (HasActiveCharacter(characterRoot))
            return;
        Debug.LogWarning("[STATE] No active yokai found under CharacterRoot.");
    }

    static bool HasActiveCharacter(RectTransform root)
    {
        foreach (Transform child in root)
        {
            if (child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

}
