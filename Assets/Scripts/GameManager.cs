using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Playing,        // Normal gameplay
    Transitioning,  // Moving between hallways
    Resetting,      // Wrong guess - smoothly resetting
    Completed       // Game won - at lift
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Hallway Setup")]
    public GameObject hallwayPrefab; // The hallway prefab to spawn (includes both doors)
    public GameObject hallway0Prefab; // Optional: Special prefab for Hallway 0 (reset hallway)
    public HallwayController manualHallway0; // Optional: Manually placed Hallway 0 in scene
    public Transform playerTransform; // XR Origin transform
    public float hallwayLength = 10f; // Length of each hallway (distance between door centers)

    [Header("Anomaly Settings")]
    [Range(0f, 1f)]
    public float anomalyChance = 0.3f; // Chance of an anomaly occurring (30% default)

    [Header("Game Completion")]
    public int totalHallways = 6; // Total hallways before reaching the lift
    public GameObject liftHallwayPrefab; // Special hallway with lift at the end

    [Header("Game State")]
    public GameState currentState = GameState.Playing;
    public int currentHallwayNumber = 0; // Current hallway player is in
    public int progressHallwayCount = 0; // Actual progression count (for win condition)
    public bool hasChanges = false; // Does current hallway have changes?

    private List<HallwayController> activeHallways = new List<HallwayController>();
    private HallwayController currentHallway;
    private HallwayController correctNextHallway; // Pre-spawned for correct guess
    private HallwayController resetHallway;       // Pre-spawned reset hallway (always Hallway 0)
    private GameObject liftHallway;               // Pre-spawned lift hallway for final level

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Spawn the first two hallways (0 and 1)
        SpawnInitialHallways();
    }

    void SpawnInitialHallways()
    {
        currentState = GameState.Playing;
        currentHallwayNumber = 0;
        progressHallwayCount = 0;

        // Hallway 0 (starting hallway - no changes)
        if (manualHallway0 != null)
        {
            // Use manually placed hallway from scene
            currentHallway = manualHallway0;
            currentHallway.Initialize(0, false);
            activeHallways.Add(currentHallway);
        }
        else
        {
            // Spawn hallway 0 dynamically
            currentHallway = SpawnHallway(0, Vector3.zero, false);
        }
        currentHallway.SetAsActiveHallway(true);

        // Pre-spawn next hallway for correct answer (Hallway 1)
        Vector3 nextPosition = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
        bool nextHasChanges = Random.value < anomalyChance;
        correctNextHallway = SpawnHallway(1, nextPosition, nextHasChanges, false);
        correctNextHallway.SetAsActiveHallway(false);

        // Pre-spawn reset hallway (Hallway 0 copy) for wrong answer
        Vector3 resetPosition = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
        resetHallway = SpawnHallway(0, resetPosition, false, false);
        resetHallway.SetAsActiveHallway(false);
        // Keep start door disabled and non-interactable on reset hallway
        if (resetHallway.startDoor != null)
        {
            resetHallway.startDoor.gameObject.SetActive(false);
            resetHallway.startDoor.isStartDoor = true; // Not interactable
            resetHallway.startDoor.SetDoorState(false, true); // Closed
        }
        resetHallway.gameObject.SetActive(false); // Hidden until needed
    }
    HallwayController SpawnHallway(int hallwayNumber, Vector3 position, bool withChanges, bool rotated = false)
    {
        Quaternion rotation = rotated ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        // Use hallway0Prefab for hallway 0 if assigned, otherwise use normal hallwayPrefab
        GameObject prefabToUse = (hallwayNumber == 0 && hallway0Prefab != null) ? hallway0Prefab : hallwayPrefab;
        GameObject hallwayObj = Instantiate(prefabToUse, position, rotation);
        hallwayObj.name = $"Hallway_{hallwayNumber}";

        HallwayController hallway = hallwayObj.GetComponent<HallwayController>();
        if (hallway != null)
        {
            hallway.Initialize(hallwayNumber, withChanges);
            activeHallways.Add(hallway);

            // Disable start door on spawned hallways (end door of previous hallway serves as entrance)
            if (hallwayNumber > 0 && hallway.startDoor != null)
            {
                hallway.startDoor.gameObject.SetActive(false);
            }
        }

        return hallway;
    }

    public void OnLightSwitchToggled(bool lightsOn)
    {
        // Player's response: lights OFF = changes detected, lights ON = no changes
        bool playerSaysChanges = !lightsOn; // Inverted logic
        bool actuallyHasChanges = currentHallway.hasChanges;

        Debug.Log($"Hallway {currentHallwayNumber}: Player says changes={playerSaysChanges}, Actual changes={actuallyHasChanges}");

        // Check if player is correct
        if (playerSaysChanges == actuallyHasChanges)
        {
            // Correct! Store the result for door opening
            currentHallway.playerResponse = playerSaysChanges;
            Debug.Log("Correct answer! You may proceed through the door.");
        }
        else
        {
            Debug.Log("Wrong answer! Game will reset when you open the door.");
            currentHallway.playerResponse = playerSaysChanges;
        }
    }

    public void OnDoorOpened()
    {
        Debug.Log($"=== OnDoorOpened CALLED === State: {currentState}, Hallway: {currentHallwayNumber}, Final: {currentHallwayNumber == totalHallways - 1}");

        if (currentState != GameState.Playing)
        {
            return;
        }

        // Check if player was correct
        bool playerSaysChanges = !currentHallway.GetLightsState(); // lights OFF = anomaly
        bool actuallyHasChanges = currentHallway.hasChanges;

        Debug.Log($"Player says changes: {playerSaysChanges}, Actually has changes: {actuallyHasChanges}, Correct: {playerSaysChanges == actuallyHasChanges}");

        // Check if we're in the final hallway and opening its door
        if (currentHallwayNumber == totalHallways - 1)
        {
            if (playerSaysChanges == actuallyHasChanges)
            {
                // Correct guess - show lift, hide reset
                currentState = GameState.Completed;

                if (resetHallway != null)
                {
                    resetHallway.gameObject.SetActive(false);
                }
                if (liftHallway != null)
                {
                    liftHallway.SetActive(true);
                }
                return;
            }
            else
            {
                // Wrong guess - hide lift, spawn fresh reset hallway
                currentState = GameState.Resetting;

                // Disable lift
                if (liftHallway != null)
                {
                    liftHallway.SetActive(false);
                }

                // Destroy the old reset hallway at lift position
                if (resetHallway != null)
                {
                    activeHallways.Remove(resetHallway);
                    Destroy(resetHallway.gameObject);
                    resetHallway = null;
                }

                // Spawn a fresh reset hallway at the lift position with doors closed
                Vector3 resetPos = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
                resetHallway = SpawnHallway(0, resetPos, false, false);
                resetHallway.SetAsActiveHallway(true);

                // Disable start door
                if (resetHallway.startDoor != null)
                {
                    resetHallway.startDoor.gameObject.SetActive(false);
                    Renderer[] doorRenderers = resetHallway.startDoor.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in doorRenderers)
                    {
                        renderer.enabled = false;
                    }
                    Collider[] doorColliders = resetHallway.startDoor.GetComponentsInChildren<Collider>();
                    foreach (var collider in doorColliders)
                    {
                        collider.enabled = false;
                    }
                }

                // Force end door CLOSED - player opens it themselves
                if (resetHallway.endDoor != null)
                {
                    resetHallway.endDoor.ForceClose();
                }

                StartCoroutine(TransitionToResetHallway());
                return;
            }
        }

        if (playerSaysChanges == actuallyHasChanges)
        {
            // Correct guess - activate correct path
            Debug.Log("CORRECT! Progressing...");
            currentState = GameState.Transitioning;

            // Hide reset hallway, show correct hallway
            resetHallway.gameObject.SetActive(false);
            correctNextHallway.gameObject.SetActive(true);

            StartCoroutine(TransitionToCorrectHallway(playerSaysChanges));
        }
        else
        {
            // Wrong guess - spawn fresh reset hallway
            currentState = GameState.Resetting;

            // Hide and destroy correct hallway
            if (correctNextHallway != null)
            {
                activeHallways.Remove(correctNextHallway);
                Destroy(correctNextHallway.gameObject);
                correctNextHallway = null;
            }

            // Destroy old reset hallway
            if (resetHallway != null)
            {
                activeHallways.Remove(resetHallway);
                Destroy(resetHallway.gameObject);
                resetHallway = null;
            }

            // Spawn a fresh reset hallway with doors closed
            Vector3 resetPos = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
            resetHallway = SpawnHallway(0, resetPos, false, false);
            resetHallway.SetAsActiveHallway(true);

            // Disable start door
            if (resetHallway.startDoor != null)
            {
                resetHallway.startDoor.gameObject.SetActive(false);
                Renderer[] doorRenderers = resetHallway.startDoor.GetComponentsInChildren<Renderer>();
                foreach (var renderer in doorRenderers)
                {
                    renderer.enabled = false;
                }
                Collider[] doorColliders = resetHallway.startDoor.GetComponentsInChildren<Collider>();
                foreach (var collider in doorColliders)
                {
                    collider.enabled = false;
                }
            }

            // Force end door CLOSED
            if (resetHallway.endDoor != null)
            {
                resetHallway.endDoor.ForceClose();
            }

            StartCoroutine(TransitionToResetHallway());
        }
    }

    IEnumerator TransitionToCorrectHallway(bool playerFoundChanges)
    {
        // Update progression immediately
        progressHallwayCount++;
        currentHallwayNumber = progressHallwayCount; // Always match progress count

        Debug.Log($"Progressing to hallway {currentHallwayNumber} (Progress: {progressHallwayCount}/{totalHallways})");

        // Move to correct hallway immediately
        currentHallway = correctNextHallway;
        currentHallway.SetAsActiveHallway(true);
        currentHallway.hallwayNumber = currentHallwayNumber;

        Debug.Log($"Player now in Hallway {currentHallway.hallwayNumber}");

        // Stay in Transitioning state - will change to Playing when player enters new hallway
        // Next hallways will be spawned after cleanup
        Debug.Log("Waiting for player to enter new hallway...");

        yield break;
    }

    IEnumerator TransitionToResetHallway()
    {
        Debug.Log("Player will enter reset hallway...");

        // Move to reset hallway immediately
        currentHallway = resetHallway;
        currentHallway.SetAsActiveHallway(true);
        currentHallway.hallwayNumber = 0;

        // Reset game progress
        currentHallwayNumber = 0;
        progressHallwayCount = 0;

        // Destroy the lift hallway if it exists
        if (liftHallway != null)
        {
            Destroy(liftHallway);
            liftHallway = null;
        }

        Debug.Log("Reset complete. Back at Hallway 0.");

        // Don't spawn next hallways here - CleanupOldHallwaysAfterDoorClose will handle it
        // when player enters the reset hallway

        // Stay in Resetting state - will change to Playing when player enters reset hallway
        Debug.Log("Waiting for player to enter reset hallway...");
        yield break;
    }

    // Called when player enters a new hallway (by trigger collider)
    public void OnPlayerEnteredHallway(HallwayController enteredHallway)
    {
        Debug.Log($"OnPlayerEnteredHallway called: Hallway {enteredHallway.hallwayNumber}, Current State: {currentState}");

        // Reset audio to normal state when entering new hallway
        ResetAudioForNewHallway();

        // Check if player entered the final hallway - spawn lift if not already spawned
        if (enteredHallway.hallwayNumber == totalHallways - 1 && liftHallway == null)
        {
            SpawnLiftHallway();

            // Destroy the old reset hallway before spawning new one at lift position
            if (resetHallway != null)
            {
                activeHallways.Remove(resetHallway);
                Destroy(resetHallway.gameObject);
            }

            // Spawn reset hallway at the SAME position as lift hallway
            Vector3 liftPos = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
            resetHallway = SpawnHallway(0, liftPos, false, false);
            resetHallway.SetAsActiveHallway(false);
            if (resetHallway.startDoor != null)
            {
                resetHallway.startDoor.gameObject.SetActive(false);
                resetHallway.startDoor.isStartDoor = true;
                resetHallway.startDoor.SetDoorState(false, true);
            }
            resetHallway.gameObject.SetActive(false);
            Debug.Log($"Spawned reset hallway at SAME position as lift: {liftPos}");
        }

        if (currentState == GameState.Transitioning)
        {
            // Player entered correct path hallway - clean up old hallways
            Debug.Log($"State=Transitioning: Player entered Hallway {enteredHallway.hallwayNumber}. Cleaning up old hallways...");

            // First, close doors on hallways we're about to delete (with delay)
            StartCoroutine(CleanupOldHallwaysAfterDoorClose(enteredHallway, false));
        }
        else if (currentState == GameState.Resetting)
        {
            // Player entered reset hallway - clean up old hallways
            Debug.Log($"State=Resetting: Player entered reset Hallway {enteredHallway.hallwayNumber}. Cleaning up...");

            // First, close doors on hallways we're about to delete (with delay)
            StartCoroutine(CleanupOldHallwaysAfterDoorClose(enteredHallway, true));
        }
        else
        {
            Debug.Log($"OnPlayerEnteredHallway ignored - state is {currentState}");
        }
    }

    void ResetAudioForNewHallway()
    {
        if (AudioManager.Instance != null)
        {
            // Stop any weird ambience coroutines
            AudioManager.Instance.StopWeirdAmbience();

            // Stop any ongoing fade effects
            AudioManager.Instance.StopAllAmbienceFades();

            // Always restore normal ambience when entering a new hallway
            AudioManager.Instance.PlayNormalAmbience();

            // Restore ambience volume if it was faded
            AudioManager.Instance.SetAmbienceVolume(AudioManager.Instance.ambienceVolume);

            Debug.Log("Audio reset to normal for new hallway");
        }
    }

    IEnumerator CleanupOldHallwaysAfterDoorClose(HallwayController enteredHallway, bool isReset)
    {
        // Close doors on old hallways before destroying them
        List<HallwayController> hallwaysToRemove = new List<HallwayController>();

        if (isReset)
        {
            // Clean up correct path hallway
            if (correctNextHallway != null)
            {
                hallwaysToRemove.Add(correctNextHallway);
            }

            // Collect all old hallways except the one player just entered
            foreach (var hallway in activeHallways)
            {
                if (hallway != enteredHallway)
                {
                    hallwaysToRemove.Add(hallway);
                }
            }
        }
        else
        {
            // Clean up reset hallway - but NOT if we're entering the final hallway
            // (the reset hallway at lift position should be kept)
            bool isEnteringFinalHallway = (enteredHallway.hallwayNumber == totalHallways - 1);
            if (resetHallway != null && resetHallway != enteredHallway && !isEnteringFinalHallway)
            {
                hallwaysToRemove.Add(resetHallway);
            }

            // Collect old hallways except the one player just entered
            // Also exclude resetHallway if entering final hallway (it's at lift position)
            foreach (var hallway in activeHallways)
            {
                bool keepHallway = (hallway == enteredHallway) ||
                                   (hallway == correctNextHallway) ||
                                   (isEnteringFinalHallway && hallway == resetHallway);
                if (!keepHallway)
                {
                    hallwaysToRemove.Add(hallway);
                }
            }
        }

        // Close all end doors on hallways to be removed
        foreach (var hallway in hallwaysToRemove)
        {
            if (hallway != null && hallway.endDoor != null)
            {
                Debug.Log($"Closing door on hallway {hallway.hallwayNumber} before cleanup");
                hallway.endDoor.CloseDoor();
            }
        }

        // Wait for doors to fully close (door animation time)
        yield return new WaitForSeconds(0.6f);

        // Enable start door IMMEDIATELY after animation (before destroying old hallways)
        // The overlap doesn't matter since old hallways are about to be destroyed anyway
        if (enteredHallway.startDoor != null && enteredHallway.hallwayNumber != 0)
        {
            Debug.Log($"Enabling start door on hallway {enteredHallway.hallwayNumber} to close gap");
            enteredHallway.startDoor.SetDoorState(false, true); // Ensure closed
            enteredHallway.startDoor.isStartDoor = true; // Make non-interactable
            enteredHallway.startDoor.gameObject.SetActive(true);
            // Re-enable renderers
            Renderer[] doorRenderers = enteredHallway.startDoor.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in doorRenderers)
            {
                renderer.enabled = true;
            }
            // Re-enable colliders
            Collider[] doorColliders = enteredHallway.startDoor.GetComponentsInChildren<Collider>(true);
            foreach (var collider in doorColliders)
            {
                collider.enabled = true;
            }
        }
        else if (enteredHallway.hallwayNumber == 0)
        {
            Debug.Log($"Hallway 0 entered - enabling start door to close gap after player passed");
            // For Hallway 0, enable the start door now that player has passed through
            if (enteredHallway.startDoor != null)
            {
                enteredHallway.startDoor.SetDoorState(false, true); // Ensure closed
                enteredHallway.startDoor.isStartDoor = true; // Make non-interactable
                enteredHallway.startDoor.gameObject.SetActive(true);
                // Re-enable renderers that were disabled earlier
                Renderer[] doorRenderers = enteredHallway.startDoor.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in doorRenderers)
                {
                    renderer.enabled = true;
                }
                // Re-enable colliders
                Collider[] doorColliders = enteredHallway.startDoor.GetComponentsInChildren<Collider>(true);
                foreach (var collider in doorColliders)
                {
                    collider.enabled = true;
                }
            }
        }

        // NOW destroy the old hallways after enabling the new door
        foreach (var hallway in hallwaysToRemove)
        {
            if (hallway != null)
            {
                Debug.Log($"Destroying old hallway: {hallway.hallwayNumber}");
                if (hallway == resetHallway) resetHallway = null;
                if (hallway == correctNextHallway) correctNextHallway = null;
                activeHallways.Remove(hallway);
                Destroy(hallway.gameObject);
            }
        }

        // Spawn next hallways AFTER everything is cleaned up and player has moved forward
        Vector3 nextPosition = enteredHallway.transform.position + new Vector3(0, 0, hallwayLength);
        bool nextHasChanges = Random.value < anomalyChance;

        if (isReset)
        {
            // Spawning from reset position (hallway 0)
            correctNextHallway = SpawnHallway(1, nextPosition, nextHasChanges, false);
            correctNextHallway.SetAsActiveHallway(false);
            correctNextHallway.gameObject.SetActive(true);

            resetHallway = SpawnHallway(0, nextPosition, false, false);
            resetHallway.SetAsActiveHallway(false);
            if (resetHallway.startDoor != null)
            {
                resetHallway.startDoor.gameObject.SetActive(false);
                resetHallway.startDoor.isStartDoor = true; // Not interactable
                resetHallway.startDoor.SetDoorState(false, true); // Closed
            }
            if (resetHallway.endDoor != null)
            {
                resetHallway.endDoor.SetDoorState(false, true); // Ensure end door is closed
            }
            resetHallway.gameObject.SetActive(false);
        }
        else
        {
            // Normal progression - just spawn next hallway and reset
            // But NOT if we're on the final hallway (lift/reset already spawned at lift position)
            if (currentHallwayNumber != totalHallways - 1)
            {
                int nextHallwayNum = currentHallwayNumber + 1;

                correctNextHallway = SpawnHallway(nextHallwayNum, nextPosition, nextHasChanges, false);
                correctNextHallway.SetAsActiveHallway(false);
                correctNextHallway.gameObject.SetActive(true);

                resetHallway = SpawnHallway(0, nextPosition, false, false);
                resetHallway.SetAsActiveHallway(false);
                if (resetHallway.startDoor != null)
                {
                    resetHallway.startDoor.gameObject.SetActive(false);
                    resetHallway.startDoor.isStartDoor = true;
                    resetHallway.startDoor.SetDoorState(false, true);
                }
                if (resetHallway.endDoor != null)
                {
                    resetHallway.endDoor.SetDoorState(false, true); // Ensure end door is closed
                }
                resetHallway.gameObject.SetActive(false);
            }
            // If on final hallway, lift and reset are already spawned at correct position
        }

        currentState = GameState.Playing;
    }

    void SpawnLiftHallway()
    {
        if (liftHallwayPrefab == null)
        {
            Debug.LogWarning("Lift Hallway Prefab not assigned!");
            return;
        }

        if (currentHallway == null)
        {
            Debug.LogWarning("Cannot spawn lift - current hallway is null!");
            return;
        }

        // Calculate position after the final hallway
        Vector3 liftPosition = currentHallway.transform.position + new Vector3(0, 0, hallwayLength);
        Quaternion liftRotation = Quaternion.Euler(0, 180, 0);

        liftHallway = Instantiate(liftHallwayPrefab, liftPosition, liftRotation);
        liftHallway.name = "Lift_Hallway";
        liftHallway.SetActive(true); // Start ENABLED (like correctNextHallway)

        Debug.Log($"Spawned lift hallway (ENABLED) at position {liftPosition} (after final hallway)");
    }


}
