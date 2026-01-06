using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// VR Menu Controller - Handles pause menu with restart and quit options.
///
/// SETUP:
/// 1. Create an empty GameObject called "VRMenuManager" (always active)
/// 2. Add this script to VRMenuManager
/// 3. Create a World Space Canvas as a CHILD of VRMenuManager
/// 4. Set canvas Render Mode to World Space, size 800x500, scale 0.001
/// 5. Add UI buttons for Restart, Quit, and Resume to the canvas
/// 6. Add TrackedDeviceGraphicRaycaster to the canvas
/// 7. Assign the canvas and buttons in this script
/// 8. Assign the Menu Action from your XRI Default Input Actions asset
///
/// The menu follows the player's head and can be toggled with the menu button.
/// The script stays on an always-active parent so it can receive input.
/// </summary>
public class VRMenuController : MonoBehaviour
{
    public static VRMenuController Instance { get; private set; }

    [Header("Menu Canvas")]
    [Tooltip("The World Space Canvas containing the menu (will be enabled/disabled)")]
    public GameObject menuCanvasObject;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Menu Positioning")]
    [Tooltip("Reference to the VR camera/head")]
    public Transform vrCamera;
    [Tooltip("Distance from player when menu opens")]
    public float menuDistance = 2f;
    [Tooltip("Height offset from camera")]
    public float heightOffset = -0.2f;

    [Header("Input - Use InputActionReference")]
    [Tooltip("Reference to the menu button action from your Input Actions asset")]
    public InputActionReference menuActionReference;

    [Header("Audio")]
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;
    public AudioClip buttonClickSound;

    private bool isMenuOpen = false;
    private AudioSource audioSource;
    private InputAction menuAction;

    void Awake()
    {
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

    void OnEnable()
    {
        // Setup input action
        if (menuActionReference != null)
        {
            menuAction = menuActionReference.action;
            menuAction.Enable();
            menuAction.performed += OnMenuButtonPressed;
            Debug.Log("VRMenuController: Menu action enabled");
        }
        else
        {
            Debug.LogWarning("VRMenuController: menuActionReference is not assigned!");
        }
    }

    void OnDisable()
    {
        // Cleanup input action
        if (menuAction != null)
        {
            menuAction.performed -= OnMenuButtonPressed;
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Menu button pressed!");
        ToggleMenu();
    }

    void Start()
    {
        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.playOnAwake = false;
        }

        // Find VR camera if not assigned
        if (vrCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                vrCamera = mainCam.transform;
            }
        }

        // Setup button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        // Start with menu hidden
        if (menuCanvasObject != null)
        {
            menuCanvasObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check keyboard for testing (Escape key) - using new Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }

        // Keep menu facing player when open
        if (isMenuOpen && menuCanvasObject != null && vrCamera != null)
        {
            // Make menu face the camera
            menuCanvasObject.transform.LookAt(vrCamera);
            menuCanvasObject.transform.Rotate(0, 180, 0); // Flip to face correctly
        }
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        Debug.Log($"OpenMenu called. menuCanvasObject={menuCanvasObject}, vrCamera={vrCamera}");

        if (menuCanvasObject == null)
        {
            Debug.LogError("OpenMenu: menuCanvasObject is null!");
            return;
        }

        if (vrCamera == null)
        {
            Debug.LogError("OpenMenu: vrCamera is null!");
            return;
        }

        isMenuOpen = true;

        // Position menu in front of player
        Vector3 forward = vrCamera.forward;
        forward.y = 0; // Keep horizontal
        forward.Normalize();

        Vector3 menuPosition = vrCamera.position + forward * menuDistance;
        menuPosition.y = vrCamera.position.y + heightOffset;

        menuCanvasObject.transform.position = menuPosition;
        menuCanvasObject.transform.LookAt(vrCamera);
        menuCanvasObject.transform.Rotate(0, 180, 0);

        menuCanvasObject.SetActive(true);

        Debug.Log($"OpenMenu: Canvas activated. Active = {menuCanvasObject.activeSelf}, Position = {menuCanvasObject.transform.position}");

        // Play sound
        PlaySound(menuOpenSound);

        // Note: Do NOT pause TimeScale in VR - player needs to move head/hands to interact

        Debug.Log("VR Menu opened");
    }

    public void CloseMenu()
    {
        if (menuCanvasObject == null) return;

        isMenuOpen = false;
        menuCanvasObject.SetActive(false);

        // Play sound
        PlaySound(menuCloseSound);

        // Optional: Resume game
        // Time.timeScale = 1f;

        Debug.Log("VR Menu closed");
    }

    public void ResumeGame()
    {
        PlaySound(buttonClickSound);
        CloseMenu();
    }

    public void RestartGame()
    {
        PlaySound(buttonClickSound);

        Debug.Log("Restarting game...");

        // Reset time scale in case it was paused
        Time.timeScale = 1f;

        // Start coroutine for slight delay so sound plays
        StartCoroutine(RestartAfterDelay(0.2f));
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        PlaySound(buttonClickSound);

        Debug.Log("Quitting game...");

        // Start coroutine for slight delay so sound plays
        StartCoroutine(QuitAfterDelay(0.2f));
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Public method to check if menu is open (for other scripts)
    public bool IsMenuOpen()
    {
        return isMenuOpen;
    }
}
