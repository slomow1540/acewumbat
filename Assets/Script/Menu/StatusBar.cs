using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StatusBar
    : MonoBehaviour
{
    public Slider slider;

    public float duration =
        0.5f;

    Coroutine routine;

    public void SetValue(
        float target
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
        float target
    )
    {
        float start =
            slider.value;

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

            slider.value =
                Mathf.Lerp(
                    start,
                    target,
                    t
                );

            yield return null;
        }

        slider.value =
            target;
    }

    public void ResetBar()
    {
        slider.value = 0;
    }
}