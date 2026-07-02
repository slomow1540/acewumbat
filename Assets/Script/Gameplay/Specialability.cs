using System.Collections;
using UnityEngine;

/// <summary>
/// Special Ability System for player aircraft.
/// Reads the chosen ability from a ValueHolder script on the "GameValues" tagged object.
/// Abilities: "manuver", "Regen", "Airburst", "Boost"
/// Each ability's cooldown, modifiers, activation sound, and visual effect object are configured in the Inspector.
/// </summary>
public class SpecialAbility : MonoBehaviour
{
    // ── Per-ability config blocks ────────────────────────────────────────────

    [System.Serializable]
    public class ManuverConfig
    {
        [Tooltip("Cooldown for this ability (seconds)")]
        public float cooldown = 30f;

        [Tooltip("How long the buff stays fully active (seconds)")]
        public float duration = 6f;

        [Tooltip("How long stats take to gradually revert after duration ends (seconds)")]
        public float revertTime = 3f;

        [Header("Stat Multipliers")]
        [Tooltip(
            "Multiplier applied to rollResponsiveness, pitchResponsiveness, yawResponsiveness"
        )]
        public float controlResponsivenessMultiplier = 2.5f;

        [Tooltip("Multiplier applied to maxAngularVelocity")]
        public float maxAngularVelocityMultiplier = 2f;

        [Tooltip("Multiplier applied to maxGForce")]
        public float maxGForceMultiplier = 1.5f;

        [Header("Audio")]
        [Tooltip("Sound played when Manuver is activated")]
        public AudioClip activateSound;

        [Header("Visual Effect")]
        [Tooltip(
            "GameObject enabled while Manuver is active, disabled when it ends. Starts inactive."
        )]
        public GameObject effectObject;
    }

    [System.Serializable]
    public class BoostConfig
    {
        [Tooltip("Cooldown for this ability (seconds)")]
        public float cooldown = 12f;

        [Tooltip("How long the buff stays fully active (seconds)")]
        public float duration = 3f;

        [Tooltip("How long stats take to gradually revert after duration ends (seconds)")]
        public float revertTime = 1.5f;

        [Tooltip("Multiplier applied to maxThrust")]
        public float maxThrustMultiplier = 3f;

        [Tooltip("Multiplier applied to thrustAcceleration")]
        public float thrustAccelerationMultiplier = 2f;

        [Header("Audio")]
        [Tooltip("Sound played when Boost is activated")]
        public AudioClip activateSound;

        [Header("Visual Effect")]
        [Tooltip(
            "GameObject enabled while Boost is active, disabled when it ends. Starts inactive."
        )]
        public GameObject effectObject;
    }

    [System.Serializable]
    public class RegenConfig
    {
        [Tooltip("Cooldown for this ability (seconds)")]
        public float cooldown = 60f;

        [Tooltip("Total HP recovered over the regen duration")]
        public float totalHP = 50f;

        [Tooltip("Duration over which regen ticks (seconds)")]
        public float duration = 10f;

        [Header("Audio")]
        [Tooltip("Sound played when Regen is activated")]
        public AudioClip activateSound;

        [Header("Visual Effect")]
        [Tooltip(
            "GameObject enabled while Regen is active, disabled when it ends. Starts inactive."
        )]
        public GameObject effectObject;
    }

    [System.Serializable]
    public class AirburstConfig
    {
        [Tooltip("Cooldown for this ability (seconds)")]
        public float cooldown = 40f;

        [Tooltip("Missile prefab to launch")]
        public GameObject missilePrefab;

        [Tooltip("Damage the missile deals")]
        public float missileDamage = 80f;

        [Tooltip("Optional explicit target; falls back to TargetingSystem.currentTarget if null")]
        public GameObject overrideTarget;

        [Header("Audio")]
        [Tooltip("Sound played when Airburst is activated")]
        public AudioClip activateSound;
    }

    [Header("Ability Info (Set by ValueHolder on Init)")]
    [Tooltip("The chosen ability name, pulled from ValueHolder on the GameValues object")]
    public string chosenAbility = "";

