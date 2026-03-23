using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform[] points;
    private Transform currentTarget;

    public float moveDuration = 1.5f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentMove;
    private int currentIndex = 0;

    private AudioManager audioManager;
    public AudioClip moveSound;

    void Start()
    {
        audioManager = AudioManager.Instance;
    }

    public void MoveTo(int index)
    {
        if (index < 0 || index >= points.Length)
            return;

        if (currentIndex == index)
            return;

        currentIndex = index;

        if (currentMove != null)
            StopCoroutine(currentMove);

        if (audioManager != null && moveSound != null)
        {
            audioManager.Play(moveSound);
        }

        currentMove = StartCoroutine(MoveRoutine(points[index]));
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
