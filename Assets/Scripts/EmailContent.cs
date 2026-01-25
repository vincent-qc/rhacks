using UnityEngine;
using TMPro;

public class EmailContent : MonoBehaviour
{
    public string subject;
    public string body;
    public string sender;
    public string summary;
    public EmailAITool.EmailCategory category;
    public int priority;

    [SerializeField] private TextMeshProUGUI fullContent;
  [SerializeField] private TextMeshProUGUI fullSenderText;
  [SerializeField] private TextMeshProUGUI fullTitleText;

  [SerializeField] private TextMeshProUGUI aiContent;
  [SerializeField] private TextMeshProUGUI aiSenderText;
  [SerializeField] private TextMeshProUGUI aiTitleText;

  public void Initialize(string sender, string subject, string body)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;

        UpdateText();
    }

    public void Initialize(string sender, string subject, string body, AudioClip audioClip)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;

        EmailAudio emailAudio = GetComponent<EmailAudio>();
        if (emailAudio == null) emailAudio = gameObject.AddComponent<EmailAudio>();
        emailAudio.SetAudioClip(audioClip);

        UpdateText();
    }

    public void Initialize(string sender, string subject, string body, string summary, AudioClip audioClip, EmailAITool.EmailCategory category, int priority)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;
        this.summary = summary;
        this.category = category;
        this.priority = priority;

        EmailAudio emailAudio = GetComponent<EmailAudio>();
        if (emailAudio == null) emailAudio = gameObject.AddComponent<EmailAudio>();
        emailAudio.SetAudioClip(audioClip);

        UpdateText();
    }

    private void UpdateText()
    {
      aiSenderText.text = "From: " + sender;
      aiTitleText.text = subject;
      aiContent.text = summary;

      fullSenderText.text = "From: " + sender;
      fullTitleText.text = subject;
      fullContent.text = body;
    }
}
