using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Improved plane HUD with dynamic lock indicators and off-screen target arrows
/// </summary>
public class PlaneHUD : MonoBehaviour
{
    [Header("References")]
    public Health planeHealth;
    public PlaneWeaponSystem weaponSystem;
    public MissileLauncher missileLauncher;
    public TargetingSystem targetingSystem;
    public SpecialAbility SpecialAbility;

    [Header("Health UI")]
    public TextMeshProUGUI healthText;
    public Image healthBar;

    [Header("Weapon UI")]
    public TextMeshProUGUI ammoText;

    //public Image ammoBar;
    public TextMeshProUGUI missileText;

    //public Image missileBar;

    [Header("Target UI")]
    public TextMeshProUGUI targetInfoText;
    public GameObject targetIndicator;
    public Image targetHealthBar;

    [Header("Misc UI")]
    public TextMeshProUGUI SpecialAbilityText;

    [Header("Lock Indicator (On Target)")]
    [Tooltip("Lock indicator that appears on the target")]
    public GameObject lockIndicator;

    //[Tooltip("Progress ring/circle inside lock indicator")]
    //public Image lockProgressRing;
    [Tooltip("Center icon/image for lock indicator")]
    public Image lockCenterIcon;

    [Tooltip("Color while locking")]
    public Color lockingColor = Color.yellow;

    [Tooltip("Color when locked")]
    public Color lockedColor = Color.green;

    [Header("Off-Screen Target Arrow")]
    [Tooltip("Arrow that points to off-screen targets")]
    public GameObject offScreenArrow;

    [Tooltip("Distance from screen edge to place arrow")]
    public float edgeOffset = 50f;

    [Tooltip("Size of the arrow")]
    public float arrowSize = 40f;

    private RectTransform lockIndicatorRect;
    private RectTransform offScreenArrowRect;
    private Canvas canvas;
    private RectTransform canvasRect;

    private void Start()
    {
        // Get canvas reference
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();
        if (canvas == null)
        {
            Debug.LogError("PlaneHUD must be a child of a Canvas!");
        }

        // Get RectTransforms
        if (lockIndicator != null)
        {
            lockIndicatorRect = lockIndicator.GetComponent<RectTransform>();
        }

        if (offScreenArrow != null)
        {
            offScreenArrowRect = offScreenArrow.GetComponent<RectTransform>();
        }

        // Hide indicators initially
        if (lockIndicator != null)
            lockIndicator.SetActive(false);
        if (offScreenArrow != null)
            offScreenArrow.SetActive(false);
    }

    private void Update()
    {
        UpdateHealthUI();
        UpdateWeaponUI();
        UpdateMissileUI();
        UpdateTargetUI();
        UpdateSpecialAbilityUI();
    }

    private void UpdateHealthUI()
    {
        if (planeHealth == null)
            return;

        if (healthText != null)
        {
            healthText.text =
                $"DMG: {(1f - planeHealth.currentHealth / planeHealth.maxHealth) * 100f:F0}%";
        }

        if (healthBar != null)
        {
            healthBar.fillAmount = planeHealth.GetHealthPercent();

            // Color based on health
            if (planeHealth.GetHealthPercent() > 0.75f)
                healthBar.color = Color.green;
            else if (planeHealth.GetHealthPercent() > 0.3f)
                healthBar.color = Color.yellow;
            else
                healthBar.color = Color.red;
        }
    }

    private void UpdateWeaponUI()
    {
        if (weaponSystem == null)
            return;

        if (ammoText != null)
        {
            if (weaponSystem.useAmmo)
            {
                ammoText.text = $"AMMO: {weaponSystem.currentAmmo}";
            }
            else
            {
                ammoText.text = "AMMO: ∞";
            }
        }

        /*if (ammoBar != null)
        {
            ammoBar.fillAmount = weaponSystem.GetAmmoPercent();

            // Color based on ammo
            if (weaponSystem.GetAmmoPercent() > 0.3f)
                ammoBar.color = Color.cyan;
            else if (weaponSystem.GetAmmoPercent() > 0.1f)
                ammoBar.color = Color.yellow;
            else
                ammoBar.color = Color.red;
        }*/
    }

