using UnityEngine;

/// <summary>
/// IFF (Identification Friend or Foe) marker for friendly aircraft
/// Shows a sprite icon when within range of the main camera
/// </summary>
public class IFFMarker : MonoBehaviour
{
    [Header("IFF Settings")]
    [Tooltip("Maximum distance to show the IFF marker")]
    public float maxRange = 500f;

    [Tooltip("Sprite to display as IFF marker")]
    public SpriteRenderer iffSprite;

    [Header("Optional Settings")]
    [Tooltip("Scale of the marker (1 = normal size)")]
    public float markerScale = 1f;

    [Tooltip("Always face the camera")]
    public bool billboardToCamera = true;

    [Tooltip("Color tint for the marker")]
    public Color markerColor = Color.green;

    [Tooltip("Fade marker based on distance")]
    public bool fadeWithDistance = true;

    [Tooltip("Minimum alpha when at max range (if fading enabled)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    private Camera mainCamera;

    private void Start()
    {
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("IFFMarker: No main camera found! Make sure a camera is tagged 'MainCamera'.");
        }

        // Setup sprite if assigned
        if (iffSprite != null)
        {
            iffSprite.transform.localScale = Vector3.one * markerScale;
            iffSprite.color = markerColor;
        }
        else
        {
            Debug.LogError("IFFMarker: No sprite assigned!");
        }
    }

    private void Update()
    {
        // Re-acquire camera if it becomes null (e.g. scene/camera changes at runtime)
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || iffSprite == null)
            return;

        // Calculate distance to camera instead of a tagged player object
        float distance = Vector3.Distance(transform.position, mainCamera.transform.position);

        // Show/hide based on range
        if (distance <= maxRange)
        {
            iffSprite.enabled = true;

            // Fade with distance if enabled
            if (fadeWithDistance)
            {
                float alpha = Mathf.Lerp(1f, minAlpha, distance / maxRange);
                Color currentColor = iffSprite.color;
                currentColor.a = alpha;
                iffSprite.color = currentColor;
            }
        }
        else
        {
            iffSprite.enabled = false;
        }

        // Billboard to camera if enabled
        if (billboardToCamera && iffSprite.enabled)
        {
            iffSprite.transform.rotation = mainCamera.transform.rotation;
        }
    }
}