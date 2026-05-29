using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum AudioChannel
    {
        SFX,
        UI,
        Music,
        Narrator,
        Ambience,
    }

    [System.Serializable]
    public class Channel
    {
        public AudioSource source;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Header("Pitch")]
        public bool randomPitch = false;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Header("Queue")]
        public bool useQueue = true;
        public int maxQueueSize = 3;

        [HideInInspector]
        public Queue<AudioClip> queue = new Queue<AudioClip>();

        [HideInInspector]
        public bool isPlaying = false;
    }

    [Header("Channels")]
    public Channel sfx;
    public Channel ui;
    public Channel music;
    public Channel narrator;
    public Channel ambience;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Play(AudioClip clip, AudioChannel type = AudioChannel.SFX)
    {
        if (clip == null)
            return;

        Channel channel = GetChannel(type);

        if (!channel.useQueue)
        {
            PlayImmediate(channel, clip);
            return;
        }

        if (channel.queue.Count >= channel.maxQueueSize)
        {
            channel.queue.Dequeue();
        }

        channel.queue.Enqueue(clip);

        if (!channel.isPlaying)
        {
            StartCoroutine(PlayQueue(channel));
        }
    }

    IEnumerator PlayQueue(Channel channel)
    {
        channel.isPlaying = true;

        while (channel.queue.Count > 0)
        {
            AudioClip clip = channel.queue.Dequeue();

            PlayImmediate(channel, clip);

            while (channel.source.isPlaying)
            {
                yield return null;
            }
        }

        channel.isPlaying = false;
    }

    void PlayImmediate(Channel channel, AudioClip clip)
    {
        if (channel.randomPitch)
        {
            channel.source.pitch = Random.Range(channel.pitchRange.x, channel.pitchRange.y);
        }
        else
        {
            channel.source.pitch = 1f;
        }

        channel.source.volume = channel.volume;

        channel.source.PlayOneShot(clip);
    }

    Channel GetChannel(AudioChannel type)
    {
        switch (type)
        {
            case AudioChannel.UI:
                return ui;

            case AudioChannel.Music:
                return music;

            case AudioChannel.Narrator:
                return narrator;

            case AudioChannel.Ambience:
                return ambience;

            default:
                return sfx;
        }
    }

    public void Stop(AudioChannel type)
    {
        Channel channel = GetChannel(type);

        channel.queue.Clear();

        channel.source.Stop();

        channel.isPlaying = false;
    }

    public void StopAllAudio()
    {
        Stop(AudioChannel.SFX);
        Stop(AudioChannel.UI);
        Stop(AudioChannel.Music);
        Stop(AudioChannel.Narrator);
        Stop(AudioChannel.Ambience);
    }

    public void SetVolume(AudioChannel type, float volume)
    {
        Channel channel = GetChannel(type);

        channel.volume = Mathf.Clamp01(volume);

        channel.source.volume = channel.volume;
    }
}
