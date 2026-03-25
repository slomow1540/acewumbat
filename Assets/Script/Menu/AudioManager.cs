using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource audioSource;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    public bool randomPitch = false;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Queue Settings")]
    public int maxQueueSize = 3;

    private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
    private bool isPlaying = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;

        clip.LoadAudioData();

        if (audioQueue.Count >= maxQueueSize)
            audioQueue.Dequeue();

        audioQueue.Enqueue(clip);

        if (!isPlaying)
            StartCoroutine(PlayQueue());
    }

    IEnumerator PlayQueue()
    {
        isPlaying = true;

        while (audioQueue.Count > 0)
        {
            AudioClip clip = audioQueue.Dequeue();

            if (randomPitch)
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            else
                audioSource.pitch = 1f;

            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();

            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

        isPlaying = false;
    }

    public void StopAllAudio()
    {
        StopAllCoroutines();
        audioQueue.Clear();
        audioSource.Stop();
        isPlaying = false;
    }
}