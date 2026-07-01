using UnityEngine;

/// <summary>
/// Ace Combat style camera with orbital free look
/// Attach as child of player plane
/// </summary>
public class PlaneCamera : MonoBehaviour
{
    [Header("Smooth Follow Settings")]
    [Tooltip("How quickly camera follows plane position")]
    [Range(1f, 50f)]
    public float positionSmoothSpeed = 30f;

    [Tooltip("How quickly camera rotation follows plane (lower = more lag)")]
    [Range(1f, 50f)]
    public float rotationSmoothSpeed = 15f;

    [Header("Orbital Free Look")]
    [Tooltip("Key to hold for orbital free look")]
    public KeyCode lookAroundKey = KeyCode.LeftAlt;

    [Tooltip("Mouse sensitivity for orbital look")]
    [Range(0.5f, 10f)]
    public float orbitSensitivity = 2f;

    [Tooltip("Distance camera orbits from plane center")]
    [Range(0.1f, 20f)]
    public float orbitDistance = 1.5f;

    [Tooltip("Maximum vertical orbit angle (degrees)")]
    public float maxVerticalOrbit = 80f;

    [Tooltip("How quickly camera returns to default")]
    [Range(1f, 30f)]
    public float returnSpeed = 15f;

    [Tooltip("Smooth camera movement during orbit")]
    [Range(1f, 50f)]
    public float orbitSmoothSpeed = 20f;

    [Header("Default Camera Position")]
    [Tooltip("Default local position relative to plane")]
    public Vector3 defaultLocalPosition = new Vector3(0f, 0.075f, -0.25f);

    [Header("Optional: G-Force Tilt")]
    [Tooltip("Enable camera tilt based on plane rotation")]
    public bool enableGForceTilt = false;

    [Tooltip("Maximum camera tilt from G-forces (degrees)")]
    [Range(0f, 20f)]
    public float maxGForceTilt = 5f;

    [Tooltip("How quickly G-force tilt responds")]
    [Range(1f, 10f)]
    public float gForceTiltSpeed = 3f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // References
    private Transform planeTransform;

    // Target state in world space
    private Vector3 targetWorldPosition;
    private Quaternion targetWorldRotation;

    // Orbital free look
    private bool isLookingAround = false;
    private bool wasLookingAround = false;
    private float orbitYaw = 0f;
    private float orbitPitch = 0f;

    // G-Force and maneuver tracking
    private float currentGForceTilt = 0f;
    private Quaternion previousPlaneRotation;

    private void Start()
    {
        planeTransform = transform.parent;

        if (planeTransform == null)
        {
            Debug.LogError("PlaneCamera must be a child of the plane!");
            enabled = false;
            return;
        }

        previousPlaneRotation = planeTransform.rotation;
        ResetCameraImmediate();
    }

    private void LateUpdate()
    {
        if (planeTransform == null)
            return;

        wasLookingAround = isLookingAround;
        isLookingAround = Input.GetKey(lookAroundKey);

        if (isLookingAround && !wasLookingAround)
        {
            EnterOrbitMode();
        }

        if (isLookingAround)
        {
            HandleOrbitalLook();
        }
        else
        {
            ReturnToDefault();
        }

        if (enableGForceTilt && !isLookingAround)
        {
            CalculateGForceTilt();
        }
        else
        {
            currentGForceTilt = Mathf.Lerp(currentGForceTilt, 0f, Time.deltaTime * gForceTiltSpeed);
        }

        ApplySmoothMovement();

        previousPlaneRotation = planeTransform.rotation;

        if (showDebugInfo)
        {
            Debug.Log(
                $"Orbit Mode: {isLookingAround} | Yaw: {orbitYaw:F1}° | Pitch: {orbitPitch:F1}° | LocalPos: {transform.localPosition}"
            );
        }
    }

    private void EnterOrbitMode()
    {
        orbitYaw = 0f;
        orbitPitch = 0f;
    }

    private void HandleOrbitalLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * orbitSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * orbitSensitivity;

        orbitYaw += mouseX;
        orbitPitch -= mouseY;

        if (orbitYaw > 180f)
            orbitYaw -= 360f;
        if (orbitYaw < -180f)
            orbitYaw += 360f;

        orbitPitch = Mathf.Clamp(orbitPitch, -maxVerticalOrbit, maxVerticalOrbit);

