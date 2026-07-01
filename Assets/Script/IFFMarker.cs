using UnityEngine;

/// <summary>
/// IFF (Identification Friend or Foe) marker for friendly aircraft
/// Shows a sprite icon when within range of the player
/// </summary>
public class IFFMarker : MonoBehaviour
{
    [Header("IFF Settings")]
    [Tooltip("Maximum distance to show the IFF marker")]
    public float maxRange = 500f;
    
    [Tooltip("Sprite to display as IFF marker")]
    public SpriteRenderer iffSprite;
    
    [Tooltip("Tag of the player plane")]
    public string playerTag = "Player";
    
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
    
    private Transform playerTransform;
    private Camera mainCamera;

    private void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"IFFMarker: No object with tag '{playerTag}' found!");
        }
        
        // Get main camera
        mainCamera = Camera.main;
        
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
        if (playerTransform == null || iffSprite == null) return;
        
        // Calculate distance to player
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        
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
        if (billboardToCamera && mainCamera != null && iffSprite.enabled)
        {
            iffSprite.transform.rotation = mainCamera.transform.rotation;
        }
    }
}
