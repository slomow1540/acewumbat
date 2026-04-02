using System.Collections;
using UnityEngine;

public class Overlay : MonoBehaviour
{
    private Renderer rend;
    public float fadeSpeed = 5f;

    private Material mat;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mat = rend.material;
    }

    public void FadeTo(float targetAlpha)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float target)
    {
        Color c = mat.color;

        while (Mathf.Abs(c.a - target) > 0.01f)
        {
            c.a = Mathf.Lerp(c.a, target, Time.deltaTime * fadeSpeed);
            mat.color = c;
            yield return null;
        }

        c.a = target;
        mat.color = c;
    }
}
