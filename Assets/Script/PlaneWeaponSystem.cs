using UnityEngine;

public class PlaneWeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Projectile prefab to spawn")]
    public GameObject projectilePrefab;
    [Tooltip("Where projectiles spawn from")]
    public Transform[] firePoints;
    [Tooltip("Damage per shot")]
    public float damage = 10f;
    [Tooltip("Projectile speed")]
    public float projectileSpeed = 500f;
    [Tooltip("Fire rate (shots per second)")]
    public float fireRate = 10f;
    [Tooltip("Key to fire weapon")]
    public KeyCode fireKey = KeyCode.Mouse0;

    [Header("Ammo (optional)")]
    [Tooltip("Use ammo system?")]
    public bool useAmmo = false;
    [Tooltip("Current ammo")]
    public int currentAmmo = 300;
    [Tooltip("Maximum ammo")]
    public int maxAmmo = 300;

    [Header("Aim Assist")]
    [Tooltip("Enable aim assist")]
    public bool useAimAssist = true;
    [Tooltip("Max angle for aim assist to activate (degrees)")]
    public float aimAssistFOV = 15f;
    [Tooltip("How strong the aim assist is (0 = none, 1 = full snap)")]
    [Range(0f, 1f)]
    public float aimAssistStrength = 0.5f;
    [Tooltip("Max distance for aim assist")]
    public float aimAssistMaxRange = 800f;
    [Tooltip("Tag for targetable enemies")]
    public string enemyTag = "Enemy";
    [Tooltip("Only assist when targeting (requires TargetingSystem)")]
    public bool onlyAssistLockedTarget = false;

    [Header("Effects")]
    [Tooltip("Muzzle flash effect")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Sound effect when firing")]
    public AudioClip fireSound;

    private float nextFireTime;
    private AudioSource audioSource;
    private int currentFirePointIndex = 0;
    private Rigidbody planeRigidbody;
    private TargetingSystem targetingSystem;
    private GameObject currentAimAssistTarget;

    private void Awake()
    {
        // Get or add audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && fireSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.playOnAwake = false;
        }

        // Get plane's rigidbody for velocity inheritance
        planeRigidbody = GetComponent<Rigidbody>();

        // Get targeting system (optional)
        targetingSystem = GetComponent<TargetingSystem>();

        // If no fire points specified, create one at plane's position
        if (firePoints == null || firePoints.Length == 0)
        {
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.parent = transform;
            firePoint.transform.localPosition = Vector3.forward * 2f;
            firePoint.transform.localRotation = Quaternion.identity;
            firePoints = new Transform[] { firePoint.transform };
        }
    }

    private void Update()
    {
        // Update aim assist target
        if (useAimAssist)
        {
            UpdateAimAssist();
        }

        HandleWeaponInput();
    }

    private void HandleWeaponInput()
    {
        // Check if fire button is held and we can fire
        if (Input.GetKey(fireKey) && CanFire())
        {
            Fire();
        }
    }

    private bool CanFire()
    {
        // Check cooldown
        if (Time.time < nextFireTime)
            return false;

        // Check ammo
        if (useAmmo && currentAmmo <= 0)
            return false;

        // Check projectile prefab exists
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to weapon system!");
            return false;
        }

        return true;
    }

    private void Fire()
    {
        // Update fire cooldown
        nextFireTime = Time.time + (1f / fireRate);

        // Use ammo
        if (useAmmo)
            currentAmmo--;

        // Get fire point (alternate between multiple fire points if available)
        Transform firePoint = firePoints[currentFirePointIndex];
        currentFirePointIndex = (currentFirePointIndex + 1) % firePoints.Length;

        // Calculate aim direction (with aim assist if available)
        Quaternion fireRotation = firePoint.rotation;

        if (useAimAssist && currentAimAssistTarget != null)
        {
            fireRotation = CalculateAimAssistRotation(firePoint);
        }

        // Spawn projectile
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, fireRotation);

        // Initialize projectile
        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(gameObject, damage, projectileSpeed);

            // Inherit plane's velocity
            if (planeRigidbody != null)
            {
                Rigidbody projectileRb = projectileObj.GetComponent<Rigidbody>();
                if (projectileRb != null)
                {
                    projectileRb.linearVelocity = planeRigidbody.linearVelocity + fireRotation * Vector3.forward * projectileSpeed;
                }
            }
        }

        // Spawn muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
            flash.transform.parent = firePoint;
            Destroy(flash, 0.1f);
        }

        // Play sound
        if (audioSource != null && fireSound != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
    }

    /// <summary>
    /// Reload ammo to full
    /// </summary>
    public void Reload()
    {
        currentAmmo = maxAmmo;
    }

    /// <summary>
    /// Add ammo
    /// </summary>
    public void AddAmmo(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    /// <summary>
    /// Get ammo as percentage
    /// </summary>
    public float GetAmmoPercent()
    {
        if (!useAmmo) return 1f;
        return (float)currentAmmo / maxAmmo;
    }

    /// <summary>
    /// Update aim assist - find best target to assist toward
    /// </summary>
    private void UpdateAimAssist()
    {
        currentAimAssistTarget = null;

        // If only assisting locked target, use that
        if (onlyAssistLockedTarget && targetingSystem != null)
        {
            if (targetingSystem.HasTarget())
            {
                GameObject lockedTarget = targetingSystem.currentTarget;
                if (IsValidAimAssistTarget(lockedTarget))
                {
                    currentAimAssistTarget = lockedTarget;
                }
            }
            return;
        }

        // Find all potential targets
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);

        GameObject closestTarget = null;
        float closestAngle = float.MaxValue;

        foreach (GameObject target in potentialTargets)
        {
            if (!IsValidAimAssistTarget(target))
                continue;

            // Check if in FOV
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle <= aimAssistFOV && angle < closestAngle)
            {
                closestAngle = angle;
                closestTarget = target;
            }
        }

        currentAimAssistTarget = closestTarget;
    }

    /// <summary>
    /// Check if target is valid for aim assist
    /// </summary>
    private bool IsValidAimAssistTarget(GameObject target)
    {
        if (target == null) return false;

        // Check distance
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > aimAssistMaxRange)
            return false;

        // Check if alive
        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && !targetHealth.IsAlive())
            return false;

        return true;
    }

    /// <summary>
    /// Calculate rotation with aim assist applied
    /// </summary>
    private Quaternion CalculateAimAssistRotation(Transform firePoint)
    {
        if (currentAimAssistTarget == null)
            return firePoint.rotation;

        // Get target position and velocity
        Vector3 targetPosition = currentAimAssistTarget.transform.position;

        // Lead the target (predict where it will be)
        Rigidbody targetRb = currentAimAssistTarget.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            float distance = Vector3.Distance(firePoint.position, targetPosition);
            float timeToTarget = distance / projectileSpeed;

            // Add target velocity prediction
            targetPosition += targetRb.linearVelocity * timeToTarget;
        }

        // Calculate direction to predicted position
        Vector3 aimDirection = (targetPosition - firePoint.position).normalized;

        // Blend between current aim and assisted aim
        Vector3 currentDirection = firePoint.forward;
        Vector3 assistedDirection = Vector3.Slerp(currentDirection, aimDirection, aimAssistStrength);

        return Quaternion.LookRotation(assistedDirection);
    }

    /// <summary>
    /// Get current aim assist target (for debugging/UI)
    /// </summary>
    public GameObject GetAimAssistTarget()
    {
        return currentAimAssistTarget;
    }
}