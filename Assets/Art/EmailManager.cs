using UnityEngine;

public class EmailManager : MonoBehaviour
{
    [SerializeField] public GameObject emailSpherePrefab;
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
            GameObject newSphere = Instantiate(emailSpherePrefab);
            EmailSphere sphere = newSphere.GetComponent<EmailSphere>();
            if (sphere != null)
            {
                sphere.subject = email.subject;
                sphere.sender = email.from;
                sphere.body = email.snippet;
            }
        }
    }
}