    private void UpdateMissileUI()
    {
        if (missileLauncher == null)
            return;

        if (missileText != null)
        {
            missileText.text = $"MSL: {missileLauncher.currentMissiles}";
        }

        /*if (missileBar != null)
        {
            float missilePercent = (float)missileLauncher.currentMissiles / missileLauncher.maxMissiles;
            missileBar.fillAmount = missilePercent;

            if (missilePercent > 0.3f)
                missileBar.color = Color.cyan;
            else if (missilePercent > 0)
                missileBar.color = Color.yellow;
            else
                missileBar.color = Color.red;
        }*/
    }

    private void UpdateSpecialAbilityUI()
    {
        if (SpecialAbility == null || SpecialAbilityText == null)
            return;

        if (SpecialAbility.IsReady())
        {
            SpecialAbilityText.text = "[SPL READY]";
            SpecialAbilityText.color = Color.green;
        }
        else
        {
            SpecialAbilityText.text = "[SPL WAIT]";
            SpecialAbilityText.color = Color.red;
        }
    }

    private void UpdateTargetUI()
    {
        if (targetingSystem == null || Camera.main == null)
            return;

        bool hasTarget = targetingSystem.HasTarget();

        // Show/hide basic target indicator
        if (targetIndicator != null)
            targetIndicator.SetActive(hasTarget);

        if (hasTarget)
        {
            GameObject target = targetingSystem.currentTarget;
            Vector3 targetWorldPos = target.transform.position;

            // Convert to screen space
            Vector3 screenPos = Camera.main.WorldToScreenPoint(targetWorldPos);
            bool isOnScreen = IsPointOnScreen(screenPos);

            // Get lock status
            float lockProgress = 0f;
            bool isLocking = false;
            bool isLocked = false;

            if (missileLauncher != null)
            {
                lockProgress = missileLauncher.GetLockProgress();
                isLocked = missileLauncher.IsLocked();
                isLocking = lockProgress > 0f; // Locking if progress > 0
            }

            // Update target info text
            UpdateTargetInfoText(target, isLocked, lockProgress);

            // Update target health bar
            UpdateTargetHealthBar(target);

            // === LOCK INDICATOR (appears on target when locking) ===
            if (lockIndicator != null)
            {
                if (isLocking)
                {
                    lockIndicator.SetActive(true);

                    // Position on target (only if on screen)
                    if (isOnScreen)
                    {
                        PositionUIElement(lockIndicatorRect, screenPos);
                    }
                    else
                    {
                        // Don't show lock indicator off-screen
                        lockIndicator.SetActive(false);
                    }

                    // Update lock progress ring
                    if (lockCenterIcon != null)
                    {
                        lockCenterIcon.fillAmount = lockProgress;
                        lockCenterIcon.color = isLocked ? lockedColor : lockingColor;
                    }

                    // Update center icon color
                    if (lockCenterIcon != null)
                    {
                        lockCenterIcon.color = isLocked ? lockedColor : lockingColor;
                    }
                }
                else
                {
                    // Not locking - hide indicator
                    lockIndicator.SetActive(false);
                }
            }

            // === TARGET INDICATOR (always shows when target exists) ===
            if (targetIndicator != null)
            {
                if (isOnScreen)
                {
                    targetIndicator.SetActive(true);
                    RectTransform indicatorRect = targetIndicator.GetComponent<RectTransform>();
                    if (indicatorRect != null)
                    {
                        PositionUIElement(indicatorRect, screenPos);
                    }
                }
                else
                {
                    targetIndicator.SetActive(false);
                }
            }

            // === OFF-SCREEN ARROW (shows when target is off-screen) ===
            if (offScreenArrow != null && offScreenArrowRect != null)
            {
                if (!isOnScreen && screenPos.z > 0) // Target is in front but off-screen
                {
                    offScreenArrow.SetActive(true);
                    UpdateOffScreenArrow(screenPos, targetWorldPos);
                }
                else if (screenPos.z <= 0) // Target is behind camera
                {
                    offScreenArrow.SetActive(true);
                    UpdateOffScreenArrowBehind(targetWorldPos);
                }
                else
                {
                    // On screen - hide arrow
                    offScreenArrow.SetActive(false);
                }
            }
        }
        else
        {
            // No target - hide everything
            if (targetInfoText != null)
            {
                targetInfoText.text = "NO TARGET\nPress E to lock";
            }

            if (lockIndicator != null)
                lockIndicator.SetActive(false);

            if (offScreenArrow != null)
                offScreenArrow.SetActive(false);
        }
    }

