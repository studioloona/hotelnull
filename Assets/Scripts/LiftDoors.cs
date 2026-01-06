using System.Collections;
using UnityEngine;

public class LiftDoors : MonoBehaviour
{
    [Header("Door References")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Door Settings")]
    public Vector3 leftDoorClosedOffset = new Vector3(1f, 0, 0); // Offset from open position when closed
    public Vector3 rightDoorClosedOffset = new Vector3(-1f, 0, 0); // Offset from open position when closed
    public float closeDuration = 2f; // How long it takes to close
    public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioSource doorAudioSource;
    public AudioClip doorCloseSound;

    private Vector3 leftDoorClosedPosition;
    private Vector3 rightDoorClosedPosition;
    private Vector3 leftDoorOpenPosition;
    private Vector3 rightDoorOpenPosition;
    private bool doorsOpen = true;

    void Start()
    {
        if (leftDoor != null && rightDoor != null)
        {
            // Doors start open - store their current positions as open positions
            leftDoorOpenPosition = leftDoor.localPosition;
            rightDoorOpenPosition = rightDoor.localPosition;

            // Calculate closed positions using the offset values
            leftDoorClosedPosition = leftDoorOpenPosition + leftDoorClosedOffset;
            rightDoorClosedPosition = rightDoorOpenPosition + rightDoorClosedOffset;

            doorsOpen = true;
        }
        else
        {
            Debug.LogWarning("Left or Right door not assigned to LiftDoors!");
        }
    }

    public void OpenDoorsImmediate()
    {
        if (leftDoor != null && rightDoor != null)
        {
            leftDoor.localPosition = leftDoorOpenPosition;
            rightDoor.localPosition = rightDoorOpenPosition;
            doorsOpen = true;
        }
    }

    public void CloseDoors()
    {
        if (!doorsOpen) return;

        StartCoroutine(CloseDoorsCoroutine());
    }

    IEnumerator CloseDoorsCoroutine()
    {
        doorsOpen = false;

        // Play door close sound
        if (doorAudioSource != null && doorCloseSound != null)
        {
            doorAudioSource.PlayOneShot(doorCloseSound);
        }

        float elapsed = 0f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = closeCurve.Evaluate(elapsed / closeDuration);

            if (leftDoor != null)
            {
                leftDoor.localPosition = Vector3.Lerp(leftDoorOpenPosition, leftDoorClosedPosition, t);
            }

            if (rightDoor != null)
            {
                rightDoor.localPosition = Vector3.Lerp(rightDoorOpenPosition, rightDoorClosedPosition, t);
            }

            yield return null;
        }

        // Ensure final positions
        if (leftDoor != null)
        {
            leftDoor.localPosition = leftDoorClosedPosition;
        }

        if (rightDoor != null)
        {
            rightDoor.localPosition = rightDoorClosedPosition;
        }

        Debug.Log("Lift doors closed");
    }
}
