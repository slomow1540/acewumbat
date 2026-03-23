using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private AudioSource audioSource;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool randomPitch = true;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        if (randomPitch)
        {
            audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        }
        else
        {
            audioSource.pitch = 1f;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}