    [Header("Current Cooldown State")]
    [Tooltip("Remaining cooldown time in seconds (public for external UI/systems)")]
    public float currentCooldown = 0f;

    [Header("Fire Points (for Airburst)")]
    [Tooltip("Transform(s) from which the Airburst missile is launched")]
    public Transform[] firePoints;

    [Header("Ability Configs")]
    public ManuverConfig manuverConfig = new ManuverConfig();
    public BoostConfig boostConfig = new BoostConfig();
    public RegenConfig regenConfig = new RegenConfig();
    public AirburstConfig airburstConfig = new AirburstConfig();

    [Header("Audio")]
    [Tooltip(
        "Sound played when ability finishes cooling down (ready again) — shared across all abilities"
    )]
    public AudioClip readySound;

    [Range(0f, 1f)]
    public float activateVolume = 0.8f;

    [Range(0f, 1f)]
    public float readyVolume = 0.5f;

    [Header("Input")]
    [Tooltip("Key to activate the special ability")]
    public KeyCode abilityKey = KeyCode.Q;

    // ── Private ────────────────────────────────────────────────────────────────

    private float maxCooldown = 0f;
    private bool isOnCooldown = false;
    private bool abilityActive = false; // True during manuver/boost/regen active window

    // Component references
    private Rigidbody rb;
    private ImprovedPlaneController planeController;
    private Health health;
    private TargetingSystem targetingSystem;
    private AudioSource audioSource;

    // Manuver — original stat cache
    private float orig_rollResponsiveness;
    private float orig_pitchResponsiveness;
    private float orig_yawResponsiveness;
    private float orig_maxAngularVelocity;
    private float orig_maxGForce;

    // Boost — original stat cache
    private float orig_thrustAcceleration;
    private float orig_maxThrust;

    // ── Cooldown lookup ────────────────────────────────────────────────────────

    private float GetCooldownForAbility(string ability)
    {
        switch (ability.ToLower())
        {
            case "manuver":
                return manuverConfig.cooldown;
            case "regen":
                return regenConfig.cooldown;
            case "airburst":
                return airburstConfig.cooldown;
            case "boost":
                return boostConfig.cooldown;
            default:
                Debug.LogWarning(
                    $"[SpecialAbility] Unknown ability '{ability}', defaulting cooldown to 30s."
                );
                return 30f;
        }
    }

    // ── Activation sound lookup ──────────────────────────────────────────────

