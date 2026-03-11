using UnityEngine;
using TMPro;

public class ImprovedPlaneController : MonoBehaviour
{
    [Header("Thrust Settings")]
    [Tooltip("How much thrust acceleration per second")]
    public float thrustAcceleration = 2f;
    [Tooltip("Maximum thrust force")]
    public float maxThrust = 2000f;

    [Header("Control Responsiveness")]
    [Tooltip("Roll responsiveness (aileron control)")]
    public float rollResponsiveness = 15f;
    [Tooltip("Pitch responsiveness (elevator control)")]
    public float pitchResponsiveness = 12f;
    [Tooltip("Yaw responsiveness (rudder control)")]
    public float yawResponsiveness = 8f;
    [Tooltip("Minimum speed for controls to work effectively")]
    public float minControlSpeed = 20f;
    [Tooltip("Maximum angular velocity (degrees per second) for each axis")]
    public float maxAngularVelocity = 180f;

    [Header("Aerodynamics")]
    [Tooltip("Lift coefficient - higher = more lift")]
    public float liftCoefficient = 0.2f;
    [Tooltip("Drag coefficient - higher = more air resistance")]
    public float dragCoefficient = 0.01f;
    [Tooltip("Sideways drag multiplier (prevents sliding)")]
    public float sidewaysDragMultiplier = 5f;
    [Tooltip("Forward velocity bias (keeps plane moving forward)")]
    public float forwardVelocityBias = 2f;
    [Tooltip("Angular drag for realistic rotation dampening")]
    public float angularDragCoefficient = 2f;
    [Tooltip("How much the plane wants to auto-level")]
    public float stabilityFactor = 0.5f;
    [Tooltip("Air density factor (affects all aerodynamic forces)")]
    public float airDensity = 1.225f;

    [Header("Perceived Speed Settings")]
    [Tooltip("Multiplier for speedometer display and G-force calculation (doesn't affect actual physics speed)")]
    public float perceivedSpeedMultiplier = 3f;

    [Header("G-Force Limiter")]
    [Tooltip("Maximum G-force the plane can pull")]
    public float maxGForce = 9f;
    [Tooltip("How quickly G-force limiting kicks in (0-1)")]
    [Range(0f, 1f)]
    public float gForceLimiterStrength = 0.8f;
    [Tooltip("How sensitive G-force is to turning (lower = more forgiving, higher = spikes faster)")]
    [Range(0.01f, 2f)]
    public float turnGForceMultiplier = 0.5f;
    [Tooltip("How much turn rate is reduced when over G-limit (0-1, higher = more reduction)")]
    [Range(0f, 1f)]
    public float turnRateLossOverGLimit = 0.7f;

    [Header("High-G Maneuver Mode (Hold 2)")]
    [Tooltip("Control responsiveness multiplier in high-G mode")]
    public float highGModeControlMultiplier = 2.5f;
    [Tooltip("Speed loss per second in high-G mode")]
    public float highGModeSpeedLoss = 15f;
    [Tooltip("G-force limit increase in high-G mode")]
    public float highGModeLimitBoost = 3f;

    [Header("UI")]
    public TextMeshProUGUI thrustText;
    public TextMeshProUGUI gForceText;
    public TextMeshProUGUI speedText;

    // Public variables
    public float thrust;
    public float currentGForce;

    // Private variables
    private float roll;
    private float pitch;
    private float yaw;
    private Vector3 lastVelocity;
    private bool isHighGMode;

    private Rigidbody rb;

