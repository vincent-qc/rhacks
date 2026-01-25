using UnityEngine;

public class EmailManager : MonoBehaviour
{
    [SerializeField] public GameObject emailSpherePrefab;
    [SerializeField] private float spawnDistance = 2.0f;
    [SerializeField] private AudioClip newEmailSoundEffect;
    
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AddEmail(GCloudPubSubManager.EmailData email)
    {
        // Play new email sound effect
        if (newEmailSoundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(newEmailSoundEffect);
        }
        
        if (emailSpherePrefab == null) return;

        EmailAITool.AnalyzeEmail(email.from, email.subject, email.snippet, (analysis) =>
        {
            string summary = analysis?.summary ?? email.snippet;

            Audio.GenerateAudio(summary, (audioClip) =>
            {
                Vector3 spawnPos = GeneratePosition();
                GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);

                if (Camera.main != null)
                {
                    newSphere.transform.LookAt(Camera.main.transform);
                    newSphere.transform.Rotate(0, 180, 0);
                }

                EmailSphere sphere = newSphere.GetComponent<EmailSphere>();
                if (sphere != null)
                {
                    sphere.Initialize(email.from, email.subject, summary, audioClip);
                }
            });
        });
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
