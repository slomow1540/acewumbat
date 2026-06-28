using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [Header("Menu Camera")]
    public Transform[] menuPoints;

    public float moveDuration = 1.5f;

    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine currentMove;

    private AudioManager audioManager;

    public AudioClip moveSound;

    public enum CameraMode
    {
        Menu,
        Hangar,
    }

    private CameraMode mode = CameraMode.Menu;

    void Start()
    {
        audioManager = AudioManager.Instance;
    }

    public void SetMenuMode()
    {
        mode = CameraMode.Menu;
    }

    public void SetHangarMode()
    {
        mode = CameraMode.Hangar;
    }

    // =================
    // MENU
    // =================
    public void MoveTo(int index)
    {
        if (mode != CameraMode.Menu)
            return;

        if (index < 0 || index >= menuPoints.Length)
        {
            return;
        }

        Move(menuPoints[index]);
    }

    // =================
    // HANGAR
    // =================
    public void MoveToTransform(Transform target)
    {
        if (mode != CameraMode.Hangar)
            return;

        if (target == null)
            return;

        Move(target);
    }

    void Move(Transform target)
    {
        if (currentMove != null)
        {
            StopCoroutine(currentMove);
        }

        if (audioManager != null && moveSound != null)
        {
            audioManager.Play(moveSound, AudioManager.AudioChannel.Camera);
        }

        currentMove = StartCoroutine(MoveRoutine(target));
    }

    IEnumerator MoveRoutine(Transform target)
    {
        Vector3 startPos = transform.position;

        Quaternion startRot = transform.rotation;

        Vector3 targetPos = target.position;

        Quaternion targetRot;

        // ===== MENU =====
        if (mode == CameraMode.Menu)
        {
            targetRot = target.rotation;
        }
        // ===== HANGAR =====
        else
        {
            Transform lookTarget = target.parent;

            Vector3 dir = (lookTarget.position - targetPos).normalized;

            targetRot = Quaternion.LookRotation(dir, Vector3.up);
        }

        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveDuration);

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
