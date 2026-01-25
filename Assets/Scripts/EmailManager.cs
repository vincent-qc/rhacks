using UnityEngine;

public class EmailManager : MonoBehaviour
{
    [SerializeField] public GameObject emailSpherePrefab;
    [SerializeField] private float spawnDistance = 2.0f;
    [SerializeField] private AudioClip spawnSoundClip;

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
        if (emailSpherePrefab == null) return;

        // Original version with ElevenLabs audio generation (commented out to save API costs):
        // EmailAITool.AnalyzeEmail(email.from, email.subject, email.snippet, (analysis) =>
        // {
        //     string summary = analysis?.summary ?? email.snippet;
        //
        //     Audio.GenerateAudio(summary, (audioClip) =>
        //     {
        //         Vector3 spawnPos = GeneratePosition();
        //         GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);
        //
        //         if (Camera.main != null)
        //         {
        //             newSphere.transform.LookAt(Camera.main.transform);
        //             newSphere.transform.Rotate(0, 180, 0);
        //         }
        //
        //         EmailSphere sphere = newSphere.GetComponent<EmailSphere>();
        //         if (sphere != null)
        //         {
        //             sphere.Initialize(email.from, email.subject, summary, audioClip);
        //         }
        //     });
        // });

        // Current version: AI summarization enabled, audio generation disabled
        EmailAITool.AnalyzeEmail(email.from, email.subject, email.snippet, (analysis) =>
        {
            string summary = analysis?.summary ?? email.snippet;
            EmailAITool.EmailCategory category = analysis?.category ?? EmailAITool.EmailCategory.Other;
            int priority = analysis?.priority ?? 1;

            Vector3 spawnPos = GeneratePosition(category);
            GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);

            // Play spawn sound effect
            if (spawnSoundClip != null)
            {
                AudioSource.PlayClipAtPoint(spawnSoundClip, spawnPos);
            }

            if (Camera.main != null)
            {
                newSphere.transform.LookAt(Camera.main.transform);
            }

            EmailContent content = newSphere.GetComponent<EmailContent>();
            if (content != null)
            {
                content.Initialize(email.from, email.subject, email.snippet, summary, null, category, priority);
            }
            // Ensure EmailSphere (if needed) does its own Start logic
            EmailSphere sphere = newSphere.GetComponent<EmailSphere>();
            // If there was any intialization on sphere, do it here. Currently it has none.
        });
    }

    private Vector3 GeneratePosition(EmailAITool.EmailCategory category)
    {
        if (Camera.main == null) return transform.position;

        Transform camTransform = Camera.main.transform;

        float distance = 5.0f;
        switch (category)
        {
            case EmailAITool.EmailCategory.Personal: distance = 1.5f; break;
            case EmailAITool.EmailCategory.Work: distance = 2.5f; break;
            case EmailAITool.EmailCategory.Social: distance = 3.2f; break;
            case EmailAITool.EmailCategory.Promotions: distance = 4.0f; break;
            case EmailAITool.EmailCategory.Other: default: distance = 5.0f; break;
        }

        // Random offsets to keep it in FOV but not dead center
        float xOffset = UnityEngine.Random.Range(-1.2f, 1.2f);
        float yOffset = UnityEngine.Random.Range(-0.4f, 0.4f); // Keep height variation smaller
        Vector3 randomOffset = (camTransform.right * xOffset) + (camTransform.up * yOffset);

        return camTransform.position + (camTransform.forward * distance) + randomOffset;
    }
}
