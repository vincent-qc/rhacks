using UnityEngine;

public class EmailManager : MonoBehaviour
{
    [SerializeField] public GameObject emailSpherePrefab;
    [SerializeField] private float spawnDistance = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddEmail(GCloudPubSubManager.EmailData email)
    {
        if (emailSpherePrefab != null)
        {
            Vector3 spawnPos = GeneratePosition();
            GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);

            // Make it look at the player so text is visible
            if (Camera.main != null)
            {
                newSphere.transform.LookAt(Camera.main.transform);
                // TextMeshPro usually looks backwards if just LookAt is used, but for a GameObject/Sphere it might be fine. 
                // If it's a Canvas in WorldSpace, it might need to simply face the camera. 
                // For now, simple LookAt.
                newSphere.transform.Rotate(0, 180, 0); // Flip to face camera if it's a canvas
            }

            EmailSphere sphere = newSphere.GetComponent<EmailSphere>();
            if (sphere != null)
            {
                sphere.Initialize(email.from, email.subject, email.snippet);
            }
        }
    }

    private Vector3 GeneratePosition()
    {
        if (Camera.main == null) return transform.position;

        Transform camTransform = Camera.main.transform;
        
        // Random offsets to keep it in FOV but not dead center
        float xOffset = UnityEngine.Random.Range(-0.5f, 0.5f); 
        float yOffset = UnityEngine.Random.Range(-0.2f, 0.2f); // Keep height variation smaller

        Vector3 randomOffset = (camTransform.right * xOffset) + (camTransform.up * yOffset);
        
        return camTransform.position + (camTransform.forward * spawnDistance) + randomOffset;
    }
}
