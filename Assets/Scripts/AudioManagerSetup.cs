using UnityEngine;

/// <summary>
/// Example setup helper for AudioManager
/// Add this to an empty GameObject in your scene and it will create the AudioManager automatically
/// </summary>
public class AudioManagerSetup : MonoBehaviour
{
    [Header("Create AudioManager on Start")]
    [Tooltip("If enabled, will create AudioManager GameObject if it doesn't exist")]
    public bool autoCreateAudioManager = true;

    void Start()
    {
        if (autoCreateAudioManager && AudioManager.Instance == null)
        {
            GameObject audioManagerObj = new GameObject("AudioManager");
            audioManagerObj.AddComponent<AudioManager>();
            Debug.Log("AudioManager created automatically");
        }
    }
}
