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

    private float nextGunFireTime;
    private float nextMissileFireTime;
    private float nextTargetUpdateTime;
    private int currentFirePointIndex = 0;
    private Vector3 lastTargetPosition;
    private bool hasMissileLock = false;

    public enum WeaponType
    {
        Gun,
        Missile
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
        if (currentTarget == null) return false;

        // Check if on cooldown
        if (Time.time < nextGunFireTime) return false;

        // Check range
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxGunRange) return false;

        // Check if target is alive
        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;

        // Check line of sight and FOV
        if (requireLineOfSight)
        {
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > gunFiringFOV)
                return false;
        }

        return true;
    }

    private bool CanShootMissile()
    {
        if (currentTarget == null) return false;

        // Check if on cooldown
        if (Time.time < nextMissileFireTime) return false;

        // Check range
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (distance > maxMissileRange) return false;

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
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

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
        if (currentTarget == null || projectilePrefab == null) return;

        // Update cooldown
        nextGunFireTime = Time.time + (1f / gunFireRate);

        // Get fire point
        Transform firePoint = firePoints[currentFirePointIndex];
        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;

        // Calculate accuracy-adjusted aim
        Vector3 aimDirection = CalculateAimDirection(firePoint.position);

        // Spawn projectile
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(aimDirection));

        // Initialize projectile
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(gameObject, gunDamage);
        }
    }

    private void ShootMissile()
    {
        if (currentTarget == null || missilePrefab == null) return;

        // Update cooldown
        nextMissileFireTime = Time.time + (1f / missileFireRate);

        // Get fire point
        Transform firePoint = firePoints[currentFirePointIndex];
        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;

        // Calculate aim direction
        Vector3 aimDirection = (currentTarget.transform.position - firePoint.position).normalized;

        // Spawn missile
        GameObject missileObj = Instantiate(missilePrefab, firePoint.position, Quaternion.LookRotation(aimDirection));

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
    }
}