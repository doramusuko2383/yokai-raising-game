using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("FireBall")]
    public GameObject fireBallPrefab;

    [Header("進化後Prefab（属性順）")]
    // 0:Good / 1:Normal / 2:Bad / 3:Secret
    public GameObject[] evolutionPrefabs;

    [Header("UI")]
    public Slider growthSlider;

    void Start()
    {
        SpawnFireBall();
    }

    public GameObject SpawnFireBall()
    {
        if (fireBallPrefab == null)
        {
            Debug.LogWarning("[SPAWN] FireBall prefab is not assigned.");
            return null;
        }

        return Instantiate(fireBallPrefab, Vector3.zero, Quaternion.identity);
    }

    public GameObject SpawnEvolved(
        int tapCount,
        float lifeTime,
        int bornHour,
        Vector3 position
    )
    {
        if (evolutionPrefabs == null || evolutionPrefabs.Length == 0)
        {
            Debug.LogWarning("[SPAWN] Evolution prefabs are not assigned.");
            return null;
        }

        YokaiAttribute attr =
            DecideSecret(bornHour, tapCount, lifeTime) ??
            DecideNormalAttribute(tapCount);

        int index = (int)attr;
        index = Mathf.Clamp(index, 0, evolutionPrefabs.Length - 1);

        if (evolutionPrefabs[index] == null)
        {
            Debug.LogWarning($"[SPAWN] Evolution prefab at index {index} is null.");
            return null;
        }

        return Instantiate(evolutionPrefabs[index], position, Quaternion.identity);
    }

    // ★ D：例外チェック（当たったら即決）
    YokaiAttribute? DecideSecret(int bornHour, int tapCount, float lifeTime)
    {
        // 夜更かし（22時〜3時）
        if (bornHour >= 22 || bornHour <= 3)
        {
            return YokaiAttribute.Secret;
        }

        // 放置主義（ほぼ触らない）
        if (tapCount == 0 && lifeTime > 15f)
        {
            return YokaiAttribute.Secret;
        }

        return null;
    }

    // ★ 通常A/B/C
    YokaiAttribute DecideNormalAttribute(int tapCount)
    {
        if (tapCount >= 20) return YokaiAttribute.Good;
        if (tapCount <= 2) return YokaiAttribute.Bad;
        return YokaiAttribute.Normal;
    }

    public void UpdateGauge(float progress01)
    {
        if (growthSlider != null)
            growthSlider.value = progress01;
    }
}
