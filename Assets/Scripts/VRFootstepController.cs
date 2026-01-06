using UnityEngine;
using UnityEngine.XR;

public class VRFootstepController : MonoBehaviour
{
    [Header("Footstep Settings")]
    [SerializeField] private float stepInterval = 0.5f; // Time between footsteps
    [SerializeField] private float movementThreshold = 0.15f; // Minimum speed to trigger footsteps (increased for VR)
    [SerializeField] private float minDistanceForStep = 0.05f; // Minimum distance moved to count as movement

    private CharacterController characterController;
    private float stepTimer = 0f;
    private Vector3 lastPosition;
    private float distanceMoved = 0f;
    private bool wasMoving = false;

    void Start()
    {
        // Try to find CharacterController in children (XR Origin structure)
        characterController = GetComponentInChildren<CharacterController>();

        if (characterController == null)
        {
            Debug.LogWarning("VRFootstepController: No CharacterController found! Add one to your XR Origin.");
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (AudioManager.Instance == null)
            return;

        // Always calculate position-based movement (more reliable for VR)
        Vector3 currentPosition = transform.position;
        Vector3 positionDelta = currentPosition - lastPosition;
        positionDelta.y = 0; // Ignore vertical movement
        float frameDistance = positionDelta.magnitude;

        // Update last position every frame
        lastPosition = currentPosition;

        // Calculate speed from position delta
        float speed = Time.deltaTime > 0 ? frameDistance / Time.deltaTime : 0f;

        // Check if player is actually moving (both speed and distance checks)
        bool isMoving = speed > movementThreshold && frameDistance > 0.001f;

        if (isMoving)
        {
            distanceMoved += frameDistance;
            stepTimer += Time.deltaTime;

            // Play footstep when enough distance has been covered OR enough time has passed
            float adjustedInterval = stepInterval / Mathf.Max(1f, speed * 0.3f);

            if (stepTimer >= adjustedInterval && distanceMoved >= minDistanceForStep)
            {
                AudioManager.Instance.PlayFootstep();
                stepTimer = 0f;
                distanceMoved = 0f;
            }

            wasMoving = true;
        }
        else
        {
            // Reset when player stops
            if (wasMoving)
            {
                stepTimer = 0f;
                distanceMoved = 0f;
                wasMoving = false;
            }
        }
    }
}
