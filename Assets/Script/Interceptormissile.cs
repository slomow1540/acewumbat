using UnityEngine;

/// <summary>
/// Simple interceptor missile - flies toward target missile and destroys it
/// Much simpler than the full Missile.cs - purpose-built for defense
/// </summary>
public class InterceptorMissile : MonoBehaviour
{
    [Header("Basic Stats")]
    [Tooltip("Target missile to intercept")]
    public GameObject targetMissile;

    [Tooltip("Who fired this interceptor")]
    public GameObject owner;

    [Tooltip("Flight speed")]
    public float speed = 200f;

    [Tooltip("Turn rate (degrees per second)")]
    public float turnRate = 360f;

    [Tooltip("Lifetime before self-destruct")]
    public float lifetime = 10f;

    [Tooltip("Proximity detonation range")]
    public float detonationRange = 15f;

    [Tooltip("Explosion radius (kills missiles instantly)")]
    public float explosionRadius = 20f;

    [Header("Effects")]
    [Tooltip("Explosion effect")]
    public GameObject explosionPrefab;

    [Tooltip("Trail renderer")]
    public TrailRenderer trail;

    [Tooltip("Engine particles")]
    public ParticleSystem engineParticles;

    [Header("Audio")]
    [Tooltip("Launch sound")]
    public AudioClip launchSound;

    [Tooltip("Explosion sound")]
    public AudioClip explosionSound;

    private Rigidbody rb;
    private bool hasExploded = false;
    private AudioSource audioSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f;
    }

    private void Start()
    {
        // Set initial velocity
        rb.linearVelocity = transform.forward * speed;

        // Start engine effects
        if (engineParticles != null)
        {
            engineParticles.Play();
        }

        // Play launch sound
        if (launchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(launchSound);
        }

        // Auto-destruct after lifetime
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (hasExploded) return;

        // Check if target still exists
        if (targetMissile == null)
        {
            // Target destroyed or lost - fly straight
            rb.linearVelocity = transform.forward * speed;
            return;
        }

        // Calculate direction to target
        Vector3 directionToTarget = (targetMissile.transform.position - transform.position).normalized;

        // Rotate toward target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnRate * Time.fixedDeltaTime
        );

        // Move forward
        rb.linearVelocity = transform.forward * speed;

        // Check if close enough to detonate
        float distanceToTarget = Vector3.Distance(transform.position, targetMissile.transform.position);
        if (distanceToTarget <= detonationRange)
        {
            Explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        // Don't hit owner
        if (other.gameObject == owner)
            return;

        // Explode on any contact
        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Spawn explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Find and destroy all missiles in blast radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hitColliders)
        {
            // Don't hit owner
            if (hit.gameObject == owner)
                continue;

            // Check if it's a missile (using either Missile or InterceptorMissile component)
            Missile missile = hit.GetComponent<Missile>();
            InterceptorMissile interceptor = hit.GetComponent<InterceptorMissile>();

            if (missile != null && missile.gameObject != gameObject)
            {
                // Destroy enemy missile instantly
                Debug.Log($"Interceptor destroyed missile: {hit.gameObject.name}");
                Destroy(hit.gameObject);
            }
            else if (interceptor != null && interceptor != this)
            {
                // Can also destroy other interceptors (chain reaction)
                Debug.Log($"Interceptor destroyed another interceptor: {hit.gameObject.name}");
                Destroy(hit.gameObject);
            }
        }

        // Destroy self
        Destroy(gameObject);
    }

    /// <summary>
    /// Initialize interceptor with target and owner
    /// </summary>
    public void Initialize(GameObject shooter, GameObject target)
    {
        owner = shooter;
        targetMissile = target;

        if (targetMissile != null)
        {
            Debug.Log($"Interceptor initialized: targeting {targetMissile.name}");
        }
    }

    private void OnDrawGizmos()
    {
        // Draw detonation range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detonationRange);

        // Draw explosion radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Draw line to target
        if (targetMissile != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetMissile.transform.position);
        }

        // Draw velocity
        if (rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, rb.linearVelocity.normalized * 5f);
        }
    }
}