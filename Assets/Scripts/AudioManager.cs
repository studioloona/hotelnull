using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup ambienceGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Ambience Clips")]
    [SerializeField] private AudioClip normalAmbience;
    [SerializeField] private AudioClip[] weirdAmbienceClips;

    [Header("Door SFX")]
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField] private AudioClip doorLockedSound;

    [Header("Player SFX")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip playerBreathSound;

    [Header("Anomaly SFX")]
    [SerializeField] private AudioClip[] creepySounds;
    [SerializeField] private AudioClip[] whisperSounds;
    [SerializeField] private AudioClip[] distortionSounds;

    [Header("Volume Settings (dB)")]
    [Range(-80f, 0f)] public float ambienceVolume = -10f;
    [Range(-80f, 0f)] public float sfxVolume = -5f;
    [Range(-80f, 0f)] public float masterVolume = 0f;

    [Header("Footstep Volume")]
    [Range(0f, 1f)] public float footstepVolumeScale = 0.3f; // Lower volume for footsteps

    private Coroutine ambienceFadeCoroutine;
    private Coroutine weirdAmbienceCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeAudioSources()
    {
        // Create audio sources if not assigned
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f; // 2D sound
            if (ambienceGroup != null)
                ambienceSource.outputAudioMixerGroup = ambienceGroup;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D sound
            if (sfxGroup != null)
                sfxSource.outputAudioMixerGroup = sfxGroup;
        }

        UpdateVolumes();
    }

    void Start()
    {
        PlayNormalAmbience();
    }

    public void UpdateVolumes()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", masterVolume);
            audioMixer.SetFloat("AmbienceVolume", ambienceVolume);
            audioMixer.SetFloat("SFXVolume", sfxVolume);
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp(volume, -80f, 0f);
        if (audioMixer != null)
            audioMixer.SetFloat("MasterVolume", masterVolume);
    }

    public void SetAmbienceVolume(float volume)
    {
        ambienceVolume = Mathf.Clamp(volume, -80f, 0f);
        if (audioMixer != null)
            audioMixer.SetFloat("AmbienceVolume", ambienceVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp(volume, -80f, 0f);
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", sfxVolume);
    }

    #region Ambience Control

    public void PlayNormalAmbience()
    {
        if (normalAmbience != null && ambienceSource != null)
        {
            StopWeirdAmbience();
            bool wasPlaying = ambienceSource.isPlaying;
            bool wasNormalClip = ambienceSource.clip == normalAmbience;
            bool needsRestart = !wasPlaying || !wasNormalClip;

            Debug.Log($"PlayNormalAmbience: wasPlaying={wasPlaying}, wasNormalClip={wasNormalClip}, needsRestart={needsRestart}");

            ambienceSource.clip = normalAmbience;
            ambienceSource.loop = true;
            ambienceSource.Play(); // Always call Play to ensure it's playing

            Debug.Log($"PlayNormalAmbience: Called Play(). isPlaying now = {ambienceSource.isPlaying}, volume = {ambienceSource.volume}");
        }
        else
        {
            Debug.LogWarning($"PlayNormalAmbience: Cannot play - normalAmbience is {(normalAmbience == null ? "null" : "set")}, ambienceSource is {(ambienceSource == null ? "null" : "set")}");
        }
    }

    public void PlayWeirdAmbience()
    {
        if (weirdAmbienceClips != null && weirdAmbienceClips.Length > 0)
        {
            AudioClip randomWeird = weirdAmbienceClips[Random.Range(0, weirdAmbienceClips.Length)];
            PlayAmbience(randomWeird);
        }
    }

    // Play weird ambience - optionally looping until StopWeirdAmbience is called
    public void PlayWeirdAmbienceLooping(bool loop = true)
    {
        if (weirdAmbienceClips != null && weirdAmbienceClips.Length > 0)
        {
            StopWeirdAmbience();
            AudioClip randomWeird = weirdAmbienceClips[Random.Range(0, weirdAmbienceClips.Length)];
            ambienceSource.clip = randomWeird;
            ambienceSource.loop = loop;
            ambienceSource.Play();

            // If not looping, start a coroutine to restore normal ambience after the clip finishes
            if (!loop)
            {
                weirdAmbienceCoroutine = StartCoroutine(RestoreAmbienceAfterClip(randomWeird.length));
            }
        }
    }

    private IEnumerator RestoreAmbienceAfterClip(float clipLength)
    {
        yield return new WaitForSeconds(clipLength + 0.1f);
        weirdAmbienceCoroutine = null;
        PlayNormalAmbience();
    }

    // Stop weird ambience and restore normal ambience
    public void RestoreNormalAmbience()
    {
        StopWeirdAmbience();
        ambienceSource.loop = true; // Ensure looping for normal ambience
        PlayNormalAmbience();
    }

    private void PlayAmbience(AudioClip clip)
    {
        if (ambienceSource != null && clip != null)
        {
            if (ambienceSource.clip != clip)
            {
                ambienceSource.clip = clip;
                ambienceSource.Play();
            }
        }
    }

    public void StopAmbience()
    {
        if (ambienceSource != null)
            ambienceSource.Stop();
    }

    public bool IsAmbiencePlaying()
    {
        return ambienceSource != null && ambienceSource.isPlaying;
    }

    public void StopWeirdAmbience()
    {
        if (weirdAmbienceCoroutine != null)
        {
            StopCoroutine(weirdAmbienceCoroutine);
            weirdAmbienceCoroutine = null;
        }
    }

    public void StopAllAmbienceFades()
    {
        if (ambienceFadeCoroutine != null)
        {
            StopCoroutine(ambienceFadeCoroutine);
            ambienceFadeCoroutine = null;
        }
    }

    // Ensure ambience keeps playing - call this to restart if stopped
    public void EnsureAmbiencePlaying()
    {
        if (ambienceSource != null && !ambienceSource.isPlaying)
        {
            ambienceSource.loop = true;
            PlayNormalAmbience();
        }
    }

    #endregion

    #region Door SFX

    public void PlayDoorOpen()
    {
        PlaySFX(doorOpenSound);
    }

    public void PlayDoorClose()
    {
        PlaySFX(doorCloseSound);
    }

    public void PlayDoorLocked()
    {
        PlaySFX(doorLockedSound);
    }

    #endregion

    #region Player SFX

    public void PlayFootstep()
    {
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            AudioClip footstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySFX(footstep, footstepVolumeScale);
        }
    }

    public void SetFootstepVolume(float volume)
    {
        footstepVolumeScale = Mathf.Clamp01(volume);
    }

    public void PlayBreath()
    {
        PlaySFX(playerBreathSound);
    }

    #endregion

    #region Anomaly SFX

    public void PlayCreepySound()
    {
        if (creepySounds != null && creepySounds.Length > 0)
        {
            AudioClip creepy = creepySounds[Random.Range(0, creepySounds.Length)];
            PlaySFX(creepy);
        }
    }

    public void PlayWhisper()
    {
        if (whisperSounds != null && whisperSounds.Length > 0)
        {
            AudioClip whisper = whisperSounds[Random.Range(0, whisperSounds.Length)];
            PlaySFX(whisper);
        }
    }

    public void PlayDistortion()
    {
        if (distortionSounds != null && distortionSounds.Length > 0)
        {
            AudioClip distortion = distortionSounds[Random.Range(0, distortionSounds.Length)];
            PlaySFX(distortion);
        }
    }

    public void PlayRandomAnomalySound()
    {
        int random = Random.Range(0, 3);
        switch (random)
        {
            case 0:
                PlayCreepySound();
                break;
            case 1:
                PlayWhisper();
                break;
            case 2:
                PlayDistortion();
                break;
        }
    }

    // Getters for spatial audio use in HallwayChange
    public AudioClip GetCreepySound()
    {
        if (creepySounds != null && creepySounds.Length > 0)
            return creepySounds[Random.Range(0, creepySounds.Length)];
        return null;
    }

    public AudioClip GetWhisperSound()
    {
        if (whisperSounds != null && whisperSounds.Length > 0)
            return whisperSounds[Random.Range(0, whisperSounds.Length)];
        return null;
    }

    public AudioClip GetDistortionSound()
    {
        if (distortionSounds != null && distortionSounds.Length > 0)
            return distortionSounds[Random.Range(0, distortionSounds.Length)];
        return null;
    }

    #endregion

    #region General SFX

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }

    #endregion
}
