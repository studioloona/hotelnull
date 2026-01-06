using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public bool isStartDoor = false; // Start door of hallway (disabled) or end door (interactable)
    public bool isLocked = false; // If true, door cannot be opened and plays locked sound
    public Transform doorPivot; // The part of the door that rotates
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool autoClose = true; // Auto-close when player passes through

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isOpen = false;
    private bool isAnimating = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool playerHasPassed = false; // Track if player passed through

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (doorPivot == null)
        {
            doorPivot = transform;
        }

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"Door '{gameObject.name}' poked. isAnimating={isAnimating}, isStartDoor={isStartDoor}, isLocked={isLocked}");

        // Check if door is locked first
        if (isLocked)
        {
            PlayLockedSound();
            Debug.Log($"Door '{gameObject.name}' is locked");
            return;
        }

        if (!isAnimating && !isStartDoor)
        {
            Debug.Log($"Door '{gameObject.name}' opening...");
            OpenDoor();
        }
        else
        {
            if (isStartDoor)
                Debug.Log($"Door '{gameObject.name}' is a start door, cannot open");
            if (isAnimating)
                Debug.Log($"Door '{gameObject.name}' is animating, cannot open");
        }
    }

    void PlayLockedSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDoorLocked();
        }
    }

    public void OpenDoor()
    {
        if (isOpen || isAnimating)
        {
            Debug.Log($"Door '{gameObject.name}' OpenDoor() called but isOpen={isOpen}, isAnimating={isAnimating}");
            return;
        }

        // Reset the passed flag when door opens
        playerHasPassed = false;

        Debug.Log($"Door '{gameObject.name}' calling GameManager.OnDoorOpened()");

        // Notify GameManager that door is being opened
        if (GameManager.Instance != null && !isStartDoor)
        {
            GameManager.Instance.OnDoorOpened();
        }

        StartCoroutine(AnimateDoor(true));
    }

    public void CloseDoor()
    {
        if (!isOpen || isAnimating) return;

        StartCoroutine(AnimateDoor(false));
    }

    IEnumerator AnimateDoor(bool open)
    {
        isAnimating = true;

        // Play sound through AudioManager
        if (AudioManager.Instance != null)
        {
            if (open)
                AudioManager.Instance.PlayDoorOpen();
            else
                AudioManager.Instance.PlayDoorClose();
        }

        Quaternion startRotation = doorPivot.localRotation;
        Quaternion targetRotation = open ? openRotation : closedRotation;

        float elapsed = 0f;
        float duration = 1f / openSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            doorPivot.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        doorPivot.localRotation = targetRotation;
        isOpen = open;
        isAnimating = false;
    }

    public void SetDoorState(bool open, bool immediate = false)
    {
        if (immediate)
        {
            isOpen = open;
            doorPivot.localRotation = open ? openRotation : closedRotation;
        }
        else
        {
            if (open && !isOpen)
                OpenDoor();
            else if (!open && isOpen)
                CloseDoor();
        }
    }

    // Force the door to closed state, recalculating rotations if needed
    public void ForceClose()
    {
        isOpen = false;
        isAnimating = false;
        // Reset to identity rotation (closed state)
        doorPivot.localRotation = Quaternion.identity;
        closedRotation = Quaternion.identity;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
    }

    // Called by trigger zone when player exits forward through the door
    public void OnPlayerPassedThrough()
    {
        if (autoClose && isOpen && !playerHasPassed)
        {
            playerHasPassed = true;
            Debug.Log($"Player passed through door, auto-closing...");
            CloseDoor();
        }
    }
}
