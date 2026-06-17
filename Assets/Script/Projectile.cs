using UnityEngine;
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("Damage dealt on impact")]
    public float damage = 10f;
    [Tooltip("Projectile speed")]
    public float speed = 500f;
    [Tooltip("Lifetime in seconds before auto-destroy")]
    public float lifetime = 5f;
    [Tooltip("Who shot this projectile?")]
    public GameObject owner;

    [Header("Physics")]
    [Tooltip("Should this projectile be affected by gravity?")]
    public bool useGravity = false;
    [Tooltip("Gravity scale (only if useGravity is true)")]
    public float gravityScale = 1f;

    [Header("Effects")]
    [Tooltip("Impact effect prefab")]
    public GameObject impactEffectPrefab;
    [Tooltip("Trail effect (optional)")]
    public TrailRenderer trailRenderer;

    [Header("Explosive Settings")]
    [Tooltip("If true, projectile deals splash damage in a radius instead of only direct-hit damage")]
    public bool isExplosive = false;
    [Tooltip("Explosion radius for splash damage (only used if isExplosive is true)")]
    public float explosionRadius = 5f;
    [Tooltip("Minimum damage percentage for direct hits (0-1, only used if isExplosive is true)")]
    [Range(0f, 1f)]
    public float minDirectHitDamage = 0.5f;
    [Tooltip("Explosion visual effect prefab (only used if isExplosive is true; falls back to impactEffectPrefab if left empty)")]
    public GameObject explosionPrefab;

    private Rigidbody rb;
    private bool hasHit = false;
    private string ownertag;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = useGravity;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        // Set initial velocity
        rb.linearVelocity = transform.forward * speed;

        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // Apply custom gravity if needed
        if (useGravity)
        {
            rb.AddForce(Physics.gravity * gravityScale * rb.mass);
        }

        // Keep projectile facing its velocity direction
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Don't hit the owner or ally
        if (collision.gameObject == owner)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            hasHit = false;
            return;
        }
        if (collision.gameObject.tag == ownertag)
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<Collider>());
            hasHit = false;
            return;
        }

        // Get impact point for effects
        Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        Vector3 impactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;

        if (isExplosive)
        {
            Explode(impactPoint);
        }
        else
        {
            // Try to damage the hit object directly
            Health targetHealth = collision.gameObject.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, owner);
            }

            // Spawn impact effect
            if (impactEffectPrefab != null)
            {
                Quaternion rotation = Quaternion.LookRotation(impactNormal);
                Instantiate(impactEffectPrefab, impactPoint, rotation);
            }

            // Destroy projectile
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Applies splash damage to everything in explosionRadius around the impact point.
    /// Damage falls off linearly from full at the center to zero at the edge of the radius.
    /// </summary>
    private void Explode(Vector3 explosionPoint)
    {
        // Spawn explosion effect (falls back to impactEffectPrefab if explosionPrefab isn't set)
        GameObject effectToSpawn = explosionPrefab != null ? explosionPrefab : impactEffectPrefab;
        if (effectToSpawn != null)
        {
            GameObject explosion = Instantiate(effectToSpawn, explosionPoint, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, explosionRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            // Don't damage owner or allies
            if (hitCollider.gameObject == owner)
                continue;
            if (hitCollider.gameObject.tag == ownertag)
                continue;

            Health targetHealth = hitCollider.GetComponent<Health>();
            if (targetHealth != null)
            {
                // Damage falloff based on distance to the closest point on the collider
                Vector3 closestPoint = hitCollider.ClosestPoint(explosionPoint);
                float distance = Vector3.Distance(explosionPoint, closestPoint);

                float damageFalloff = 1f - (distance / explosionRadius);
                damageFalloff = Mathf.Clamp01(damageFalloff);

                // Guarantee minimum damage for a direct hit
                if (distance < 1f)
                {
                    damageFalloff = Mathf.Max(damageFalloff, minDirectHitDamage);
                }

                float actualDamage = damage * damageFalloff;

                if (actualDamage > 0)
                {
                    targetHealth.TakeDamage(actualDamage, owner);
                }
            }
        }

        // Destroy projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// Initialize projectile with custom settings
    /// </summary>
    public void Initialize(GameObject shooter, float customDamage = -1, float customSpeed = -1)
    {
        owner = shooter;
        ownertag = owner.tag;
        if (customDamage > 0)
            damage = customDamage;

        if (customSpeed > 0)
            speed = customSpeed;
    }
}