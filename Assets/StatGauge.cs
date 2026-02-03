using UnityEngine;

public class StatGauge
{
    public float Current { get; private set; }
    public float Max { get; private set; }
    public float Normalized => Max > 0f ? Mathf.Clamp01(Current / Max) : 0f;

    public StatGauge(float max, float current)
    {
        Reset(current, max);
    }

    public void Reset(float current, float max)
    {
        Max = Mathf.Max(0f, max);
        Current = Mathf.Clamp(current, 0f, Max);
    }

    public void SetCurrent(float value)
    {
        Current = Mathf.Clamp(value, 0f, Max);
    }

    public void Add(float amount)
    {
        SetCurrent(Current + amount);
    }
}