    private void UpdateTargetInfoText(GameObject target, bool isLocked, float lockProgress)
    {
        if (targetInfoText == null)
            return;

        float distance = targetingSystem.GetTargetDistance();
        float angle = targetingSystem.GetTargetAngle();

        string targetName = target.name;
        string lockStatus = "";

        if (isLocked)
        {
            lockStatus = "[LOCKED]";
        }
        else if (lockProgress > 0f)
        {
            lockStatus = $"[LOCKING {lockProgress * 100:F0}%]";
        }

        targetInfoText.text =
            $"TARGET: {targetName}\n{lockStatus}\nDIST: {distance:F0}m\nANGLE: {angle:F1}°";
    }

    private void UpdateTargetHealthBar(GameObject target)
    {
        if (targetHealthBar == null)
            return;

        Health targetHealth = target.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealthBar.fillAmount = targetHealth.GetHealthPercent();

            // Color based on target health
            if (targetHealth.GetHealthPercent() > 0.5f)
                targetHealthBar.color = Color.red;
            else if (targetHealth.GetHealthPercent() > 0.25f)
                targetHealthBar.color = Color.yellow;
            else
                targetHealthBar.color = Color.gray;
        }
        else
        {
            targetHealthBar.fillAmount = 0;
        }
    }

    private void UpdateOffScreenArrow(Vector3 screenPos, Vector3 worldPos)
    {
        // Clamp to screen edges
        Vector2 clampedPos = ClampToScreenEdge(screenPos);

        // Position arrow at edge
        offScreenArrowRect.position = clampedPos;

        // Rotate arrow to point toward target
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 direction = new Vector2(screenPos.x, screenPos.y) - screenCenter;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Rotate arrow (assuming arrow points right by default)
        offScreenArrowRect.rotation = Quaternion.Euler(0, 0, angle);

        // Optional: Color based on distance/lock status
        UpdateArrowColor();
    }

    private void UpdateOffScreenArrowBehind(Vector3 worldPos)
    {
        // Target is behind - point arrow toward bottom of screen
        Vector2 bottomCenter = new Vector2(Screen.width / 2f, edgeOffset);
        offScreenArrowRect.position = bottomCenter;

        // Point downward (target is behind)
        offScreenArrowRect.rotation = Quaternion.Euler(0, 0, -90);

        UpdateArrowColor();
    }

    private void UpdateArrowColor()
    {
        // Color arrow based on lock status
        Image arrowImage = offScreenArrow.GetComponent<Image>();
        if (arrowImage != null && missileLauncher != null)
        {
            if (missileLauncher.IsLocked())
            {
                arrowImage.color = lockedColor;
            }
            else if (missileLauncher.GetLockProgress() > 0f)
            {
                arrowImage.color = lockingColor;
            }
            else
            {
                arrowImage.color = Color.white;
            }
        }
    }

    private Vector2 ClampToScreenEdge(Vector3 screenPos)
    {
        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        Vector2 clampedPos = new Vector2(screenPos.x, screenPos.y);

        // Calculate direction from center
        Vector2 center = new Vector2(screenWidth / 2f, screenHeight / 2f);
        Vector2 direction = clampedPos - center;

        // Normalize direction
        if (direction.magnitude > 0.1f)
        {
            direction.Normalize();

            // Calculate intersection with screen edges
            float xEdge = (screenWidth / 2f) - edgeOffset;
            float yEdge = (screenHeight / 2f) - edgeOffset;

            // Find which edge we hit first
            float xTime =
                Mathf.Abs(direction.x) > 0.01f ? xEdge / Mathf.Abs(direction.x) : float.MaxValue;
            float yTime =
                Mathf.Abs(direction.y) > 0.01f ? yEdge / Mathf.Abs(direction.y) : float.MaxValue;

            float time = Mathf.Min(xTime, yTime);

            // Position at edge
            clampedPos = center + direction * time;
        }

        return clampedPos;
    }

    private bool IsPointOnScreen(Vector3 screenPos)
    {
        // Check if point is on screen and in front of camera
        if (screenPos.z <= 0)
            return false;

        return screenPos.x >= 0
            && screenPos.x <= Screen.width
            && screenPos.y >= 0
            && screenPos.y <= Screen.height;
    }

    private void PositionUIElement(RectTransform rectTransform, Vector3 screenPos)
    {
        if (rectTransform == null || canvasRect == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint
        );

        rectTransform.anchoredPosition = localPoint;
    }
}
