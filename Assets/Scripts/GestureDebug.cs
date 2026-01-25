using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GestureDebug : MonoBehaviour
{
    [SerializeField] private float resetLerpDuration = 0.5f;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void TestLogR() {
      UnityEngine.Debug.Log("DEBUG: RH SWIPE");
    }

    /// <summary>
    /// Resets all EmailSpheres in the scene to new positions based on priority and camera view direction.
    /// </summary>
    public void ResetField()
    {
        // Find all EmailSpheres in the scene
        EmailSphere[] allSpheres = FindObjectsByType<EmailSphere>(FindObjectsSortMode.None);
        
        if (allSpheres.Length == 0) return;

        // Get spheres with their priorities, sorted by priority (higher priority = lower number = closer)
        var spheresWithPriority = allSpheres
            .Select(sphere => new {
                Sphere = sphere,
                Content = sphere.GetComponent<EmailContent>(),
                Priority = sphere.GetComponent<EmailContent>()?.priority ?? 5
            })
            .OrderBy(x => x.Priority)
            .ToList();

        // Generate new positions for each sphere
        for (int i = 0; i < spheresWithPriority.Count; i++)
        {
            var item = spheresWithPriority[i];
            Vector3 newPosition = GeneratePositionForPriority(item.Priority, i, spheresWithPriority.Count);
            
            // Start lerp coroutine for this sphere
            StartCoroutine(LerpToPosition(item.Sphere.transform, newPosition));
        }
    }

    /// <summary>
    /// Generates a position based on priority and camera view direction.
    /// Higher priority (lower number) = closer to camera.
    /// </summary>
    private Vector3 GeneratePositionForPriority(int priority, int index, int totalCount)
    {
        if (Camera.main == null) return Vector3.zero;

        Transform camTransform = Camera.main.transform;

        // Distance based on priority (1-5 scale, 1 being highest priority)
        // Priority 1 = closest, Priority 5 = farthest
        float baseDistance = 1.5f + (priority - 1) * 0.8f;

        // Spread spheres horizontally within same priority level
        // Calculate horizontal offset based on index to avoid overlap
        float spreadAngle = 30f; // degrees between spheres
        float angleOffset = (index - (totalCount - 1) / 2f) * Mathf.Deg2Rad * spreadAngle;
        
        // Random vertical offset for natural look
        float yOffset = UnityEngine.Random.Range(-0.3f, 0.3f);

        // Calculate position in front of camera with spread
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        
        float xOffset = Mathf.Sin(angleOffset) * baseDistance * 0.5f;
        
        Vector3 position = camTransform.position 
            + (forward * baseDistance) 
            + (right * xOffset) 
            + (camTransform.up * yOffset);

        return position;
    }

    /// <summary>
    /// Smoothly lerps a transform to a target position over time.
    /// </summary>
    private IEnumerator LerpToPosition(Transform target, Vector3 targetPosition)
    {
        if (target == null) yield break;

        Vector3 startPosition = target.position;
        float elapsedTime = 0f;

        // Temporarily make kinematic if it has a rigidbody
        Rigidbody rb = target.GetComponent<Rigidbody>();
        bool wasKinematic = rb != null && rb.isKinematic;
        if (rb != null) rb.isKinematic = true;

        while (elapsedTime < resetLerpDuration)
        {
            if (target == null) yield break;
            
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / resetLerpDuration);
            target.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            yield return null;
        }

        // Ensure final position is exact
        if (target != null)
        {
            target.position = targetPosition;
            
            // Restore original kinematic state if not focused
            EmailSphere sphere = target.GetComponent<EmailSphere>();
            if (rb != null && sphere != null && !sphere.focused)
            {
                rb.isKinematic = wasKinematic;
            }
        }
    }
}
