using UnityEngine;

/// <summary>
/// Enemy plane AI that flies realistically and has combat behaviors
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyPlaneAI : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Tag to identify targets (e.g., 'Player')")]
    public string targetTag = "Player";
    [Tooltip("Current target")]
    public GameObject currentTarget;
    [Tooltip("Detection range")]
    public float detectionRange = 2000f;
    [Tooltip("How often to check for targets (seconds)")]
    public float targetUpdateRate = 1f;

    [Header("Flight Settings")]
    [Tooltip("Reference to AI input interface")]
    public AIPlaneInput aiInput;
    [Tooltip("Preferred cruise speed (percentage 0-100)")]
    public float cruiseSpeed = 60f;
    [Tooltip("Combat speed when engaging (percentage 0-100)")]
    public float combatSpeed = 80f;
    [Tooltip("Evasion speed (percentage 0-100)")]
    public float evasionSpeed = 100f;

    [Header("Combat Behavior")]
    [Tooltip("Current AI behavior mode")]
    public BehaviorMode currentBehavior = BehaviorMode.Chasing;
    [Tooltip("Weapon system reference")]
    public EnemyAI weaponSystem;

    [Header("Aggressive Behavior")]
    [Tooltip("Just point at target and shoot")]
    public float aggressiveRange = 600f;

    [Header("Chasing Behavior")]
    [Tooltip("Preferred attack range")]
    public float optimalChaseRange = 400f;
    [Tooltip("Maximum chase range")]
    public float maxChaseRange = 800f;

    [Header("Opportunistic Behavior")]
    [Tooltip("Preferred missile range")]
    public float optimalMissileRange = 1000f;

    [Header("Evasion Behavior")]
    [Tooltip("Distance to maintain from threats")]
    public float evasionDistance = 600f;
    [Tooltip("How aggressive evasive maneuvers are (0-1)")]
    [Range(0f, 1f)]
    public float evasionAggressiveness = 0.8f;
    [Tooltip("Tag for incoming missiles")]
    public string missileTag = "EnemyMissile";

    [Header("Behavior Switching")]
    [Tooltip("Health percentage to switch to evasion")]
    [Range(0f, 1f)]
    public float evasionHealthThreshold = 0.3f;
    [Tooltip("Distance to target before switching behaviors")]
    public float opportunisticSwitchDistance = 1200f;
    [Tooltip("Distance for aggressive behavior")]
    public float aggressiveSwitchDistance = 600f;

    [Header("Ground Avoidance")]
    [Tooltip("Minimum altitude to maintain (meters)")]
    public float minAltitude = 100f;
    [Tooltip("Altitude at which to start pulling up")]
    public float pullUpAltitude = 150f;
    [Tooltip("How aggressive the pull-up is")]
    public float pullUpStrength = 2f;
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer;

    // Private variables
    private Rigidbody rb;
    private Health health;
    private float nextTargetUpdateTime;
    private Vector3 desiredDirection;
    private float desiredPitch;
    private float desiredRoll;
    private float desiredYaw;
    private GameObject nearestMissile;
    private float lastMissileCheckTime;
    private float currentAltitude;

    public enum BehaviorMode
    {
        Aggressive,    // Point directly at target, use both weapons
        Chasing,       // Get behind target, use both weapons  
        Opportunistic, // Survive mode, use missile's wider FOV
        Evading        // Dodge threats
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();

        // Get AI input interface
        aiInput = GetComponent<AIPlaneInput>();
        if (aiInput == null)
        {
            aiInput = gameObject.AddComponent<AIPlaneInput>();
        }

        // Get weapon system (EnemyAI)
        if (weaponSystem == null)
        {
            weaponSystem = GetComponent<EnemyAI>();
        }
    }

    private void Update()
    {
        // Update target periodically
        if (Time.time >= nextTargetUpdateTime)
        {
            FindTarget();
            nextTargetUpdateTime = Time.time + targetUpdateRate;
        }

        // Check for incoming missiles periodically
        if (Time.time >= lastMissileCheckTime + 0.5f)
        {
            CheckForIncomingMissiles();
            lastMissileCheckTime = Time.time;
        }

        // Update current altitude
        UpdateAltitude();

        // Decide behavior
        DecideBehavior();

        // Execute current behavior
        switch (currentBehavior)
        {
            case BehaviorMode.Aggressive:
                ExecuteAggressiveBehavior();
                break;
            case BehaviorMode.Chasing:
                ExecuteChasingBehavior();
                break;
            case BehaviorMode.Opportunistic:
                ExecuteOpportunisticBehavior();
                break;
            case BehaviorMode.Evading:
                ExecuteEvadingBehavior();
                break;
        }

        // Ground avoidance (PRIORITY)
        CheckGroundAvoidance();

        // Apply flight controls
        ApplyFlightControls();
    }

    private void FindTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(targetTag);

        GameObject closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (GameObject target in potentialTargets)
        {
            // Check if target is alive
            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null && !targetHealth.IsAlive())
                continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance <= detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        currentTarget = closestTarget;
    }

    private void CheckForIncomingMissiles()
    {
        GameObject[] missiles = GameObject.FindGameObjectsWithTag(missileTag);

        GameObject nearestThreat = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject missile in missiles)
        {
            // Check if missile is targeting us
            Missile missileScript = missile.GetComponent<Missile>();
            if (missileScript != null && missileScript.target == gameObject)
            {
                float distance = Vector3.Distance(transform.position, missile.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestThreat = missile;
                }
            }
        }

        nearestMissile = nearestThreat;
    }

    private void UpdateAltitude()
    {
        // Raycast down to find ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1000f, groundLayer))
        {
            currentAltitude = hit.distance;
        }
        else
        {
            currentAltitude = transform.position.y; // Fallback to world Y
        }
    }

    private void DecideBehavior()
    {
        // Priority 1: Evade if under missile threat
        if (nearestMissile != null)
        {
            currentBehavior = BehaviorMode.Evading;
            return;
        }

        // Priority 2: Evade if low health
        if (health != null && health.GetHealthPercent() < evasionHealthThreshold)
        {
            currentBehavior = BehaviorMode.Evading;
            return;
        }

        // Priority 3: Combat behaviors based on distance
        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance < aggressiveSwitchDistance)
            {
                // Very close - just point and shoot
                currentBehavior = BehaviorMode.Aggressive;
            }
            else if (distance > opportunisticSwitchDistance)
            {
                // Long range - use missiles, stay defensive
                currentBehavior = BehaviorMode.Opportunistic;
            }
            else
            {
                // Medium range - classic dogfight
                currentBehavior = BehaviorMode.Chasing;
            }
        }
        else
        {
            // No target - cruise
            currentBehavior = BehaviorMode.Chasing;
        }
    }

    private void ExecuteAggressiveBehavior()
    {
        if (currentTarget == null)
        {
            FlyLevel();
            SetThrust(cruiseSpeed);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        // Lead the target for better accuracy
        Vector3 targetPosition = currentTarget.transform.position;
        Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();

        if (targetRb != null && targetRb.linearVelocity.magnitude > 1f)
        {
            // Lead target based on velocity
            float timeToIntercept = distance / Mathf.Max(rb.linearVelocity.magnitude, 1f);
            targetPosition += targetRb.linearVelocity * timeToIntercept * 0.5f;
        }

        // Calculate direction to target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // ANTI-STALL: Limit vertical angle to prevent pointing straight up/down
        float maxPitchAngle = 45f; // Don't pitch more than 45 degrees up/down

        // Get horizontal direction (on XZ plane)
        Vector3 horizontalDirection = new Vector3(directionToTarget.x, 0f, directionToTarget.z).normalized;

        // Calculate current pitch to target
        float pitchAngle = Mathf.Asin(directionToTarget.y) * Mathf.Rad2Deg;

        // Clamp pitch angle
        float clampedPitch = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        float clampedPitchRad = clampedPitch * Mathf.Deg2Rad;

        // Reconstruct direction with limited pitch
        Vector3 limitedDirection = horizontalDirection;
        if (horizontalDirection.magnitude > 0.01f)
        {
            // Apply limited pitch to horizontal direction
            limitedDirection = (horizontalDirection * Mathf.Cos(clampedPitchRad) +
                               Vector3.up * Mathf.Sin(clampedPitchRad)).normalized;
        }
        else
        {
            // Target directly above/below - spiral climb/dive instead
            limitedDirection = (transform.forward + Vector3.up * Mathf.Sign(directionToTarget.y) * 0.5f).normalized;
        }

        // Blend current direction with desired direction for smooth turns
        // This prevents sudden snapping that causes stalls
        float blendFactor = 0.7f; // How quickly to turn toward target
        desiredDirection = Vector3.Slerp(transform.forward, limitedDirection, blendFactor).normalized;

        // Check if we need high-G mode (tight turns)
        float angleToTarget = Vector3.Angle(transform.forward, limitedDirection);
        if (angleToTarget > 30f)
        {
            UseHighGMode(true);
        }
        else
        {
            UseHighGMode(false);
        }

        // CRITICAL: Maintain high speed to prevent stalls
        // Always use full combat speed or higher in aggressive mode
        if (distance < aggressiveRange * 0.5f)
        {
            // Close range - maintain speed for maneuverability
            SetThrust(combatSpeed * 0.9f);
        }
        else if (distance > aggressiveRange)
        {
            // Too far - full speed to close in
            SetThrust(evasionSpeed);
        }
        else
        {
            // Good range - combat speed
            SetThrust(combatSpeed);
        }
    }

    private void ExecuteChasingBehavior()
    {
        if (currentTarget == null)
        {
            FlyLevel();
            SetThrust(cruiseSpeed);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        // Get behind target
        Vector3 targetPosition = currentTarget.transform.position;

        // Lead the target based on relative velocity
        Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 relativeVelocity = targetRb.linearVelocity - rb.linearVelocity;
            float timeToIntercept = distance / Mathf.Max(rb.linearVelocity.magnitude, 1f);
            targetPosition += relativeVelocity * timeToIntercept * 0.5f;
        }

        // Try to get on their six (behind them)
        Vector3 optimalPosition = targetPosition;
        if (targetRb != null && targetRb.linearVelocity.magnitude > 10f)
        {
            // Position behind target's velocity vector
            Vector3 targetVelocityDir = targetRb.linearVelocity.normalized;
            optimalPosition = targetPosition - targetVelocityDir * optimalChaseRange;
        }

        // Set desired direction toward optimal position
        desiredDirection = (optimalPosition - transform.position).normalized;

        // Speed control based on distance
        if (distance > maxChaseRange)
        {
            SetThrust(combatSpeed);
        }
        else if (distance < optimalChaseRange * 0.5f)
        {
            SetThrust(cruiseSpeed * 0.7f); // Slow down when too close
        }
        else
        {
            SetThrust(combatSpeed * 0.85f);
        }
    }

    private void ExecuteOpportunisticBehavior()
    {
        if (currentTarget == null)
        {
            FlyLevel();
            SetThrust(cruiseSpeed);
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;

        // Try to maintain missile range and keep target in wide FOV
        if (distance < optimalMissileRange * 0.7f)
        {
            // Too close - extend distance while keeping in FOV
            Vector3 right = Vector3.Cross(directionToTarget, Vector3.up).normalized;
            desiredDirection = (-directionToTarget + right * 0.3f).normalized;
            SetThrust(combatSpeed);
        }
        else if (distance > optimalMissileRange * 1.3f)
        {
            // Too far - close in
            desiredDirection = directionToTarget;
            SetThrust(combatSpeed);
        }
        else
        {
            // Good range - circle/orbit to keep in missile FOV
            Vector3 right = Vector3.Cross(directionToTarget, Vector3.up).normalized;
            desiredDirection = (directionToTarget * 0.5f + right * 0.5f).normalized;
            SetThrust(cruiseSpeed * 0.8f);
        }
    }

    private void ExecuteEvadingBehavior()
    {
        Vector3 threatDirection = Vector3.zero;
        bool hasThreat = false;

        // Evade missile
        if (nearestMissile != null)
        {
            threatDirection = (nearestMissile.transform.position - transform.position).normalized;
            hasThreat = true;

            float missileDistance = Vector3.Distance(transform.position, nearestMissile.transform.position);

            // Aggressive evasion when missile is close
            if (missileDistance < 300f)
            {
                // Hard perpendicular turn to get missile off angle
                Vector3 perpendicular = Vector3.Cross(threatDirection, Vector3.up).normalized;
                desiredDirection = perpendicular;

                // Use high-G mode
                UseHighGMode(true);
            }
            else
            {
                // Gentler evasion
                Vector3 perpendicular = Vector3.Cross(threatDirection, Vector3.up).normalized;
                desiredDirection = (perpendicular - threatDirection * 0.5f).normalized;
                UseHighGMode(false);
            }

            SetThrust(evasionSpeed);
        }
        // Evade target (low health)
        else if (currentTarget != null)
        {
            threatDirection = (currentTarget.transform.position - transform.position).normalized;
            hasThreat = true;

            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance < evasionDistance)
            {
                // Get away from target
                Vector3 right = Vector3.Cross(threatDirection, Vector3.up).normalized;
                desiredDirection = (-threatDirection + right * 0.5f).normalized;
                SetThrust(evasionSpeed);
            }
            else
            {
                // Far enough - return to normal
                FlyLevel();
                SetThrust(cruiseSpeed);
                UseHighGMode(false);
            }
        }
        else
        {
            // No threat - return to normal
            FlyLevel();
            SetThrust(cruiseSpeed);
            UseHighGMode(false);
        }
    }

    private void CheckGroundAvoidance()
    {
        // CRITICAL: Pull up if too low
        if (currentAltitude < pullUpAltitude)
        {
            float urgency = 1f - (currentAltitude / pullUpAltitude);
            urgency = Mathf.Clamp01(urgency) * pullUpStrength;

            // Override desired direction to pull up
            Vector3 pullUpDirection = Vector3.up;
            desiredDirection = Vector3.Lerp(desiredDirection, pullUpDirection, urgency);

            // If VERY low, full override
            if (currentAltitude < minAltitude)
            {
                desiredDirection = Vector3.up;
                SetThrust(evasionSpeed); // Full throttle to gain altitude
            }
        }
    }

    private void ApplyFlightControls()
    {
        if (aiInput == null) return;

        // Calculate control inputs to reach desired direction
        Vector3 localDesiredDirection = transform.InverseTransformDirection(desiredDirection);

        // Calculate pitch (up/down)
        float targetPitch = -Mathf.Atan2(localDesiredDirection.y, localDesiredDirection.z) * Mathf.Rad2Deg;
        desiredPitch = Mathf.Clamp(targetPitch / 45f, -1f, 1f);

        // Calculate yaw (left/right)
        float targetYaw = Mathf.Atan2(localDesiredDirection.x, localDesiredDirection.z) * Mathf.Rad2Deg;
        desiredYaw = Mathf.Clamp(targetYaw / 45f, -1f, 1f);

        // Calculate roll (bank into turns)
        desiredRoll = -desiredYaw * 0.8f;

        // Apply inputs through AI interface
        aiInput.SetControlInputs(desiredPitch, desiredRoll, desiredYaw);
    }

    private void FlyLevel()
    {
        // Try to maintain level flight
        Vector3 upDir = Vector3.up;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, upDir).normalized;

        if (forward.magnitude > 0.1f)
        {
            desiredDirection = forward;
        }
        else
        {
            desiredDirection = transform.forward;
        }
    }

    private void SetThrust(float targetThrust)
    {
        if (aiInput != null)
        {
            aiInput.SetThrust(targetThrust);
        }
    }

    private void UseHighGMode(bool enable)
    {
        if (aiInput != null)
        {
            aiInput.SetHighGMode(enable);
        }
    }

    private void OnDrawGizmos()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw desired direction
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, desiredDirection * 50f);
        }

        // Draw line to target
        if (currentTarget != null)
        {
            Color behaviorColor = currentBehavior == BehaviorMode.Evading ? Color.red :
                                   currentBehavior == BehaviorMode.Aggressive ? Color.magenta :
                                   currentBehavior == BehaviorMode.Opportunistic ? Color.yellow :
                                   Color.green;
            Gizmos.color = behaviorColor;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Draw line to nearest missile
        if (nearestMissile != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nearestMissile.transform.position);
        }

        // Draw altitude line
        if (Application.isPlaying)
        {
            Gizmos.color = currentAltitude < pullUpAltitude ? Color.red : Color.green;
            Gizmos.DrawRay(transform.position, Vector3.down * currentAltitude);
        }
    }
}