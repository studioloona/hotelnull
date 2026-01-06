using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional: Attach to a trigger zone to detect when player fully enters a new hallway
/// Useful for cleaning up old hallways or triggering events
/// </summary>
public class PlayerTrigger : MonoBehaviour
{
    public int hallwayNumber;
    public HallwayController hallway;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered (XR Origin usually has CharacterController or Rigidbody)
        if (!triggered && (other.CompareTag("Player") || other.name.Contains("XR Origin")))
        {
            triggered = true;
            Debug.Log($"Player entered Hallway {hallwayNumber}");

            // Optional: Notify hallway controller
            if (hallway != null)
            {
                // Could trigger events here like closing door behind player
            }
        }
    }
}
