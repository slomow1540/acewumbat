using UnityEngine;

/// <summary>
/// Missile with APN (Augmented Proportional Navigation) guidance
/// </summary>
public class Missile : MonoBehaviour
{
    [Header("Missile Stats")]
    [Tooltip("Damage on impact")]
    public float damage = 50f;
    [Tooltip("Initial launch speed")]
    public float launchSpeed = 100f;
    [Tooltip("Maximum speed")]
    public float maxSpeed = 300f;
    [Tooltip("Acceleration")]
    public float acceleration = 50f;
    [Tooltip("Lifetime before self-destruct")]
    public float lifetime = 15f;
    [Tooltip("Explosion radius for splash damage")]
    public float explosionRadius = 10f;
    [Tooltip("Minimum damage percentage for direct hits (0-1)")]
    [Range(0f, 1f)]
    public float minDirectHitDamage = 0.5f;

    [Header("APN Guidance Settings")]
    [Tooltip("Navigation gain (higher = more aggressive turning, 3-5 is typical)")]
    public float navigationGain = 4f;
    [Tooltip("Turn rate limit (degrees per second)")]
    public float maxTurnRate = 180f;
    [Tooltip("How much to lead the target (accounts for target velocity)")]
    public float leadMultiplier = 1f;
    [Tooltip("Distance at which missile starts terminal guidance (more aggressive)")]
    public float terminalGuidanceRange = 100f;
    [Tooltip("Terminal navigation gain multiplier")]
    public float terminalGainMultiplier = 1.5f;

    [Header("Tracking Loss Settings")]
    [Tooltip("Maximum angle off-target before losing lock (degrees)")]
    public float maxTrackingAngle = 90f;
    [Tooltip("Maximum distance before losing lock")]
    public float maxTrackingRange = 2000f;
    [Tooltip("Time without seeing target before losing lock (seconds)")]
    public float lockLossTime = 3f;
    [Tooltip("Minimum speed to maintain tracking")]
    public float minTrackingSpeed = 50f;

    [Header("Target & Owner")]
    [Tooltip("Locked target")]
    public GameObject target;
    [Tooltip("Who fired this missile")]
    public GameObject owner;
    [Tooltip("Does missile currently have valid tracking lock?")]
    public bool hasTarget = false;

    [Header("Effects")]
    [Tooltip("Explosion effect prefab")]
    public GameObject explosionPrefab;
    [Tooltip("Trail effect")]
    public TrailRenderer trailRenderer;
    [Tooltip("Engine particles")]
    public ParticleSystem engineParticles;
    [Tooltip("Missile sound (engine/flight)")]
    public AudioSource flightAudio;

