using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Special Ability System for player aircraft.
/// Reads the chosen ability from a ValueHolder script on the "GameValues" tagged object.
/// Abilities: "manuver", "Regen", "Airburst", "Boost"
/// </summary>
public class SpecialAbility : MonoBehaviour
{
    [Header("Ability Info (Set by ValueHolder on Init)")]
    [Tooltip("The chosen ability name, pulled from ValueHolder on the GameValues object")]
    public string chosenAbility = "";

    [Header("Current Cooldown State")]
    [Tooltip("Remaining cooldown time in seconds (public for external UI/systems)")]
    public float currentCooldown = 0f;

    [Header("Fire Points (for Airburst)")]
    [Tooltip("Transform(s) from which the Airburst missile is launched")]
    public Transform[] firePoints;

    [Header("Airburst Settings")]
    [Tooltip("Missile prefab to launch for Airburst ability")]
    public GameObject airburstMissilePrefab;

    [Tooltip("Damage the Airburst missile deals")]
    public float airburstMissileDamage = 80f;

    [Tooltip("Target to fire the Airburst missile at (uses TargetingSystem if null)")]
    public GameObject airburstTarget;

    [Header("Boost Settings")]
    [Tooltip("Force applied forward during Boost")]
    public float boostForce = 160000f;

    [Tooltip("Duration of the boost force application (seconds)")]
    public float boostDuration = 0.3f;

    [Header("Manuver Settings")]
    [Tooltip("Duration of the manuver buff (seconds)")]
    public float manuverDuration = 6f;

    [Tooltip("How long the stats take to gradually revert after manuver ends (seconds)")]
    public float manuverRevertTime = 3f;

    [Header("Regen Settings")]
    [Tooltip("Total HP recovered over the regen duration")]
    public float regenTotalHP = 50f;

    [Tooltip("Duration over which regen ticks (seconds)")]
    public float regenDuration = 10f;

    [Header("Audio")]
    [Tooltip("Sound played when ability activates")]
    public AudioClip activateSound;

    [Tooltip("Sound played when ability finishes cooling down (ready again)")]
    public AudioClip readySound;

    [Range(0f, 1f)]
    public float activateVolume = 0.8f;

    [Range(0f, 1f)]
    public float readyVolume = 0.5f;

    [Header("Events")]
    public UnityEvent onAbilityActivated;
    public UnityEvent onAbilityCooldownComplete;
    public UnityEvent<float> onCooldownChanged; // Passes normalized 0-1 progress

    [Header("Input")]
    [Tooltip("Key to activate the special ability")]
    public KeyCode abilityKey = KeyCode.C;

    // ── Private ────────────────────────────────────────────────────────────────

    private float maxCooldown = 0f;
    private bool isOnCooldown = false;
    private bool abilityActive = false; // True during manuver/regen active window

    // Component references
    private Rigidbody rb;
    private ImprovedPlaneController planeController;
    private Health health;
    private TargetingSystem targetingSystem;
    private AudioSource audioSource;

    // Manuver — original stat cache
    private float orig_thrustAcceleration;
    private float orig_maxThrust;
    private float orig_rollResponsiveness;
    private float orig_pitchResponsiveness;
    private float orig_yawResponsiveness;
    private float orig_maxAngularVelocity;
    private float orig_maxGForce;

    // ── Cooldown lookup ────────────────────────────────────────────────────────

    private float GetCooldownForAbility(string ability)
    {
        switch (ability.ToLower())
        {
            case "manuver":
                return 30f;
            case "regen":
                return 60f;
            case "airburst":
                return 40f;
            case "boost":
                return 12f;
            default:
                Debug.LogWarning(
                    $"[SpecialAbility] Unknown ability '{ability}', defaulting cooldown to 30s."
                );
                return 30f;
        }
    }

