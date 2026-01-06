using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChangeType
{
    DisableObject,      // Make an object disappear
    EnableObject,       // Make an object appear
    ChangeColor,        // Change object color
    ChangePosition,     // Move object slightly
    ChangeRotation,     // Rotate object
    ChangeScale,        // Scale object
    SwapObject,         // Swap with another object
    PlayCreepySound,    // Play a creepy sound effect
    PlayWhisper,        // Play whisper sound
    PlayDistortion,     // Play distortion sound
    StopAmbience,       // Stop background ambience
    WeirdAmbience       // Play weird ambience (optionally looping)
}

[System.Serializable]
public class ChangeConfiguration
{
    public ChangeType changeType;

    [Header("Visual Change Details")]
    // Color change
    [SerializeField] private Color newColor = Color.red;

    // Position change
    [SerializeField] private Vector3 positionOffset = Vector3.zero;

    // Rotation change
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    // Scale change
    [SerializeField] private Vector3 scaleMultiplier = Vector3.one;

    // Swap object
    [SerializeField] private GameObject swapWithObject;
    [SerializeField] private bool flipSwappedObject = false; // Rotate 180 degrees on Y axis
    [SerializeField] private Vector3 swapPositionOffset = Vector3.zero; // Position offset for swapped object

    [Header("Audio Change Details")]
    [SerializeField] private bool loopAudio = false; // Loop the audio effect

    // Public getters for the private fields
    public Color NewColor => newColor;
    public Vector3 PositionOffset => positionOffset;
    public Vector3 RotationOffset => rotationOffset;
    public Vector3 ScaleMultiplier => scaleMultiplier;
    public GameObject SwapWithObject => swapWithObject;
    public bool FlipSwappedObject => flipSwappedObject;
    public Vector3 SwapPositionOffset => swapPositionOffset;
    public bool LoopAudio => loopAudio;

    // Helper method to check if field should be visible
    public bool ShowColorField() => changeType == ChangeType.ChangeColor;
    public bool ShowPositionField() => changeType == ChangeType.ChangePosition;
    public bool ShowRotationField() => changeType == ChangeType.ChangeRotation;
    public bool ShowScaleField() => changeType == ChangeType.ChangeScale;
    public bool ShowSwapObjectField() => changeType == ChangeType.SwapObject;
    public bool ShowLoopField() =>
        changeType == ChangeType.PlayCreepySound ||
        changeType == ChangeType.PlayWhisper ||
        changeType == ChangeType.PlayDistortion ||
        changeType == ChangeType.WeirdAmbience;
}

public class HallwayChange : MonoBehaviour
{
    [Header("Change Configuration")]
    [Tooltip("Multiple change types - one will be randomly selected when applied")]
    public List<ChangeConfiguration> changeConfigurations = new List<ChangeConfiguration>();
    public GameObject targetObject;

    // Store selected change and original values
    private ChangeConfiguration selectedChange;
    private bool originalActiveState;
    private Color originalColor;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Renderer objectRenderer;
    private bool changeApplied = false;
    private GameObject instantiatedSwapObject;

    // Spatial audio for sound anomalies
    private AudioSource spatialAudioSource;
    private Coroutine loopingSoundCoroutine;

