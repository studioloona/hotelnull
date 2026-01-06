using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class LightSwitchInteractable : MonoBehaviour
{
    [Header("References")]
    public HallwayController hallwayController;

    [Header("Visual Feedback")]
    public GameObject switchOnVisual;  // Visual indicator when lights are ON
    public GameObject switchOffVisual; // Visual indicator when lights are OFF
    public AudioSource toggleSound;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool lightsOn = true;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void Start()
    {
        // Find hallway controller if not assigned
        if (hallwayController == null)
        {
            hallwayController = GetComponentInParent<HallwayController>();
        }

        UpdateVisuals();
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        ToggleSwitch();
    }

    public void ToggleSwitch()
    {
        lightsOn = !lightsOn;

        // Update hallway lights
        if (hallwayController != null)
        {
            hallwayController.SetLights(lightsOn);
        }

        // Play sound
        if (toggleSound != null)
        {
            toggleSound.Play();
        }

        // Update visuals
        UpdateVisuals();

        Debug.Log($"Light switch toggled. Lights are now: {(lightsOn ? "ON" : "OFF")}");
    }

    void UpdateVisuals()
    {
        if (switchOnVisual != null)
            switchOnVisual.SetActive(lightsOn);

        if (switchOffVisual != null)
            switchOffVisual.SetActive(!lightsOn);
    }

    public void ResetSwitch()
    {
        lightsOn = true;
        UpdateVisuals();
    }
}
