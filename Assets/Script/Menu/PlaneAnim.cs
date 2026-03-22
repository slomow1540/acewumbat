using UnityEngine;

public class PlaneAnim : MonoBehaviour
{
    public enum PlaneState
    {
        Idle,
        Mission,
        Survival,
    }

    [Header("Current State")]
    public PlaneState currentState = PlaneState.Idle;

    [Header("Base Motion")]
    public float moveAmount = 32f;
    public float moveSpeed = 1f;

    public float verticalAmount = 0.5f;
    public float verticalSpeed = 1f;

    [Header("Shake")]
    public float shakeAmount = 0.1f;
    public float shakeSpeed = 5f;

    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 stateOffset = Vector3.zero;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void Update()
    {
        ApplyMotion();
    }

    void ApplyMotion()
    {
        float move = 0f;
        float vertical = 0f;
        float shake = 0f;

        switch (currentState)
        {
            case PlaneState.Idle:
                move = 0;
                vertical = Mathf.Sin(Time.time * verticalSpeed) * verticalAmount * 0.5f;
                shake = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * shakeAmount * 1.2f;
                break;

            case PlaneState.Mission:
                move = 0;
                vertical = Mathf.Sin(Time.time * verticalSpeed) * verticalAmount;
                shake = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * shakeAmount * 1.5f;
                break;

            case PlaneState.Survival:
                move = Mathf.Sin(Time.time * moveSpeed * 1.5f) * moveAmount;
                vertical = Mathf.Sin(Time.time * verticalSpeed * 1.2f) * verticalAmount * 1.2f;
                shake = Mathf.PerlinNoise(Time.time * shakeSpeed * 2f, 0f) * shakeAmount * 2f;
                break;
        }

        Vector3 offset =
            transform.right * move
            + transform.forward * vertical
            + transform.up * (shake - 0.5f * shakeAmount);

        transform.position = startPos + stateOffset + offset;

        float pitch = vertical * 2f;
        float roll = shake * 10f;

        transform.rotation = startRot * Quaternion.Euler(pitch, 0f, roll);
    }

    public void SetState(PlaneState state)
    {
        currentState = state;

        switch (state)
        {
            case PlaneState.Idle:
                stateOffset = Vector3.zero;
                break;

            case PlaneState.Mission:
                stateOffset = Vector3.zero;
                break;

            case PlaneState.Survival:
                stateOffset = -transform.up * 32f;
                break;
        }
    }

    public void SetMoveAmount(float value)
    {
        moveAmount = value;
    }

    public void SetShakeAmount(float value)
    {
        shakeAmount = value;
    }
}
