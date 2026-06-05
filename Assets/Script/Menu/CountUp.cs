using System.Collections;
using TMPro;
using UnityEngine;

public class CountUp
    : MonoBehaviour
{
    public TMP_Text text;

    public string prefix;
    public string suffix;

    public float duration =
        0.35f;

    Coroutine routine;

    int currentValue;

    public void SetValue(
        int target
    )
    {
        if (routine != null)
        {
            StopCoroutine(
                routine
            );
        }

        routine =
            StartCoroutine(
                Animate(target)
            );
    }

    IEnumerator Animate(
        int target
    )
    {
        int start =
            currentValue;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t =
                Mathf.SmoothStep(
                    0,
                    1,
                    time / duration
                );

            int value =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start,
                        target,
                        t
                    )
                );

            text.text =
                prefix +
                value +
                suffix;

            yield return null;
        }

        currentValue =
            target;

        text.text =
            prefix +
            target +
            suffix;
    }

    public void ResetCount()
    {
        currentValue = 0;

        text.text =
            prefix +
            "0" +
            suffix;
    }
}