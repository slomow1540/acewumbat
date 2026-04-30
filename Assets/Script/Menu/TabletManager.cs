using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class TabletManager : PopOut
{
    [Header("UI References")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI threatText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI scoreText;

    public Image previewImage;

    [Header("Animation")]
    public float changeDuration = 0.25f;
    public CanvasGroup canvasGroup;

    private Coroutine changeRoutine;
    private int currentIndex;

    void Awake()
    {
        base.Awake();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }


    public void SetData(LevelData data, int index)
    {
        if (changeRoutine != null)
            StopCoroutine(changeRoutine);

        currentIndex = index;

        changeRoutine = StartCoroutine(AnimateChange(data));
    }


    IEnumerator AnimateChange(LevelData data)
    {
        // fade out
        yield return Fade(1f, 0f);

        // update data
        ApplyData(data);

        // sedikit delay biar feel "loading"
        yield return new WaitForSeconds(0.05f);

        // fade in
        yield return Fade(0f, 1f);
    }

    IEnumerator Fade(float from, float to)
    {
        float time = 0f;

        while (time < changeDuration)
        {
            time += Time.deltaTime;
            float t = time / changeDuration;

            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    void ApplyData(LevelData data)
    {
        levelText.text = "OPERATION_" + currentIndex.ToString("00");
        titleText.text = data.title;
        locationText.text = data.location;
        threatText.text = data.threat;

        previewImage.sprite = data.previewImage;

        int score = ProgressManager.GetScore(currentIndex);
        float time = ProgressManager.GetTime(currentIndex);

        if (score > 0)
            scoreText.text = score.ToString("N0");
        else
            scoreText.text = "---";

        if (time > 0f)
            timeText.text = FormatTime(time);
        else
            timeText.text = "--:--";
    }

    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}