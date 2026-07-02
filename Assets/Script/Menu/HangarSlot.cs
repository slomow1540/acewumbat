using System.Collections;
using UnityEngine;

public class HangarSlot : MonoBehaviour
{
    public Transform anchor;
    public Transform cameraPoint;
    public Transform planePoint;

    [HideInInspector]
    public PlaneData plane;

    private GameObject spawnedPlane;

    Vector3 basePos;
    Vector3 hiddenPos;

    public Transform LookTarget
    {
        get
        {
            if (spawnedPlane != null)
                return spawnedPlane.transform;

            return planePoint;
        }
    }

    public void RefreshPosition()
    {
        basePos = transform.localPosition;
        hiddenPos = basePos + new Vector3(0f, 0f, -0.04f);
    }

    public void Setup(PlaneData data)
    {
        plane = data;

        if (spawnedPlane != null)
            Destroy(spawnedPlane);

        if (data != null && data.prefab != null)
        {
            spawnedPlane = Instantiate(data.prefab, planePoint);

            spawnedPlane.transform.localPosition = Vector3.zero;

            spawnedPlane.transform.localRotation = Quaternion.identity;

            spawnedPlane.transform.localScale = Vector3.one;
        }
    }

    public void Show(float delay = 0f)
    {
        StopAllCoroutines();

        gameObject.SetActive(true);

        StartCoroutine(AnimateShow(delay));
    }

    public void Hide(float delay = 0f)
    {
        if (!gameObject.activeSelf)
            return;

        StopAllCoroutines();

        StartCoroutine(AnimateHide(delay));
    }

    IEnumerator AnimateHide(float delay)
    {
        yield return new WaitForSeconds(delay);

        float t = 0f;

        Vector3 startPos = basePos;
        Vector3 endPos = hiddenPos;

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;

            float e = Mathf.SmoothStep(0, 1, t);

            transform.localPosition = Vector3.Lerp(startPos, endPos, e);

            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.85f, e);

            yield return null;
        }

        gameObject.SetActive(false);
    }

    IEnumerator AnimateShow(float delay)
    {
        yield return new WaitForSeconds(delay);

        gameObject.SetActive(true);

        float t = 0f;

        transform.localPosition = hiddenPos;
        transform.localScale = Vector3.one * 0.85f;

        while (t < 1f)
        {
            t += Time.deltaTime * 3f;

            float e = Mathf.SmoothStep(0, 1, t);

            transform.localPosition = Vector3.Lerp(hiddenPos, basePos, e);

            transform.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, e);

            yield return null;
        }

        transform.localPosition = basePos;
    }

    public void ShowInstant()
    {
        StopAllCoroutines();

        transform.localScale = Vector3.one;

        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg != null)
            cg.alpha = 1f;
    }

    public void HideInstant()
    {
        StopAllCoroutines();

        transform.localScale = Vector3.zero;

        CanvasGroup cg = GetComponent<CanvasGroup>();

        if (cg != null)
            cg.alpha = 0f;
    }
}
