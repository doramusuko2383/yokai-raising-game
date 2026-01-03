using UnityEngine;

public class CharacterRespawnGuard : MonoBehaviour
{
    [SerializeField] RectTransform characterRoot;
    [SerializeField] GameObject fireBallPrefab;
    [SerializeField] string fireBallName = "FireBall";
    [SerializeField] bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
            EnsureCharacterExists();
    }

    public void EnsureCharacterExists()
    {
        if (characterRoot == null)
            characterRoot = GetComponent<RectTransform>();

        if (characterRoot == null)
        {
            Debug.LogWarning("[RESPAWN] CharacterRoot is not assigned.");
            return;
        }

        if (HasActiveCharacter(characterRoot))
            return;

        GameObject existing = FindChildByName(characterRoot, fireBallName);
        if (existing != null)
        {
            existing.SetActive(true);
            return;
        }

        if (fireBallPrefab == null)
        {
            Debug.LogWarning("[RESPAWN] FireBall prefab is not assigned.");
            return;
        }

        GameObject instance = Instantiate(fireBallPrefab, characterRoot);
        instance.name = fireBallName;
        ResetRectTransform(instance.transform as RectTransform);
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

    static GameObject FindChildByName(RectTransform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name)
                return child.gameObject;
        }

        return null;
    }

    static void ResetRectTransform(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.localPosition = Vector3.zero;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
