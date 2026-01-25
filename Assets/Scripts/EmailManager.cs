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

        // Version with ElevenLabs audio generation
        EmailAITool.AnalyzeEmail(email.from, email.subject, email.snippet, (analysis) =>
        {
            string summary = analysis?.summary ?? email.snippet;
            EmailAITool.EmailCategory category = analysis?.category ?? EmailAITool.EmailCategory.Other;
            int priority = analysis?.priority ?? 1;
        
            Audio.GenerateAudio(summary, (audioClip) =>
            {
                Vector3 spawnPos = GeneratePosition(category);
                GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);
        
                if (spawnSoundClip != null)
                {
                    AudioSource.PlayClipAtPoint(spawnSoundClip, spawnPos);
                }

                if (Camera.main != null)
                {
                    newSphere.transform.LookAt(Camera.main.transform);
                }

                string senderEmail = ExtractEmailAddress(email.from);
                string senderName = ExtractSenderName(email.from);
        
                EmailContent content = newSphere.GetComponent<EmailContent>();
                if (content != null)
                {
                    content.Initialize(senderName, senderEmail, email.subject, email.snippet, summary, audioClip, category, priority);
                }
            });
        });

        // Version without audio (commented out):
        // EmailAITool.AnalyzeEmail(email.from, email.subject, email.snippet, (analysis) =>
        // {
        //     string summary = analysis?.summary ?? email.snippet;
        //     EmailAITool.EmailCategory category = analysis?.category ?? EmailAITool.EmailCategory.Other;
        //     int priority = analysis?.priority ?? 1;

        //     Vector3 spawnPos = GeneratePosition(category);
        //     GameObject newSphere = Instantiate(emailSpherePrefab, spawnPos, Quaternion.identity);

        //     if (spawnSoundClip != null)
        //     {
        //         AudioSource.PlayClipAtPoint(spawnSoundClip, spawnPos);
        //     }

        //     if (Camera.main != null)
        //     {
        //         newSphere.transform.LookAt(Camera.main.transform);
        //     }

        //     string senderEmail = ExtractEmailAddress(email.from);
        //     string senderName = ExtractSenderName(email.from);

        //     EmailContent content = newSphere.GetComponent<EmailContent>();
        //     if (content != null)
        //     {
        //         content.Initialize(senderName, senderEmail, email.subject, email.snippet, summary, null, category, priority);
        //     }
        // });
    }

    private string ExtractEmailAddress(string fromField)
    {
        if (string.IsNullOrEmpty(fromField)) return "";
        
        int startIndex = fromField.IndexOf('<');
        int endIndex = fromField.IndexOf('>');
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return fromField.Substring(startIndex + 1, endIndex - startIndex - 1);
        }
        
        if (fromField.Contains("@"))
        {
            return fromField.Trim();
        }
        
        return fromField;
    }

    private string ExtractSenderName(string fromField)
    {
        if (string.IsNullOrEmpty(fromField)) return "";
        
        int startIndex = fromField.IndexOf('<');
        
        if (startIndex > 0)
        {
            return fromField.Substring(0, startIndex).Trim().Trim('"');
        }
        
        return fromField;
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