    private AudioClip GetActivateSoundForAbility(string ability)
    {
        switch (ability.ToLower())
        {
            case "manuver":
                return manuverConfig.activateSound;
            case "regen":
                return regenConfig.activateSound;
            case "airburst":
                return airburstConfig.activateSound;
            case "boost":
                return boostConfig.activateSound;
            default:
                return null;
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

        // Ensure all ability effect objects start deactivated
        if (manuverConfig.effectObject != null)
            manuverConfig.effectObject.SetActive(false);
        if (boostConfig.effectObject != null)
            boostConfig.effectObject.SetActive(false);
        if (regenConfig.effectObject != null)
            regenConfig.effectObject.SetActive(false);
    }

    private void Start()
    {
        // ── Pull ability name from GameValues ──────────────────────────────────
        GameObject gameValuesObj = GameObject.FindGameObjectWithTag("GameValues");
        // if (gameValuesObj != null)
        // {
        //     ValueHolder valueHolder = gameValuesObj.GetComponent<ValueHolder>();
        //     if (valueHolder != null)
        //     {
        //         chosenAbility = valueHolder.SpecialWeaponName;
        //         Debug.Log($"[SpecialAbility] Ability loaded from ValueHolder: '{chosenAbility}'");
        //     }
        //     else
        //     {
        //         Debug.LogWarning(
        //             "[SpecialAbility] GameValues object has no ValueHolder component!"
        //         );
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("[SpecialAbility] No object with tag 'GameValues' found in scene!");
        // }

        maxCooldown = GetCooldownForAbility(chosenAbility);
        currentCooldown = 0f;
    }

    private void Update()
    {
        // Tick cooldown
        if (isOnCooldown)
        {
            currentCooldown -= Time.deltaTime;

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

        AudioClip clip = GetActivateSoundForAbility(chosenAbility);
        if (clip != null)
            audioSource.PlayOneShot(clip, activateVolume);

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
    }

    private void OnCooldownComplete()
    {
        if (readySound != null)
            audioSource.PlayOneShot(readySound, readyVolume);

        Debug.Log($"[SpecialAbility] '{chosenAbility}' is ready.");
    }

    // ── MANUVER (turn rate + G-force stats) ─────────────────────────────────────

    private IEnumerator ManuverRoutine()
    {
        if (planeController == null)
        {
            Debug.LogWarning("[SpecialAbility] Manuver requires ImprovedPlaneController!");
            yield break;
        }

        abilityActive = true;

        if (manuverConfig.effectObject != null)
            manuverConfig.effectObject.SetActive(true);

        // Cache originals
        orig_rollResponsiveness = planeController.rollResponsiveness;
        orig_pitchResponsiveness = planeController.pitchResponsiveness;
        orig_yawResponsiveness = planeController.yawResponsiveness;
        orig_maxAngularVelocity = planeController.maxAngularVelocity;
        orig_maxGForce = planeController.maxGForce;

        float controlMult = manuverConfig.controlResponsivenessMultiplier;
        float angVelMult = manuverConfig.maxAngularVelocityMultiplier;
        float gForceMult = manuverConfig.maxGForceMultiplier;

        // Apply buff instantly
        planeController.rollResponsiveness = orig_rollResponsiveness * controlMult;
        planeController.pitchResponsiveness = orig_pitchResponsiveness * controlMult;
        planeController.yawResponsiveness = orig_yawResponsiveness * controlMult;
        planeController.maxAngularVelocity = orig_maxAngularVelocity * angVelMult;
        planeController.maxGForce = orig_maxGForce * gForceMult;

        Debug.Log("[SpecialAbility] Manuver: stats boosted.");

        // Hold buff for configured duration
        yield return new WaitForSeconds(manuverConfig.duration);

        // Gradually revert stats over configured revertTime
        float elapsed = 0f;
        float revertTime = Mathf.Max(0.0001f, manuverConfig.revertTime); // avoid div by zero
        while (elapsed < revertTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / revertTime; // 0 → 1

            planeController.rollResponsiveness = Mathf.Lerp(
                orig_rollResponsiveness * controlMult,
                orig_rollResponsiveness,
                t
            );
            planeController.pitchResponsiveness = Mathf.Lerp(
                orig_pitchResponsiveness * controlMult,
                orig_pitchResponsiveness,
                t
            );
            planeController.yawResponsiveness = Mathf.Lerp(
                orig_yawResponsiveness * controlMult,
                orig_yawResponsiveness,
                t
            );
            planeController.maxAngularVelocity = Mathf.Lerp(
                orig_maxAngularVelocity * angVelMult,
                orig_maxAngularVelocity,
                t
            );
            planeController.maxGForce = Mathf.Lerp(orig_maxGForce * gForceMult, orig_maxGForce, t);

            yield return null;
        }

        // Snap to exact originals at the end (avoids floating point drift)
        planeController.rollResponsiveness = orig_rollResponsiveness;
        planeController.pitchResponsiveness = orig_pitchResponsiveness;
        planeController.yawResponsiveness = orig_yawResponsiveness;
        planeController.maxAngularVelocity = orig_maxAngularVelocity;
        planeController.maxGForce = orig_maxGForce;

        if (manuverConfig.effectObject != null)
            manuverConfig.effectObject.SetActive(false);

        Debug.Log("[SpecialAbility] Manuver: stats fully reverted.");
        abilityActive = false;
    }

    // ── BOOST (maxThrust + thrustAcceleration buff, i.e. afterburner) ───────────

    private IEnumerator BoostRoutine()
    {
        if (planeController == null)
        {
            Debug.LogWarning("[SpecialAbility] Boost requires ImprovedPlaneController!");
            yield break;
        }

        abilityActive = true;

        if (boostConfig.effectObject != null)
            boostConfig.effectObject.SetActive(true);

        orig_thrustAcceleration = planeController.thrustAcceleration;
        orig_maxThrust = planeController.maxThrust;
        float thrustAccelMult = boostConfig.thrustAccelerationMultiplier;
        float maxThrustMult = boostConfig.maxThrustMultiplier;

        planeController.maxThrust = orig_maxThrust * maxThrustMult;
        planeController.thrustAcceleration = orig_thrustAcceleration * thrustAccelMult;
        Debug.Log("[SpecialAbility] Boost: maxThrust boosted.");

        // Hold buff for configured duration
        yield return new WaitForSeconds(boostConfig.duration);

        // Gradually revert over configured revertTime
        float elapsed = 0f;
        float revertTime = Mathf.Max(0.0001f, boostConfig.revertTime);
        while (elapsed < revertTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / revertTime;

            planeController.thrustAcceleration = Mathf.Lerp(
                orig_thrustAcceleration * thrustAccelMult,
                orig_thrustAcceleration,
                t
            );
            planeController.maxThrust = Mathf.Lerp(
                orig_maxThrust * maxThrustMult,
                orig_maxThrust,
                t
            );

            yield return null;
        }

        planeController.thrustAcceleration = orig_thrustAcceleration;
        planeController.maxThrust = orig_maxThrust;

        if (boostConfig.effectObject != null)
            boostConfig.effectObject.SetActive(false);

        Debug.Log("[SpecialAbility] Boost: maxThrust fully reverted.");
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

        if (regenConfig.effectObject != null)
            regenConfig.effectObject.SetActive(true);

        Debug.Log(
            $"[SpecialAbility] Regen: restoring {regenConfig.totalHP} HP over {regenConfig.duration}s."
        );

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, regenConfig.duration);
        float healPerSecond = regenConfig.totalHP / duration;

        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            health.Heal(healPerSecond * dt);
            elapsed += dt;
            yield return null;
        }

        if (regenConfig.effectObject != null)
            regenConfig.effectObject.SetActive(false);

        Debug.Log("[SpecialAbility] Regen: complete.");
        abilityActive = false;
    }

