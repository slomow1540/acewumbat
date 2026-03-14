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

        // Try to damage the hit object
        Health targetHealth = collision.gameObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(damage, owner);
        }
        
        // Spawn impact effect
        if (impactEffectPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rotation = Quaternion.LookRotation(contact.normal);
            Instantiate(impactEffectPrefab, contact.point, rotation);
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
