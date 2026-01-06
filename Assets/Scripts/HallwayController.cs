using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HallwayController : MonoBehaviour
{
    [Header("Hallway Info")]
    public int hallwayNumber;
    public bool hasChanges = false;
    public bool playerResponse = false; // Player's answer (lights on/off)
    private bool playerEntered = false; // Track if player has entered this hallway

    [Header("Lighting")]
    public Light[] hallwayLights; // All lights in this hallway
    public Renderer[] lightModelRenderers; // Renderers for light models (bulbs, fixtures, etc.)
    public bool lightsOn = true;

    [Header("Emission Settings")]
    public Color emissionColor = Color.white;
    [Range(0f, 10f)] public float emissionIntensity = 2f;

    [Header("Props & Changes")]
    public List<HallwayChange> possibleChanges = new List<HallwayChange>();
    public List<HallwayChange> activeChanges = new List<HallwayChange>();

    [Header("Doors")]
    public DoorController startDoor; // Door at the beginning of hallway
    public DoorController endDoor;   // Door at the end of hallway

    public void Initialize(int number, bool withChanges)
    {
        hallwayNumber = number;
        hasChanges = withChanges;

        // Find all lights if not assigned
        if (hallwayLights == null || hallwayLights.Length == 0)
        {
            hallwayLights = GetComponentsInChildren<Light>();
        }

        // Find doors if not assigned
        if (startDoor == null || endDoor == null)
        {
            DoorController[] doors = GetComponentsInChildren<DoorController>();
            if (doors.Length >= 2)
            {
                startDoor = doors[0];
                endDoor = doors[1];
                Debug.Log($"Hallway {hallwayNumber}: Found {doors.Length} doors. Start: {startDoor.name}, End: {endDoor.name}");
            }
        }

        // Ensure both doors start closed and set proper flags
        if (startDoor != null)
        {
            startDoor.SetDoorState(false, true); // Closed, immediate
            startDoor.isStartDoor = true; // Start door is not interactable by default
            Debug.Log($"Hallway {hallwayNumber}: Start door '{startDoor.name}' set to non-interactable (isStartDoor=true)");
        }
        if (endDoor != null)
        {
            endDoor.SetDoorState(false, true); // Closed, immediate
            endDoor.isStartDoor = false; // End door is always interactable
            Debug.Log($"Hallway {hallwayNumber}: End door '{endDoor.name}' set to interactable (isStartDoor=false)");
        }

        // Apply changes if this hallway should have them
        if (withChanges && possibleChanges.Count > 0)
        {
            ApplyRandomChanges();
        }

        // Hallway 0 special case - always no changes, lights start ON
        if (hallwayNumber == 0)
        {
            hasChanges = false;
            SetLights(true);
        }
        else
        {
            // Other hallways start with lights ON
            SetLights(true);
        }
    }

    void ApplyRandomChanges()
    {
        // Apply only ONE random change (anomaly affects single object)
        if (possibleChanges.Count == 0) return;

        int randomIndex = Random.Range(0, possibleChanges.Count);
        HallwayChange change = possibleChanges[randomIndex];

        // Safety check: skip if change is null
        if (change == null)
        {
            Debug.LogWarning($"Hallway {hallwayNumber}: possibleChanges[{randomIndex}] is null! Check your hallway prefab.");
            return;
        }

        change.ApplyChange();
        activeChanges.Add(change);

        Debug.Log($"Hallway {hallwayNumber}: Applied 1 anomaly to {change.gameObject.name}");
    }

    public void SetLights(bool on)
    {
        lightsOn = on;

        // Toggle Light components
        foreach (Light light in hallwayLights)
        {
            if (light != null)
                light.enabled = on;
        }

        // Toggle emission on light model materials
        foreach (Renderer renderer in lightModelRenderers)
        {
            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (on)
                    {
                        // Enable emission
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                    }
                    else
                    {
                        // Disable emission
                        mat.DisableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.black);
                    }
                }
            }
        }
    }

    public bool GetLightsState()
    {
        return lightsOn;
    }

    public void ToggleLights()
    {
        SetLights(!lightsOn);

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLightSwitchToggled(lightsOn);
        }
    }

    public void SetAsActiveHallway(bool active)
    {
        // End door is always active for progression
        if (endDoor != null)
            endDoor.gameObject.SetActive(true);

        // Start door only active on hallway 0 (entrance to game)
        if (startDoor != null && hallwayNumber == 0)
            startDoor.gameObject.SetActive(true);
    }

    void OnDrawGizmos()
    {
        // Visualize hallway in editor
        Gizmos.color = hasChanges ? Color.red : Color.green;

        // Calculate center position based on door positions if available
        Vector3 centerPos = transform.position;
        if (startDoor != null && endDoor != null)
        {
            // Draw from start door to end door
            Vector3 startPos = startDoor.transform.position;
            Vector3 endPos = endDoor.transform.position;
            centerPos = (startPos + endPos) / 2f;
            // Use hallway root X position and adjust Y height
            centerPos.x = transform.position.x;
            centerPos.y = transform.position.y + 1.5f; // Adjust height from hallway root (lowered)
            float length = Vector3.Distance(startPos, endPos);
            Gizmos.DrawWireCube(centerPos, new Vector3(3f, 2.5f, length));
        }
        else
        {
            // Fallback: offset by half length so cube centers on hallway
            centerPos = transform.position + new Vector3(0, 1.5f, 5f);
            Gizmos.DrawWireCube(centerPos, new Vector3(3f, 2.5f, 10f));
        }
    }

    // Detect when player enters this hallway
    void OnTriggerEnter(Collider other)
    {
        // Always log to debug what's triggering
        Debug.Log($"Trigger entered by: {other.name} (Tag: {other.tag}, Parent: {(other.transform.parent != null ? other.transform.parent.name : "none")})");

        // Check if this is a CharacterController (XR Origin typically uses this)
        CharacterController charController = other.GetComponent<CharacterController>();

        // Check if this collider or any parent is the player
        bool isPlayer = other.CompareTag("Player") || charController != null;

        // Check parents up the hierarchy for XR Origin, Player tag, or CharacterController
        if (!isPlayer)
        {
            Transform current = other.transform;
            while (current != null && !isPlayer)
            {
                if (current.CompareTag("Player") || current.name.Contains("XR Origin"))
                {
                    isPlayer = true;
                    Debug.Log($"Found player in parent: {current.name}");
                    break;
                }

                // Check if parent has CharacterController
                if (current.GetComponent<CharacterController>() != null)
                {
                    isPlayer = true;
                    Debug.Log($"Found CharacterController in parent: {current.name}");
                    break;
                }

                current = current.parent;
            }
        }
        else if (charController != null)
        {
            Debug.Log($"Found CharacterController directly on: {other.name}");
        }

        if (!playerEntered && isPlayer)
        {
            playerEntered = true;
            Debug.Log($"PLAYER ENTERED Hallway {hallwayNumber} - Notifying GameManager");

            // Notify GameManager that player entered
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerEnteredHallway(this);
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null!");
            }
        }
    }
}
