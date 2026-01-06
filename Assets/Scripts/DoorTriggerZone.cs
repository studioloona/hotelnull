using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Place this on a trigger zone just past the door to detect when player has passed through
/// Attach to a Box Collider (Is Trigger = true) positioned in front of the door
/// </summary>
public class DoorTriggerZone : MonoBehaviour
{
    [Header("Door Reference")]
    public DoorController door; // The door this trigger controls

    [Header("Debug")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;

    void OnTriggerExit(Collider other)
    {
        // Check if player exited the trigger (moved forward through door)
        bool isPlayer = other.CompareTag("Player");

        // Also check for CharacterController in hierarchy
        if (!isPlayer)
        {
            Transform current = other.transform;
            while (current != null && !isPlayer)
            {
                if (current.CompareTag("Player") || current.name.Contains("XR Origin"))
                {
                    isPlayer = true;
                    break;
                }

                if (current.GetComponent<CharacterController>() != null)
                {
                    isPlayer = true;
                    break;
                }

                current = current.parent;
            }
        }

        if (isPlayer && door != null)
        {
            Debug.Log($"Player exited door trigger zone - closing door");
            door.OnPlayerPassedThrough();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
    }
}