    // Responsive modifiers for each axis
    private float RollModifier => (rb.mass / 10f) * rollResponsiveness;
    private float PitchModifier => (rb.mass / 10f) * pitchResponsiveness;
    private float YawModifier => (rb.mass / 10f) * yawResponsiveness;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastVelocity = rb.linearVelocity;
    }

    private void HandleInputs()
    {
        // Check if we have AI input component
        AIPlaneInput aiInput = GetComponent<AIPlaneInput>();

        if (aiInput != null)
        {
            // AI controlled
            roll = aiInput.aiRollInput;
            pitch = aiInput.aiPitchInput;
            yaw = aiInput.aiYawInput;

            // AI thrust control
            if (thrust < aiInput.aiThrustTarget)
                thrust += thrustAcceleration * Time.deltaTime * 100f;
            else if (thrust > aiInput.aiThrustTarget)
                thrust -= thrustAcceleration * Time.deltaTime * 100f;

            // AI high-G mode
            isHighGMode = aiInput.aiHighGMode;
        }
        else
        {
            // Player controlled (original code)
            roll = Input.GetAxis("Roll");
            pitch = Input.GetAxis("Pitch");
            yaw = Input.GetAxis("Yaw");

            // High-G maneuver mode (Hold 2 key)
            isHighGMode = Input.GetKey(KeyCode.Alpha2);

            // Thrust control
            if (Input.GetKey(KeyCode.Space))
                thrust += thrustAcceleration * Time.deltaTime * 100f;
            else if (Input.GetKey(KeyCode.LeftControl))
                thrust -= thrustAcceleration * Time.deltaTime * 100f;
        }

        thrust = Mathf.Clamp(thrust, 0f, 100f);
    }

    private void Update()
    {
        HandleInputs();
        UpdateUI();
    }

    private void FixedUpdate()
    {
        // Calculate G-force (using perceived speed)
        CalculateGForce();

        // Apply thrust
        ApplyThrust();

        // Apply aerodynamics (lift and drag)
        ApplyAerodynamics();

        // Apply control inputs with G-force limiting
        ApplyControlInputs();

        // Apply stability assistance
        ApplyStability();

        // Limit angular velocity
        LimitAngularVelocity();

        // Store velocity for next frame's G-force calculation
        lastVelocity = rb.linearVelocity;
    }

    private void ApplyThrust()
    {
        // Thrust uses REAL speed (no multiplier)
        float thrustForce = maxThrust * (thrust / 100f);
        rb.AddForce(transform.forward * thrustForce);

        // High-G maneuver mode speed loss (air brake effect)
        if (isHighGMode && rb.linearVelocity.magnitude > 1f)
        {
            // Lose speed when pulling high-G maneuvers (simulates energy bleed)
            Vector3 speedLoss = -rb.linearVelocity.normalized * highGModeSpeedLoss * rb.mass;
            rb.AddForce(speedLoss);
        }
    }

    private void ApplyAerodynamics()
    {
        if (rb.linearVelocity.magnitude < 0.1f) return; // Skip if barely moving

        // Calculate velocity and speed (REAL speed for physics)
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float speed = rb.linearVelocity.magnitude;
        float speedSquared = speed * speed;

        // === DIRECTIONAL DRAG (high drag for sideways/vertical movement) ===
        // This prevents "ice skating" behavior
        float forwardDrag = dragCoefficient * localVelocity.z * localVelocity.z * airDensity;
        float sidewaysDrag = dragCoefficient * sidewaysDragMultiplier * localVelocity.x * localVelocity.x * airDensity;
        float verticalDrag = dragCoefficient * sidewaysDragMultiplier * localVelocity.y * localVelocity.y * airDensity;

        Vector3 localDrag = new Vector3(
            -Mathf.Sign(localVelocity.x) * sidewaysDrag,
            -Mathf.Sign(localVelocity.y) * verticalDrag,
            -Mathf.Sign(localVelocity.z) * forwardDrag
        );
        rb.AddRelativeForce(localDrag);

        // === FORWARD VELOCITY BIAS (gently pushes velocity toward forward direction) ===
        // Only apply when moving sideways/vertically significantly
        if (Mathf.Abs(localVelocity.x) > 1f || Mathf.Abs(localVelocity.y) > 1f)
        {
            // Add force toward the forward direction
            float biasForce = forwardVelocityBias * rb.mass;
            rb.AddForce(transform.forward * biasForce);

            // Add extra resistance to sideways motion
            rb.AddRelativeForce(new Vector3(-localVelocity.x * 0.5f, -localVelocity.y * 0.5f, 0f));
        }

        // === LIFT FORCE ===
        float angleOfAttack = Vector3.Dot(transform.up, rb.linearVelocity.normalized);

        if (angleOfAttack > 0)
        {
            Vector3 liftDirection = Vector3.Cross(rb.linearVelocity, transform.right).normalized;
            float liftMagnitude = liftCoefficient * speedSquared * angleOfAttack * airDensity;
            rb.AddForce(liftDirection * liftMagnitude);
        }

        // === INDUCED DRAG ===
        float inducedDrag = Mathf.Abs(angleOfAttack) * dragCoefficient * speedSquared * 2f * airDensity;
        rb.AddForce(-rb.linearVelocity.normalized * inducedDrag);

        // === ANGULAR DRAG ===
        float angularDragFactor = angularDragCoefficient * (1f + speed * 0.01f);
        Vector3 angularDrag = -rb.angularVelocity * angularDragFactor;
        rb.AddTorque(angularDrag);
    }

    private void CalculateGForce()
    {
        // Calculate G-force primarily from TURNING (centripetal acceleration)
        // Real planes experience G-forces from turning, not from speed changes

        float perceivedSpeed = rb.linearVelocity.magnitude * perceivedSpeedMultiplier;

        // Centripetal acceleration = v² / r, where v²/r can be approximated as v * ω
        // This gives us the "pull" felt during turns
        float angularVelocityMagnitude = rb.angularVelocity.magnitude;
        float centripetalAcceleration = perceivedSpeed * angularVelocityMagnitude;

        // Convert to G-force and apply multiplier for tuning sensitivity
        // Lower multiplier (e.g. 0.3) = more forgiving, gentle inputs don't spike G's
        // Higher multiplier (e.g. 1.0+) = more sensitive, quick to hit G-limit
        float turnGForce = (centripetalAcceleration / 9.81f) * turnGForceMultiplier;

        // Also include linear acceleration (speed changes) but with less weight
        Vector3 perceivedVelocity = rb.linearVelocity * perceivedSpeedMultiplier;
        Vector3 perceivedLastVelocity = lastVelocity * perceivedSpeedMultiplier;
        Vector3 linearAcceleration = (perceivedVelocity - perceivedLastVelocity) / Time.fixedDeltaTime;
        float linearGForce = linearAcceleration.magnitude / 9.81f;

        // Turning is the primary G-force source (weighted more heavily)
        currentGForce = turnGForce + (linearGForce * 0.2f);

        // Add constant 1G from gravity when flying level
        currentGForce += 1f;
    }

    private void ApplyControlInputs()
    {
        // Calculate control surface effectiveness based on airspeed (REAL speed)
        float currentSpeed = rb.linearVelocity.magnitude;
        float controlEffectiveness = Mathf.Clamp01(currentSpeed / minControlSpeed);

        // Determine effective G-force limit (boosted in high-G mode)
        float effectiveMaxGForce = isHighGMode ? maxGForce + highGModeLimitBoost : maxGForce;

        // Calculate G-force limiter with SMOOTH GRADUAL transition
        // Start reducing authority at 70% of max, smoothly transition to minimum
        float gForceLimiter = 1f;
        float softLimitStart = effectiveMaxGForce * 0.7f;

        if (currentGForce > softLimitStart)
        {
            // Calculate how far into the limiting zone we are (0 to 1)
            float limitZoneRange = effectiveMaxGForce * 0.5f; // 70% to 120% of max
            float gForceRatio = (currentGForce - softLimitStart) / limitZoneRange;
            gForceRatio = Mathf.Clamp01(gForceRatio);

            // Use smooth exponential curve for gradual feel
            float smoothCurve = 1f - Mathf.Pow(gForceRatio, 2f); // Quadratic falloff
            gForceLimiter = Mathf.Lerp(0.3f, 1f, smoothCurve);

            // Apply limiter strength setting
            gForceLimiter = Mathf.Lerp(1f, gForceLimiter, gForceLimiterStrength);
        }

        // High-G mode control boost
        float controlBoost = isHighGMode ? highGModeControlMultiplier : 1f;

        // Combined effectiveness
        float finalEffectiveness = controlEffectiveness * gForceLimiter * controlBoost;

        // Apply torques
        rb.AddTorque(transform.up * yaw * YawModifier * finalEffectiveness);
        rb.AddTorque(transform.right * pitch * PitchModifier * finalEffectiveness);
        rb.AddTorque(-transform.forward * roll * RollModifier * finalEffectiveness);
    }

    private void LimitAngularVelocity()
    {
        float maxAngularVelocityRad = maxAngularVelocity * Mathf.Deg2Rad;

        // Apply turn rate reduction when over G-limit
        float turnRateMultiplier = 1f;
        if (currentGForce > maxGForce)
        {
            // Reduce maximum turn rate when pulling too many G's
            float gForceExcess = (currentGForce - maxGForce) / maxGForce;
            turnRateMultiplier = Mathf.Lerp(1f, 1f - turnRateLossOverGLimit, Mathf.Clamp01(gForceExcess));
            maxAngularVelocityRad *= turnRateMultiplier;
        }

        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        localAngularVelocity.x = Mathf.Clamp(localAngularVelocity.x, -maxAngularVelocityRad, maxAngularVelocityRad);
        localAngularVelocity.y = Mathf.Clamp(localAngularVelocity.y, -maxAngularVelocityRad, maxAngularVelocityRad);
        localAngularVelocity.z = Mathf.Clamp(localAngularVelocity.z, -maxAngularVelocityRad, maxAngularVelocityRad);

        rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }

    private void ApplyStability()
    {
        if (Mathf.Abs(roll) < 0.1f && Mathf.Abs(pitch) < 0.1f)
        {
            Vector3 predictedUp = Quaternion.AngleAxis(
                rb.angularVelocity.magnitude * Mathf.Rad2Deg * stabilityFactor / rb.mass,
                rb.angularVelocity
            ) * transform.up;

            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * stabilityFactor * rb.mass);
        }
    }

    private void UpdateUI()
    {
        if (thrustText != null)
        {
            string highGIndicator = isHighGMode ? " [HIGH-G]" : "";
            thrustText.text = $"Thrust: {thrust:F0}%{highGIndicator}";
        }

        if (gForceText != null)
        {
            // Show current G-force and effective limit
            float effectiveMaxGForce = isHighGMode ? maxGForce + highGModeLimitBoost : maxGForce;
            gForceText.text = $"G-Force: {currentGForce:F1}G / {effectiveMaxGForce:F0}G";

            // Change color based on effective limit
            if (currentGForce > effectiveMaxGForce)
                gForceText.color = Color.red;
            else if (currentGForce > effectiveMaxGForce * 0.8f)
                gForceText.color = Color.yellow;
            else
                gForceText.color = Color.white;
        }

        if (speedText != null)
        {
            // Display PERCEIVED speed (multiplied)
            float perceivedSpeed = rb.linearVelocity.magnitude * perceivedSpeedMultiplier;
            speedText.text = $"Speed: {perceivedSpeed:F0} m/s";
        }
    }
}