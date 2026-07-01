using UnityEngine;

/// <summary>
/// Enemy AI that can fight back using guns or missiles
/// </summary>
public class EnemyAI : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Tag to identify targets (e.g., 'Player')")]
    public string targetTag = "Player";

    [Tooltip("Current target")]
    public GameObject currentTarget;

    [Tooltip("Detection range")]
    public float detectionRange = 1000f;

    [Tooltip("How often to check for targets (seconds)")]
    public float targetUpdateRate = 1f;

    [Header("Weapon Settings")]
    [Tooltip("Use guns")]
    public bool useGuns = true;

    [Tooltip("Use missiles")]
    public bool useMissiles = true;

    [Tooltip("Projectile prefab")]
    public GameObject projectilePrefab;

    [Tooltip("Missile prefab")]
    public GameObject missilePrefab;

    [Tooltip("Fire points")]
    public Transform[] firePoints;

    [Tooltip("Gun damage per shot")]
    public float gunDamage = 10f;

    [Tooltip("Missile damage")]
    public float missileDamage = 50f;

    [Tooltip("Gun fire rate (shots per second)")]
    public float gunFireRate = 5f;

    [Tooltip("Missile fire rate (shots per second)")]
    public float missileFireRate = 0.5f;

    [Tooltip("Maximum gun range")]
    public float maxGunRange = 800f;

    [Tooltip("Maximum missile range")]
    public float maxMissileRange = 1500f;

    [Tooltip("Gun firing FOV (degrees)")]
    public float gunFiringFOV = 30f;

    [Tooltip("Missile lock FOV (degrees)")]
    public float missileLockFOV = 80f;

    [Header("Accuracy Settings")]
    [Tooltip("Accuracy at point-blank range (0-1, 1 = perfect)")]
    [Range(0f, 1f)]
    public float closeRangeAccuracy = 0.9f;

    [Tooltip("Accuracy at max range (0-1)")]
    [Range(0f, 1f)]
    public float longRangeAccuracy = 0.3f;

    [Tooltip("Distance considered 'close range' for accuracy")]
    public float closeRange = 200f;

    [Tooltip("Lead target movement (0 = no lead, 1 = perfect lead)")]
    [Range(0f, 1f)]
    public float leadAccuracy = 0.8f;

    [Header("Missile Settings")]
    [Tooltip("Lock-on time for missiles (seconds)")]
    public float missileLockTime = 2f;

    [Tooltip("Current missile lock progress")]
    [Range(0f, 1f)]
    public float currentLockProgress = 0f;

    [Header("Behavior")]
    [Tooltip("Only shoot when target is in front")]
    public bool requireLineOfSight = true;

    // ─────────────────────────────────────────────────────────────────────────
    [Header("Turret Mode")]
    [Tooltip(
        "Enable turret mode – the AI will physically rotate azimuth / elevation objects to face the target instead of relying only on FOV checks."
    )]
    public bool isTurret = false;

    [System.Serializable]
    public class TurretAxis
    {
        [Tooltip(
            "The Transform to rotate for this axis (e.g. the horizontal base or vertical barrel mount)."
        )]
        public Transform target;

        [Tooltip("Which local axis to ROTATE AROUND (the hinge / pivot axis).")]
        public RotationAxis rotationAxis = RotationAxis.Y;

        [Tooltip(
            "Which local axis points DOWN THE BARREL / in the firing direction. "
                + "Check Scene view arrows on the object: X = red, Y = green, Z = blue."
        )]
        public RotationAxis forwardAxis = RotationAxis.Z;

        [Tooltip(
            "Flip the forward axis 180°. Use this when the barrel faces the opposite direction "
                + "to the arrow you selected above."
        )]
        public bool invertForward = false;

        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 90f;

        [Tooltip("Invert the rotation direction.")]
        public bool inverted = false;
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z,
    }

    [Tooltip("Azimuth (horizontal / yaw) axis settings.")]
    public TurretAxis azimuth = new TurretAxis();

    [Tooltip("Elevation (vertical / pitch) axis settings.")]
    public TurretAxis elevation = new TurretAxis();

    // ─────────────────────────────────────────────────────────────────────────

    private float nextGunFireTime;
    private float nextMissileFireTime;
    private float nextTargetUpdateTime;
    private int currentFirePointIndex = 0;
    private Vector3 lastTargetPosition;
    private bool hasMissileLock = false;

    public enum WeaponType
    {
        Gun,
        Missile,
    }

    private void Start()
    {
        // Setup fire points if not assigned
        if (firePoints == null || firePoints.Length == 0)
        {
            GameObject firePoint = new GameObject("EnemyFirePoint");
            firePoint.transform.parent = transform;
            firePoint.transform.localPosition = Vector3.forward;
            firePoints = new Transform[] { firePoint.transform };
        }

        if (currentTarget == null)
        {
            FindTarget();
        }

        lastTargetPosition = transform.position;
    }

    private void Update()
    {
        // Update target periodically
        if (Time.time >= nextTargetUpdateTime)
        {
            FindTarget();
            nextTargetUpdateTime = Time.time + targetUpdateRate;
        }

        // Drive turret rotation every frame
        if (isTurret && currentTarget != null)
        {
            RotateTurretAxis(azimuth, currentTarget.transform.position);
            RotateTurretAxis(elevation, currentTarget.transform.position);
        }

        // Try to shoot at target
        if (currentTarget != null)
        {
            // Update missile lock if using missiles
            if (useMissiles)
            {
                UpdateMissileLock();
            }

            // Try to fire guns
            if (useGuns && CanShootGun())
            {
                ShootGun();
            }

            // Try to fire missiles
            if (useMissiles && CanShootMissile())
            {
                ShootMissile();
            }
        }
        else
        {
            // Reset missile lock if no target
            currentLockProgress = 0f;
            hasMissileLock = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Turret helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rotates a single turret axis (azimuth or elevation) toward the world-space target position.
    /// Works for any combination of rotation axis and barrel forward axis.
    /// </summary>
    private void RotateTurretAxis(TurretAxis axis, Vector3 worldTargetPos)
    {
        if (axis == null || axis.target == null)
            return;

        Transform pivot = axis.target;

        // Direction to target in the pivot's PARENT local space.
        // For elevation (child of azimuth) this is already in azimuth-rotated space.
        Vector3 localDir =
            pivot.parent != null
                ? pivot.parent.InverseTransformDirection(worldTargetPos - pivot.position)
                : worldTargetPos - pivot.position;

        if (localDir.sqrMagnitude < 0.0001f)
            return;

        Vector3 rotAxisVec = AxisToVector(axis.rotationAxis);
        Vector3 fwdAxisVec = AxisToVector(axis.forwardAxis);
        if (axis.invertForward)
            fwdAxisVec = -fwdAxisVec;

        // Project the target direction onto the plane the pivot can actually rotate in.
        // This isolates the component the pivot is responsible for.
        Vector3 targetOnPlane = Vector3.ProjectOnPlane(localDir, rotAxisVec);

        if (targetOnPlane.sqrMagnitude < 0.0001f)
            return; // target is directly along hinge — no meaningful angle

        // Signed angle from the barrel forward to the projected target, around the hinge.
        float desiredAngle = Vector3.SignedAngle(fwdAxisVec, targetOnPlane, rotAxisVec);

        if (axis.inverted)
            desiredAngle = -desiredAngle;

        // Build a clean single-axis rotation and step toward it
        Quaternion targetRotation = Quaternion.AngleAxis(desiredAngle, rotAxisVec);

        pivot.localRotation = Quaternion.RotateTowards(
            pivot.localRotation,
            targetRotation,
            axis.rotationSpeed * Time.deltaTime
        );
    }

    private static Vector3 AxisToVector(RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

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

            // Within detection range?
            if (distance <= detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = target;
            }
        }

        currentTarget = closestTarget;
    }

    private bool CanShootGun()
    {
        if (currentTarget == null)
            return false;

        // Check if on cooldown
        if (Time.time < nextGunFireTime)
            return false;

        // Check range
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxGunRange)
            return false;

        // Check if target is alive
        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;

        // Check line of sight and FOV
        if (requireLineOfSight)
        {
            Vector3 directionToTarget = (
                currentTarget.transform.position - transform.position
            ).normalized;

            // In turret mode use the firepoint's forward (barrel is what's actually aimed).
            // In non-turret mode use the root transform forward.
            Vector3 aimForward =
                (isTurret && firePoints != null && firePoints.Length > 0)
                    ? firePoints[currentFirePointIndex % firePoints.Length].forward
                    : transform.forward;

            float angle = Vector3.Angle(aimForward, directionToTarget);

            if (angle > gunFiringFOV)
                return false;
        }

        return true;
    }

    private bool CanShootMissile()
    {
        if (currentTarget == null)
            return false;

        // Check if on cooldown
        if (Time.time < nextMissileFireTime)
            return false;

        // Check range
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxMissileRange)
            return false;

        // Check if target is alive
        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;

        // Missiles require lock
        if (!hasMissileLock)
            return false;

        return true;
    }

    private void UpdateMissileLock()
    {
        if (currentTarget == null)
        {
            currentLockProgress = 0f;
            hasMissileLock = false;
            return;
        }

        // Check if target is in lock parameters
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxMissileRange)
        {
            currentLockProgress = 0f;
            hasMissileLock = false;
            return;
        }

        if (requireLineOfSight)
        {
            Vector3 directionToTarget = (
                currentTarget.transform.position - transform.position
            ).normalized;

            Vector3 aimForward =
                (isTurret && firePoints != null && firePoints.Length > 0)
                    ? firePoints[currentFirePointIndex % firePoints.Length].forward
                    : transform.forward;

            float angle = Vector3.Angle(aimForward, directionToTarget);

            if (angle > missileLockFOV)
            {
                currentLockProgress = 0f;
                hasMissileLock = false;
                return;
            }
        }

        // Increase lock progress
        if (currentLockProgress < 1f)
        {
            currentLockProgress += Time.deltaTime / missileLockTime;
            currentLockProgress = Mathf.Clamp01(currentLockProgress);

            if (currentLockProgress >= 1f)
            {
                hasMissileLock = true;
            }
        }
    }

    private void ShootGun()
    {
        if (currentTarget == null || projectilePrefab == null)
            return;

        // Update cooldown
        nextGunFireTime = Time.time + (1f / gunFireRate);

        // Get fire point
        Transform firePoint = firePoints[currentFirePointIndex];
        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;

        // In turret mode the barrel is already physically aimed — fire straight out.
        // In non-turret mode use the lead + accuracy calculation.
        Vector3 aimDirection = isTurret
            ? ApplySpread(firePoint.forward, firePoint.position)
            : CalculateAimDirection(firePoint.position);

        // Spawn projectile
        GameObject projectileObj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.LookRotation(aimDirection)
        );

        // Initialize projectile
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(gameObject, gunDamage);
        }
    }

    private void ShootMissile()
    {
        if (currentTarget == null || missilePrefab == null)
            return;

        // Update cooldown
        nextMissileFireTime = Time.time + (1f / missileFireRate);

        // Get fire point
        Transform firePoint = firePoints[currentFirePointIndex];
        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;

        // Calculate aim direction
        Vector3 aimDirection = (currentTarget.transform.position - firePoint.position).normalized;

        // Spawn missile
        GameObject missileObj = Instantiate(
            missilePrefab,
            firePoint.position,
            Quaternion.LookRotation(aimDirection)
        );

        // Initialize missile
        Missile missile = missileObj.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Initialize(gameObject, currentTarget, missileDamage);
        }

        // Reset lock after firing
        currentLockProgress = 0f;
        hasMissileLock = false;
    }

    /// <summary>
    /// Applies accuracy-based random spread to an already-correct direction (turret mode).
    /// </summary>
    private Vector3 ApplySpread(Vector3 baseDirection, Vector3 firePosition)
    {
        float distance =
            currentTarget != null
                ? Vector3.Distance(firePosition, currentTarget.transform.position)
                : 0f;

        float accuracyValue = CalculateAccuracy(distance);
        float maxSpread = (1f - accuracyValue) * 30f;

        float spreadX = Random.Range(-maxSpread, maxSpread);
        float spreadY = Random.Range(-maxSpread, maxSpread);

        return (Quaternion.Euler(spreadY, spreadX, 0f) * baseDirection).normalized;
    }

    private Vector3 CalculateAimDirection(Vector3 firePosition)
    {
        // Base direction to target
        Vector3 targetPosition = currentTarget.transform.position;

        // Lead the target based on velocity
        Vector3 targetVelocity = (targetPosition - lastTargetPosition) / Time.deltaTime;
        lastTargetPosition = targetPosition;

        // Calculate lead
        float distance = Vector3.Distance(firePosition, targetPosition);
        Projectile projectileScript = projectilePrefab?.GetComponent<Projectile>();
        float projectileSpeed = projectileScript != null ? projectileScript.speed : 500f;

        float timeToTarget = distance / projectileSpeed;
        Vector3 leadOffset = targetVelocity * timeToTarget * leadAccuracy;
        Vector3 aimPoint = targetPosition + leadOffset;

        // Apply accuracy spread based on distance
        Vector3 baseDirection = (aimPoint - firePosition).normalized;

        // Calculate accuracy based on distance
        float accuracyValue = CalculateAccuracy(distance);

        // Add random spread based on accuracy
        // Lower accuracy = more spread
        float maxSpread = (1f - accuracyValue) * 30f; // Up to 30 degrees spread

        float spreadX = Random.Range(-maxSpread, maxSpread);
        float spreadY = Random.Range(-maxSpread, maxSpread);

        Quaternion spread = Quaternion.Euler(spreadY, spreadX, 0f);
        Vector3 finalDirection = spread * baseDirection;

        return finalDirection.normalized;
    }

    private float CalculateAccuracy(float distance)
    {
        // Interpolate accuracy based on distance (for guns)
        if (distance <= closeRange)
        {
            // Close range - use close range accuracy
            return closeRangeAccuracy;
        }
        else if (distance >= maxGunRange)
        {
            // Max range - use long range accuracy
            return longRangeAccuracy;
        }
        else
        {
            // Between close and max - interpolate
            float t = (distance - closeRange) / (maxGunRange - closeRange);
            return Mathf.Lerp(closeRangeAccuracy, longRangeAccuracy, t);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw max gun range
        if (useGuns)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxGunRange);
        }

        // Draw max missile range
        if (useMissiles)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, maxMissileRange);
        }

        // Draw line to target
        if (currentTarget != null)
        {
            Gizmos.color = hasMissileLock ? Color.green : Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Draw gun FOV
        if (requireLineOfSight && useGuns)
        {
            Gizmos.color = Color.red;
            Vector3 forward = transform.forward * maxGunRange;
            Vector3 right = Quaternion.Euler(0, gunFiringFOV, 0) * forward;
            Vector3 left = Quaternion.Euler(0, -gunFiringFOV, 0) * forward;

            Gizmos.DrawRay(transform.position, right);
            Gizmos.DrawRay(transform.position, left);
        }

        // Draw missile FOV
        if (requireLineOfSight && useMissiles)
        {
            Gizmos.color = Color.magenta;
            Vector3 forward = transform.forward * maxMissileRange;
            Vector3 right = Quaternion.Euler(0, missileLockFOV, 0) * forward;
            Vector3 left = Quaternion.Euler(0, -missileLockFOV, 0) * forward;

            Gizmos.DrawRay(transform.position, right);
            Gizmos.DrawRay(transform.position, left);
        }

        // Draw turret axis pivots
        if (isTurret)
        {
            DrawTurretAxisGizmo(azimuth, Color.blue, "AZ");
            DrawTurretAxisGizmo(elevation, Color.green, "EL");
        }
    }

    private void DrawTurretAxisGizmo(TurretAxis axis, Color color, string label)
    {
        if (axis == null || axis.target == null)
            return;

        Gizmos.color = color;

        // Draw a small cross at the pivot
        float size = 5f;
        Gizmos.DrawLine(
            axis.target.position - axis.target.right * size,
            axis.target.position + axis.target.right * size
        );
        Gizmos.DrawLine(
            axis.target.position - axis.target.up * size,
            axis.target.position + axis.target.up * size
        );
        Gizmos.DrawLine(
            axis.target.position - axis.target.forward * size,
            axis.target.position + axis.target.forward * size
        );

        // Draw forward arrow to show where the pivot is currently pointing
        Gizmos.DrawRay(axis.target.position, axis.target.forward * size * 2f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(axis.target.position + Vector3.up * (size + 1f), label);
#endif
    }
}
