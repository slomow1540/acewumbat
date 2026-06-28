using System.Collections;
using TMPro;
using UnityEngine;

public class Counting : MonoBehaviour
{
    public TMP_Text text;

    public string prefix;
    public string suffix;

    public float duration = 0.35f;

    Coroutine routine;

    int currentValue;

    public void SetValue(int target)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(Animate(target));
    }

    IEnumerator Animate(int target)
    {
        int start = currentValue;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, time / duration);

            int value = Mathf.RoundToInt(Mathf.Lerp(start, target, t));

            SetText(value);

            yield return null;
        }

        currentValue = target;

        SetText(target);
    }

    void SetText(int value)
    {
        text.text = prefix + value.ToString("N0") + suffix;
    }

    public void ResetCount()
    {
        currentValue = 0;

        SetText(0);
    }
}
