using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform[] points;

    public float moveDuration = 1.5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentMove;

    public void MoveTo(int index)
    {
        if (index < 0 || index >= points.Length)
            return;

        if (currentMove != null)
            StopCoroutine(currentMove);

        currentMove = StartCoroutine(MoveRoutine(points[index]));
    }

    public void MoveTo(Transform target)
    {
        if (currentMove != null)
            StopCoroutine(currentMove);

        currentMove = StartCoroutine(MoveRoutine(target));
    }

    IEnumerator MoveRoutine(Transform target)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Vector3 targetPos = target.position;
        Quaternion targetRot = target.rotation;

        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float t = time / moveDuration;

            float eased = easeCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        currentMove = null;
    }
}
