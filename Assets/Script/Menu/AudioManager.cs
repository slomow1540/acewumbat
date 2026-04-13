using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum AudioChannel
    {
        SFX,
        Music,
        Narrator
    }

    [System.Serializable]
    public class Channel
    {
        public AudioSource source;

        [Range(0f, 1f)]
        public float volume = 1f;

        public bool randomPitch = false;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        [Header("Queue")]
        public int maxQueueSize = 3;

        [HideInInspector]
        public Queue<AudioClip> queue = new Queue<AudioClip>();

        [HideInInspector]
        public bool isPlaying = false;
    }

    [Header("Channels")]
    public Channel sfx;
    public Channel music;
    public Channel narrator;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public void Play(AudioClip clip, AudioChannel channelType = AudioChannel.SFX)
    {
        if (clip == null) return;

        Channel channel = GetChannel(channelType);

        if (channel.queue.Count >= channel.maxQueueSize)
            channel.queue.Dequeue();

        channel.queue.Enqueue(clip);

        if (!channel.isPlaying)
            StartCoroutine(PlayQueue(channel));
    }

    IEnumerator PlayQueue(Channel channel)
    {
        channel.isPlaying = true;

        while (channel.queue.Count > 0)
        {
            AudioClip clip = channel.queue.Dequeue();

            if (channel.randomPitch)
                channel.source.pitch = Random.Range(channel.pitchRange.x, channel.pitchRange.y);
            else
                channel.source.pitch = 1f;

            channel.source.clip = clip;
            channel.source.volume = channel.volume;
            channel.source.Play();

            while (channel.source.isPlaying)
                yield return null;
        }

        channel.isPlaying = false;
    }


    Channel GetChannel(AudioChannel type)
    {
        switch (type)
        {
            case AudioChannel.Music: return music;
            case AudioChannel.Narrator: return narrator;
            default: return sfx;
        }
    }


    public void Stop(AudioChannel type)
    {
        Channel channel = GetChannel(type);

        StopCoroutine(PlayQueue(channel));
        channel.queue.Clear();
        channel.source.Stop();
        channel.isPlaying = false;
    }

    public void StopAllAudio()
    {
        Stop(AudioChannel.SFX);
        Stop(AudioChannel.Music);
        Stop(AudioChannel.Narrator);
    }
}