        CalculateOrbitPosition();
    }

    private void CalculateOrbitPosition()
    {
        Vector3 planeCenter = planeTransform.position;
        float yawRad = orbitYaw * Mathf.Deg2Rad;
        float pitchRad = orbitPitch * Mathf.Deg2Rad;

        Vector3 planeForward = planeTransform.forward;
        Vector3 planeRight = planeTransform.right;
        Vector3 planeUp = planeTransform.up;

        float horizontalDist = orbitDistance * Mathf.Cos(pitchRad);
        Vector3 horizontalOffset =
            (-planeForward * Mathf.Cos(yawRad) + planeRight * Mathf.Sin(yawRad)) * horizontalDist;
        Vector3 verticalOffset = planeUp * (orbitDistance * Mathf.Sin(pitchRad));

        targetWorldPosition = planeCenter + horizontalOffset + verticalOffset;

        Vector3 directionToPlane = planeCenter - targetWorldPosition;
        if (directionToPlane.sqrMagnitude > 0.001f)
        {
            targetWorldRotation = Quaternion.LookRotation(directionToPlane, planeUp);
        }
    }

    private void ReturnToDefault()
    {
        float returnDelta = Time.deltaTime * returnSpeed;

        orbitYaw = Mathf.MoveTowards(orbitYaw, 0f, returnDelta * 60f);
        orbitPitch = Mathf.MoveTowards(orbitPitch, 0f, returnDelta * 60f);

        if (Mathf.Abs(orbitYaw) < 0.5f)
            orbitYaw = 0f;
        if (Mathf.Abs(orbitPitch) < 0.5f)
            orbitPitch = 0f;

        // Always calculate default position based on current local position target
        targetWorldPosition = planeTransform.TransformPoint(defaultLocalPosition);
        targetWorldRotation = planeTransform.rotation;
    }

    private void CalculateGForceTilt()
    {
        Rigidbody rb = planeTransform.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Get angular velocity
            Vector3 angularVel = rb.angularVelocity;

            // Roll (rotation around forward axis)
            float rollRate = Vector3.Dot(angularVel, planeTransform.forward);

            // Pitch (rotation around right axis)
            float pitchRate = Vector3.Dot(angularVel, planeTransform.right);

            // Yaw (rotation around up axis)
            float yawRate = Vector3.Dot(angularVel, planeTransform.up);

            // Apply tilt based on roll primarily, pitch secondarily
            float rollTilt = Mathf.Clamp(-rollRate * maxGForceTilt, -maxGForceTilt, maxGForceTilt);
            float pitchTilt = Mathf.Clamp(
                pitchRate * (maxGForceTilt * 0.5f),
                -maxGForceTilt * 0.5f,
                maxGForceTilt * 0.5f
            );

            float targetTilt = rollTilt + pitchTilt;
            targetTilt = Mathf.Clamp(targetTilt, -maxGForceTilt, maxGForceTilt);

            currentGForceTilt = Mathf.Lerp(
                currentGForceTilt,
                targetTilt,
                Time.deltaTime * gForceTiltSpeed
            );
        }
    }

    private void ApplySmoothMovement()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            targetWorldPosition,
            Time.deltaTime * positionSmoothSpeed
        );

        Quaternion finalRotation = targetWorldRotation;

        if (enableGForceTilt && !isLookingAround)
        {
            Quaternion gForceTiltRotation = Quaternion.Euler(0f, 0f, currentGForceTilt);
            finalRotation = targetWorldRotation * gForceTiltRotation;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            Time.deltaTime * rotationSmoothSpeed
        );
    }

    public void SetDefaultPosition(Vector3 localOffset)
    {
        defaultLocalPosition = localOffset;
    }

    public void ResetCameraImmediate()
    {
        orbitYaw = 0f;
        orbitPitch = 0f;
        currentGForceTilt = 0f;

        transform.localPosition = defaultLocalPosition;
        transform.localRotation = Quaternion.identity;
    }

    public bool IsLookingAround()
    {
        return isLookingAround;
    }

    public Vector2 GetLookAngles()
    {
        return new Vector2(orbitYaw, orbitPitch);
    }

    public void SetOrbitDistance(float distance)
    {
        orbitDistance = Mathf.Clamp(distance, 0.1f, 5f);
    }
}
