using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class LiftButtonController : MonoBehaviour
{
    [Header("Settings")]
    public bool isUpButton = true; // true for Up, false for Down

    [Header("Visual Feedback")]
    public Material pressedMaterial;
    public AudioSource buttonSound;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Renderer buttonRenderer;
    private Material originalMaterial;
    private bool hasBeenPressed = false;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        buttonRenderer = GetComponent<Renderer>();

        if (buttonRenderer != null)
        {
            originalMaterial = buttonRenderer.material;
        }
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnButtonPressed);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnButtonPressed);
    }

    void OnButtonPressed(SelectEnterEventArgs args)
    {
        if (hasBeenPressed) return;

        hasBeenPressed = true;

        // Play sound
        if (buttonSound != null)
        {
            buttonSound.Play();
        }

        // Change material
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        // End game
        string direction = isUpButton ? "UP" : "DOWN";
        Debug.Log($"Lift button pressed: {direction}. Game completed!");

        StartCoroutine(EndGame());
    }

    IEnumerator EndGame()
    {
        yield return new WaitForSeconds(2f);

        // You can add credits screen, scene transition, etc. here
        Debug.Log("=== GAME COMPLETED ===");
        Debug.Log("Rolling credits...");

        // Example: Load credits scene
        // SceneManager.LoadScene("CreditsScene");

        // Or quit application (for standalone builds)
        // Application.Quit();
    }
}