    // ── Unity lifecycle ────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        planeController = GetComponent<ImprovedPlaneController>();
        health = GetComponent<Health>();
        targetingSystem = GetComponent<TargetingSystem>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // Default fire point if none assigned
        if (firePoints == null || firePoints.Length == 0)
        {
            GameObject fp = new GameObject("AbilityFirePoint");
            fp.transform.parent = transform;
            fp.transform.localPosition = Vector3.forward * 3f;
            fp.transform.localRotation = Quaternion.identity;
            firePoints = new Transform[] { fp.transform };
        }
    }

    private void Start()
    {
        // ── Pull ability name from GameValues ──────────────────────────────────
        GameObject gameValuesObj = GameObject.FindGameObjectWithTag("GameValues");
        if (gameValuesObj != null)
        {
            ValueHolder valueHolder = gameValuesObj.GetComponent<ValueHolder>();
            if (valueHolder != null)
            {
                chosenAbility = valueHolder.SpecialWeaponName;
                Debug.Log($"[SpecialAbility] Ability loaded from ValueHolder: '{chosenAbility}'");
            }
            else
            {
                Debug.LogWarning(
                    "[SpecialAbility] GameValues object has no ValueHolder component!"
                );
            }
        }
        else
        {
            Debug.LogWarning("[SpecialAbility] No object with tag 'GameValues' found in scene!");
        }

        maxCooldown = GetCooldownForAbility(chosenAbility);
        currentCooldown = 0f;
    }

    private void Update()
    {
        // Tick cooldown
        if (isOnCooldown)
        {
            currentCooldown -= Time.deltaTime;
            onCooldownChanged?.Invoke(Mathf.Clamp01(currentCooldown / maxCooldown));

            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
                isOnCooldown = false;
                OnCooldownComplete();
            }
        }

        // Input
        if (Input.GetKeyDown(abilityKey))
        {
            TryActivateAbility();
        }
    }

    // ── Ability gate ───────────────────────────────────────────────────────────

    private void TryActivateAbility()
    {
        if (isOnCooldown)
        {
            Debug.Log(
                $"[SpecialAbility] '{chosenAbility}' still on cooldown: {currentCooldown:F1}s remaining."
            );
            return;
        }

        if (abilityActive)
        {
            Debug.Log($"[SpecialAbility] '{chosenAbility}' is already active.");
            return;
        }

        ActivateAbility();
    }

    private void ActivateAbility()
    {
        Debug.Log($"[SpecialAbility] Activating '{chosenAbility}'");

        if (activateSound != null)
            audioSource.PlayOneShot(activateSound, activateVolume);

        onAbilityActivated?.Invoke();

        switch (chosenAbility.ToLower())
        {
            case "manuver":
                StartCoroutine(ManuverRoutine());
                break;
            case "regen":
                StartCoroutine(RegenRoutine());
                break;
            case "airburst":
                ActivateAirburst();
                break;
            case "boost":
                StartCoroutine(BoostRoutine());
                break;
            default:
                Debug.LogWarning(
                    $"[SpecialAbility] No implementation for ability '{chosenAbility}'."
                );
                break;
        }

        StartCooldown();
    }

    private void StartCooldown()
    {
        maxCooldown = GetCooldownForAbility(chosenAbility);
        currentCooldown = maxCooldown;
        isOnCooldown = true;
        onCooldownChanged?.Invoke(1f);
    }

    private void OnCooldownComplete()
    {
        if (readySound != null)
            audioSource.PlayOneShot(readySound, readyVolume);

        onCooldownChanged?.Invoke(0f);
        onAbilityCooldownComplete?.Invoke();
        Debug.Log($"[SpecialAbility] '{chosenAbility}' is ready.");
    }

    // ── MANUVER ───────────────────────────────────────────────────────────────

    private IEnumerator ManuverRoutine()
    {
        if (planeController == null)
        {
            Debug.LogWarning("[SpecialAbility] Manuver requires ImprovedPlaneController!");
            yield break;
        }

        abilityActive = true;

        // Cache originals
        orig_thrustAcceleration = planeController.thrustAcceleration;
        orig_maxThrust = planeController.maxThrust;
        orig_rollResponsiveness = planeController.rollResponsiveness;
        orig_pitchResponsiveness = planeController.pitchResponsiveness;
        orig_yawResponsiveness = planeController.yawResponsiveness;
        orig_maxAngularVelocity = planeController.maxAngularVelocity;
        orig_maxGForce = planeController.maxGForce;

        // Apply buff instantly
        planeController.thrustAcceleration = orig_thrustAcceleration * 2f;
        planeController.maxThrust = orig_maxThrust * 3f;
        planeController.rollResponsiveness = orig_rollResponsiveness * 2.5f;
        planeController.pitchResponsiveness = orig_pitchResponsiveness * 2.5f;
        planeController.yawResponsiveness = orig_yawResponsiveness * 2.5f;
        planeController.maxAngularVelocity = orig_maxAngularVelocity * 2f;
        planeController.maxGForce = orig_maxGForce * 1.5f;

        Debug.Log("[SpecialAbility] Manuver: stats boosted.");

        // Hold buff for manuverDuration
        yield return new WaitForSeconds(manuverDuration);

        // Gradually revert stats over manuverRevertTime
        float elapsed = 0f;
        while (elapsed < manuverRevertTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / manuverRevertTime; // 0 → 1

            planeController.thrustAcceleration = Mathf.Lerp(
                orig_thrustAcceleration * 2f,
                orig_thrustAcceleration,
                t
            );
            planeController.maxThrust = Mathf.Lerp(orig_maxThrust * 3f, orig_maxThrust, t);
            planeController.rollResponsiveness = Mathf.Lerp(
                orig_rollResponsiveness * 2.5f,
                orig_rollResponsiveness,
                t
            );
            planeController.pitchResponsiveness = Mathf.Lerp(
                orig_pitchResponsiveness * 2.5f,
                orig_pitchResponsiveness,
                t
            );
            planeController.yawResponsiveness = Mathf.Lerp(
                orig_yawResponsiveness * 2.5f,
                orig_yawResponsiveness,
                t
            );
            planeController.maxAngularVelocity = Mathf.Lerp(
                orig_maxAngularVelocity * 2f,
                orig_maxAngularVelocity,
                t
            );
            planeController.maxGForce = Mathf.Lerp(orig_maxGForce * 1.5f, orig_maxGForce, t);

            yield return null;
        }

        // Snap to exact originals at the end (avoids floating point drift)
        planeController.thrustAcceleration = orig_thrustAcceleration;
        planeController.maxThrust = orig_maxThrust;
        planeController.rollResponsiveness = orig_rollResponsiveness;
        planeController.pitchResponsiveness = orig_pitchResponsiveness;
        planeController.yawResponsiveness = orig_yawResponsiveness;
        planeController.maxAngularVelocity = orig_maxAngularVelocity;
        planeController.maxGForce = orig_maxGForce;

        Debug.Log("[SpecialAbility] Manuver: stats fully reverted.");
        abilityActive = false;
    }

    // ── REGEN ─────────────────────────────────────────────────────────────────

    private IEnumerator RegenRoutine()
    {
        if (health == null)
        {
            Debug.LogWarning("[SpecialAbility] Regen requires a Health component!");
            yield break;
        }

        abilityActive = true;
        Debug.Log($"[SpecialAbility] Regen: restoring {regenTotalHP} HP over {regenDuration}s.");

        float elapsed = 0f;
        float healPerSecond = regenTotalHP / regenDuration;

        while (elapsed < regenDuration)
        {
            float dt = Time.deltaTime;
            health.Heal(healPerSecond * dt);
            elapsed += dt;
            yield return null;
        }

        Debug.Log("[SpecialAbility] Regen: complete.");
        abilityActive = false;
    }

    // ── AIRBURST ──────────────────────────────────────────────────────────────

    private void ActivateAirburst()
    {
        if (airburstMissilePrefab == null)
        {
            Debug.LogWarning("[SpecialAbility] Airburst: no missile prefab assigned!");
            return;
        }

        // Determine target — prefer explicit target, fall back to TargetingSystem
        GameObject target = airburstTarget;
        if (target == null && targetingSystem != null && targetingSystem.HasTarget())
        {
            target = targetingSystem.currentTarget;
        }

        // Choose fire point (first one; expand to cycle if you add more later)
        Transform fp = firePoints[0];

        GameObject missileObj = Instantiate(airburstMissilePrefab, fp.position, fp.rotation);

        Missile missile = missileObj.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Initialize(gameObject, target, airburstMissileDamage);
        }
        else
        {
            Debug.LogWarning("[SpecialAbility] Airburst missile prefab has no Missile component.");
        }

        Debug.Log(
            $"[SpecialAbility] Airburst fired! Target: {(target != null ? target.name : "none")}"
        );
    }

    // ── BOOST ─────────────────────────────────────────────────────────────────

    private IEnumerator BoostRoutine()
    {
        if (rb == null)
        {
            Debug.LogWarning("[SpecialAbility] Boost requires a Rigidbody!");
            yield break;
        }

        abilityActive = true;
        Debug.Log("[SpecialAbility] Boost: applying forward impulse.");

        float elapsed = 0f;
        while (elapsed < boostDuration)
        {
            float dt = Time.deltaTime;
            // Apply force every FixedUpdate equivalent; using ForceMode.Force so mass is respected
            rb.AddForce(transform.forward * boostForce * dt, ForceMode.Impulse);
            elapsed += dt;
            yield return null;
        }

        Debug.Log("[SpecialAbility] Boost: complete.");
        abilityActive = false;
    }

    // ── Public helpers ─────────────────────────────────────────────────────────

    /// <summary>Returns true if the ability is ready to use.</summary>
    public bool IsReady() => !isOnCooldown && !abilityActive;

    /// <summary>Returns cooldown progress as 0 (ready) → 1 (just used).</summary>
    public float GetCooldownNormalized() =>
        maxCooldown > 0f ? Mathf.Clamp01(currentCooldown / maxCooldown) : 0f;

    /// <summary>Returns true while a timed ability (manuver/regen/boost) is still running.</summary>
    public bool IsAbilityActive() => abilityActive;

    /// <summary>Force-reset the cooldown (e.g. from a pickup or cheat).</summary>
    public void ResetCooldown()
    {
        currentCooldown = 0f;
        isOnCooldown = false;
        onCooldownChanged?.Invoke(0f);
    }
}