    // ── AIRBURST ──────────────────────────────────────────────────────────────

    private void ActivateAirburst()
    {
        if (airburstConfig.missilePrefab == null)
        {
            Debug.LogWarning("[SpecialAbility] Airburst: no missile prefab assigned!");
            return;
        }

        // Determine target — prefer explicit override, fall back to TargetingSystem
        GameObject target = airburstConfig.overrideTarget;
        if (target == null && targetingSystem != null && targetingSystem.HasTarget())
        {
            target = targetingSystem.currentTarget;
        }

        // Choose fire point (first one; expand to cycle if you add more later)
        Transform fp = firePoints[0];

        GameObject missileObj = Instantiate(airburstConfig.missilePrefab, fp.position, fp.rotation);

        Missile missile = missileObj.GetComponent<Missile>();
        if (missile != null)
        {
            missile.Initialize(gameObject, target, airburstConfig.missileDamage);
        }
        else
        {
            Debug.LogWarning("[SpecialAbility] Airburst missile prefab has no Missile component.");
        }

        Debug.Log(
            $"[SpecialAbility] Airburst fired! Target: {(target != null ? target.name : "none")}"
        );
    }

    // ── Public helpers ─────────────────────────────────────────────────────────

    /// <summary>Returns true if the ability is ready to use.</summary>
    public bool IsReady() => !isOnCooldown && !abilityActive;

    /// <summary>Returns cooldown progress as 0 (ready) → 1 (just used).</summary>
    public float GetCooldownNormalized() =>
        maxCooldown > 0f ? Mathf.Clamp01(currentCooldown / maxCooldown) : 0f;

    /// <summary>Returns true while a timed ability (manuver/boost/regen) is still running.</summary>
    public bool IsAbilityActive() => abilityActive;

    /// <summary>Force-reset the cooldown (e.g. from a pickup or cheat).</summary>
    public void ResetCooldown()
    {
        currentCooldown = 0f;
        isOnCooldown = false;
    }
}