    void Awake()
    {
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        // Store original values
        originalActiveState = targetObject.activeSelf;
        originalPosition = targetObject.transform.localPosition;
        originalRotation = targetObject.transform.localRotation;
        originalScale = targetObject.transform.localScale;

        objectRenderer = targetObject.GetComponent<Renderer>();
        if (objectRenderer != null && objectRenderer.material != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    public void ApplyChange()
    {
        if (changeApplied) return;

        // Select a random change configuration if multiple are available
        if (changeConfigurations.Count == 0)
        {
            Debug.LogWarning($"No change configurations assigned to {gameObject.name}");
            return;
        }

        selectedChange = changeConfigurations[Random.Range(0, changeConfigurations.Count)];

        switch (selectedChange.changeType)
        {
            case ChangeType.DisableObject:
                targetObject.SetActive(false);
                Debug.Log($"Change Applied: Disabled {targetObject.name}");
                break;

            case ChangeType.EnableObject:
                targetObject.SetActive(true);
                Debug.Log($"Change Applied: Enabled {targetObject.name}");
                break;

            case ChangeType.ChangeColor:
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = selectedChange.NewColor;
                    Debug.Log($"Change Applied: Changed color of {targetObject.name}");
                }
                break;

            case ChangeType.ChangePosition:
                targetObject.transform.localPosition = originalPosition + selectedChange.PositionOffset;
                Debug.Log($"Change Applied: Moved {targetObject.name}");
                break;

            case ChangeType.ChangeRotation:
                targetObject.transform.localRotation = originalRotation * Quaternion.Euler(selectedChange.RotationOffset);
                Debug.Log($"Change Applied: Rotated {targetObject.name}");
                break;

            case ChangeType.ChangeScale:
                targetObject.transform.localScale = Vector3.Scale(originalScale, selectedChange.ScaleMultiplier);
                Debug.Log($"Change Applied: Scaled {targetObject.name}");
                break;

            case ChangeType.SwapObject:
                if (selectedChange.SwapWithObject != null)
                {
                    // Hide original object
                    targetObject.SetActive(false);

                    // Calculate rotation (flip 180 degrees if enabled)
                    Quaternion spawnRotation = targetObject.transform.rotation;
                    if (selectedChange.FlipSwappedObject)
                    {
                        spawnRotation *= Quaternion.Euler(0, 180, 0);
                    }

                    // Instantiate swap object at original position/rotation first
                    instantiatedSwapObject = Instantiate(selectedChange.SwapWithObject,
                        targetObject.transform.position,
                        spawnRotation,
                        targetObject.transform.parent);
                    instantiatedSwapObject.transform.localScale = targetObject.transform.localScale;

                    // Apply position offset after spawning
                    instantiatedSwapObject.transform.position += selectedChange.SwapPositionOffset;

                    Debug.Log($"Change Applied: Swapped {targetObject.name} with {selectedChange.SwapWithObject.name}" +
                             (selectedChange.FlipSwappedObject ? " (flipped 180°)" : "") +
                             (selectedChange.SwapPositionOffset != Vector3.zero ? $" (offset: {selectedChange.SwapPositionOffset})" : ""));
                }
                else
                {
                    Debug.LogWarning($"SwapObject change on {targetObject.name} has no swap object assigned!");
                }
                break;

            case ChangeType.PlayCreepySound:
                if (AudioManager.Instance != null)
                {
                    PlaySpatialAnomalySound(AudioManager.Instance.GetCreepySound(), selectedChange.LoopAudio);
                    Debug.Log($"Change Applied: Playing creepy sound at {targetObject.name} (loop: {selectedChange.LoopAudio})");
                }
                break;

            case ChangeType.PlayWhisper:
                if (AudioManager.Instance != null)
                {
                    PlaySpatialAnomalySound(AudioManager.Instance.GetWhisperSound(), selectedChange.LoopAudio);
                    Debug.Log($"Change Applied: Playing whisper at {targetObject.name} (loop: {selectedChange.LoopAudio})");
                }
                break;

            case ChangeType.PlayDistortion:
                if (AudioManager.Instance != null)
                {
                    PlaySpatialAnomalySound(AudioManager.Instance.GetDistortionSound(), selectedChange.LoopAudio);
                    Debug.Log($"Change Applied: Playing distortion at {targetObject.name} (loop: {selectedChange.LoopAudio})");
                }
                break;

            case ChangeType.StopAmbience:
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopAmbience();
                    Debug.Log($"Change Applied: Stopped ambience");
                }
                break;

            case ChangeType.WeirdAmbience:
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWeirdAmbienceLooping(selectedChange.LoopAudio);
                    Debug.Log($"Change Applied: Playing weird ambience (loop: {selectedChange.LoopAudio})");
                }
                break;
        }

        changeApplied = true;
    }

    public void RevertChange()
    {
        if (!changeApplied || selectedChange == null) return;

        switch (selectedChange.changeType)
        {
            case ChangeType.DisableObject:
            case ChangeType.EnableObject:
                targetObject.SetActive(originalActiveState);
                break;

            case ChangeType.ChangeColor:
                if (objectRenderer != null)
                {
                    objectRenderer.material.color = originalColor;
                }
                break;

            case ChangeType.ChangePosition:
                targetObject.transform.localPosition = originalPosition;
                break;

            case ChangeType.ChangeRotation:
                targetObject.transform.localRotation = originalRotation;
                break;

            case ChangeType.ChangeScale:
                targetObject.transform.localScale = originalScale;
                break;

            case ChangeType.SwapObject:
                // Destroy the swap object
                if (instantiatedSwapObject != null)
                {
                    Destroy(instantiatedSwapObject);
                    instantiatedSwapObject = null;
                }
                // Restore original object
                targetObject.SetActive(originalActiveState);
                break;

            case ChangeType.StopAmbience:
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayNormalAmbience();
                }
                break;

            // Audio effects - stop spatial audio if looping
            case ChangeType.PlayCreepySound:
            case ChangeType.PlayWhisper:
            case ChangeType.PlayDistortion:
                StopSpatialAnomalySound();
                break;

            case ChangeType.WeirdAmbience:
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.RestoreNormalAmbience();
                }
                break;
        }

        changeApplied = false;
    }

    // Play a sound from the targetObject's position with optional looping
    private void PlaySpatialAnomalySound(AudioClip clip, bool loop)
    {
        if (clip == null || targetObject == null) return;

        // Create or get the spatial audio source on the target object
        if (spatialAudioSource == null)
        {
            spatialAudioSource = targetObject.GetComponent<AudioSource>();
            if (spatialAudioSource == null)
            {
                spatialAudioSource = targetObject.AddComponent<AudioSource>();
            }

            // Configure for 3D spatial audio
            spatialAudioSource.spatialBlend = 1f; // Full 3D
            spatialAudioSource.rolloffMode = AudioRolloffMode.Linear;
            spatialAudioSource.minDistance = 1f;
            spatialAudioSource.maxDistance = 15f;
            spatialAudioSource.playOnAwake = false;
        }

        spatialAudioSource.clip = clip;
        spatialAudioSource.loop = loop;
        spatialAudioSource.Play();

        // If not looping, we don't need to track it for stopping
        // If looping, it will be stopped in RevertChange
    }

    private void StopSpatialAnomalySound()
    {
        if (loopingSoundCoroutine != null)
        {
            StopCoroutine(loopingSoundCoroutine);
            loopingSoundCoroutine = null;
        }

        if (spatialAudioSource != null && spatialAudioSource.isPlaying)
        {
            spatialAudioSource.Stop();
        }
    }
}
