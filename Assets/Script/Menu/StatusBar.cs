using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusBar : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text valueText;
    public Image fill;

    [Header("Setting")]
    public float duration = 0.5f;
    public float maxValue = 100f;

    Coroutine routine;

    float currentValue;

    public void SetValue(float target)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(Animate(target));
    }

    IEnumerator Animate(float target)
    {
        float start = currentValue;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, time / duration);

            float value = Mathf.Lerp(start, target, t);

            // Fill image
            fill.fillAmount = value / maxValue;

            // Number count up
            valueText.text = Mathf.RoundToInt(value).ToString();

            yield return null;
        }

        currentValue = target;

        fill.fillAmount = target / maxValue;

        valueText.text = Mathf.RoundToInt(target).ToString();
    }

    public void ResetBar()
    {
        currentValue = 0;

        fill.fillAmount = 0f;

        valueText.text = "0";
    }
}
