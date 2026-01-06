using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class LiftButton : MonoBehaviour
{
    [Header("References")]
    public LiftDoors liftDoors;

    [Header("Button Animation")]
    public Transform buttonTransform; // The actual button that moves
    public float pressDistance = 0.02f; // How far the button moves when pressed
    public float pressSpeed = 5f;

    [Header("Timing")]
    public float doorCloseDelay = 0.5f; // Delay before doors start closing
    public float fadeDelay = 2f; // Delay after doors close before fade starts

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Vector3 originalPosition;
    private bool hasBeenPressed = false;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (buttonTransform != null)
        {
            originalPosition = buttonTransform.localPosition;
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
        Debug.Log("Lift button pressed! Starting end sequence...");

        // Animate button press
        if (buttonTransform != null)
        {
            StartCoroutine(AnimateButtonPress());
        }

        // Start the end sequence
        StartCoroutine(EndGameSequence());

        // Play button sound if AudioManager exists
        if (AudioManager.Instance != null)
        {
            // You can add a button press sound here if you want
            // AudioManager.Instance.PlayButtonPress();
        }
    }

    IEnumerator AnimateButtonPress()
    {
        // Press down
        Vector3 pressedPosition = originalPosition - buttonTransform.forward * pressDistance;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * pressSpeed;
            buttonTransform.localPosition = Vector3.Lerp(originalPosition, pressedPosition, t);
            yield return null;
        }

        // Stay pressed
        buttonTransform.localPosition = pressedPosition;
    }

    IEnumerator EndGameSequence()
    {
        // Wait a moment
        yield return new WaitForSeconds(doorCloseDelay);

        // Close lift doors
        if (liftDoors != null)
        {
            Debug.Log("Closing lift doors...");
            liftDoors.CloseDoors();

            // Wait for doors to close
            yield return new WaitForSeconds(liftDoors.closeDuration);
        }

        // Wait a bit after doors close
        yield return new WaitForSeconds(fadeDelay);

        // Start fade to black and credits
        if (FadeController.Instance != null)
        {
            Debug.Log("Starting fade to black and credits...");
            FadeController.Instance.StartFadeAndCredits();
        }
        else
        {
            Debug.LogWarning("FadeController Instance not found in scene!");
        }
    }
}