    private Rigidbody rb;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;
    private float currentSpeed;
    private bool hasHit = false;
    private float timeSinceLastSeen = 0f;
    private string ownertag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        currentSpeed = launchSpeed;
    }

    private void Start()
    {
        // Set initial velocity
        rb.linearVelocity = transform.forward * launchSpeed;

        // Store target info if available
        if (target != null)
        {
            hasTarget = true;
            lastTargetPosition = target.transform.position;
            Debug.Log($"Missile initialized with target: {target.name}");
        }
        else
        {
            Debug.LogWarning("Missile launched without target!");
        }

        // Start engine particles
        if (engineParticles != null)
        {
            engineParticles.Play();
        }

        // Auto-destruct after lifetime
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // Check if target is still valid and trackable
        if (hasTarget)
        {
            if (target == null || !IsTargetAlive())
            {
                LoseTracking("Target destroyed");
            }
            else if (!CanTrackTarget())
            {
                timeSinceLastSeen += Time.fixedDeltaTime;

                if (timeSinceLastSeen >= lockLossTime)
                {
                    LoseTracking("Lost sight of target");
                }
            }
            else
            {
                // Can see target - reset timer
                timeSinceLastSeen = 0f;
            }
        }

        if (hasTarget)
        {
            // APN Guidance
            ApplyAPNGuidance();
        }
        else
        {
            // No target - fly straight and accelerate
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);
            rb.linearVelocity = transform.forward * currentSpeed;
        }

        // Keep missile facing velocity direction
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }

    private void ApplyAPNGuidance()
    {
        if (target == null) return;

        Vector3 targetPosition = target.transform.position;
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Estimate target velocity
        Vector3 currentTargetVelocity = (targetPosition - lastTargetPosition) / Time.fixedDeltaTime;
        lastTargetPosition = targetPosition;

        // Calculate time to intercept
        float timeToIntercept = distanceToTarget / Mathf.Max(currentSpeed, 1f);

        // Lead the target
        Vector3 predictedPosition = targetPosition + currentTargetVelocity * timeToIntercept * leadMultiplier;

        // Direction to predicted position
        Vector3 directionToTarget = (predictedPosition - transform.position).normalized;

        // Calculate desired velocity direction
        Vector3 currentDirection = rb.linearVelocity.normalized;

        // Calculate how much we need to turn (proportional navigation)
        Vector3 desiredDirection = directionToTarget;

        // Apply navigation gain for more aggressive turning
        float currentNavGain = navigationGain;
        if (distanceToTarget < terminalGuidanceRange)
        {
            currentNavGain *= terminalGainMultiplier;
        }

        // Smoothly rotate towards desired direction with turn rate limit
        float maxTurnRadians = maxTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newDirection = Vector3.RotateTowards(currentDirection, desiredDirection, maxTurnRadians * currentNavGain, 0f);

        // Accelerate
        currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);

        // Apply velocity
        rb.linearVelocity = newDirection * currentSpeed;

        // Update rotation to face direction
        if (newDirection.magnitude > 0.1f)
        {
            transform.forward = newDirection;
        }
    }

    private bool IsTargetAlive()
    {
        if (target == null) return false;

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null)
        {
            return targetHealth.IsAlive();
        }

        return true; // Assume alive if no health component
    }

    private bool CanTrackTarget()
    {
        if (target == null) return false;

        // Check distance
        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > maxTrackingRange)
        {
            return false;
        }

        // Check angle (is target behind us?)
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToTarget);

        if (angle > maxTrackingAngle)
        {
            return false;
        }

        // Check if missile is too slow
        if (currentSpeed < minTrackingSpeed)
        {
            return false;
        }

        return true;
    }

    private void LoseTracking(string reason)
    {
        if (hasTarget)
        {
            Debug.Log($"Missile lost tracking: {reason}");
            hasTarget = false;
            target = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        // Don't hit the owner
        if (collision.gameObject == owner)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            return;
        }

        hasHit = true;

        // Get explosion point
        Vector3 explosionPoint = collision.contacts.Length > 0 ?
            collision.contacts[0].point : transform.position;

        // Explode
        Explode(explosionPoint);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        Debug.Log($"Missile collide with target: {other.gameObject.tag} and {owner.tag}");

        // Don't hit the owner or ally
        if (other.gameObject == owner)
        {
            return;
        }

        if (other.gameObject.tag == ownertag)
        {
            return;
        }

        hasHit = true;

        // Explode at missile position
        Explode(transform.position);
    }

    private void Explode(Vector3 explosionPoint)
    {
        Debug.Log($"Missile exploding at {explosionPoint} with damage={damage}, radius={explosionRadius}");

        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, explosionPoint, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        // Apply splash damage to nearby objects
        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, explosionRadius);
        Debug.Log($"Found {hitColliders.Length} objects in explosion radius");

        foreach (Collider hitCollider in hitColliders)
        {
            // Don't damage owner
            if (hitCollider.gameObject == owner)
                continue;
            if (hitCollider.gameObject.tag == ownertag)
                continue;

            Health targetHealth = hitCollider.GetComponent<Health>();
            if (targetHealth != null)
            {
                // Calculate damage falloff based on distance to CLOSEST POINT on collider
                // This ensures even large objects take proper damage
                Vector3 closestPoint = hitCollider.ClosestPoint(explosionPoint);
                float distance = Vector3.Distance(explosionPoint, closestPoint);

                // Damage falloff: full damage at center, 0 at edge of radius
                float damageFalloff = 1f - (distance / explosionRadius);
                damageFalloff = Mathf.Clamp01(damageFalloff);

                // Ensure minimum damage for anything caught in blast
                // If distance is very small (direct hit), guarantee at least minDirectHitDamage
                if (distance < 1f)
                {
                    damageFalloff = Mathf.Max(damageFalloff, minDirectHitDamage);
                }

                float actualDamage = damage * damageFalloff;

                Debug.Log($"Target: {hitCollider.gameObject.name} | Distance: {distance:F2}m | Falloff: {damageFalloff:F2} | Damage: {actualDamage:F1}");

                if (actualDamage > 0)
                {
                    targetHealth.TakeDamage(actualDamage, owner);
                }
            }
        }

        // Destroy missile
        Destroy(gameObject);
    }

    /// <summary>
    /// Initialize missile with target and owner
    /// </summary>
    public void Initialize(GameObject shooter, GameObject lockedTarget, float customDamage = -1)
    {
        owner = shooter;
        target = lockedTarget;
        ownertag = owner.tag;

        if (target != null)
        {
            hasTarget = true;
            lastTargetPosition = target.transform.position;
        }

        if (customDamage > 0)
            damage = customDamage;
    }

    private void OnDrawGizmos()
    {
        // Visualize explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Draw line to target if we have one
        if (target != null && hasTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.transform.position);

            // Show predicted position
            Vector3 targetVel = (target.transform.position - lastTargetPosition) / Time.fixedDeltaTime;
            float timeToTarget = Vector3.Distance(transform.position, target.transform.position) / currentSpeed;
            Vector3 predicted = target.transform.position + targetVel * timeToTarget * leadMultiplier;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(predicted, 2f);
            Gizmos.DrawLine(transform.position, predicted);
        }

        // Show velocity
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, rb != null ? rb.linearVelocity.normalized * 5f : transform.forward * 5f);
    }
}