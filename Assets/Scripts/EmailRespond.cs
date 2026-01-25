using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class EmailRespond : MonoBehaviour
{
    [Header("Email Canvas Integration")]
    [SerializeField] private EmailCanvas emailCanvas;
    [SerializeField] private EmailContent emailContentRef;

    [Header("UI States")]
    [SerializeField] private GameObject DetectingUI;
    [SerializeField] private GameObject GeneratingUI;
    [SerializeField] private GameObject DisplayingUI;

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private TextMeshProUGUI generatedEmailText;

    private TextMeshProUGUI emailContent;

    private SpeechRecognition speechRecognition;
    private bool isInitialized = false;
    private bool hasStartedReplyWorkflow = false;
    private bool isRecording = false;
    private bool isGenerating = false;
    private bool isDisplayingDraft = false;
    private string currentDraftEmail = "";

    public bool IsDisplayingDraft => isDisplayingDraft;

    private IEnumerator Start()
    {
        yield return null;

        speechRecognition = FindFirstObjectByType<SpeechRecognition>();
        if (speechRecognition == null)
        {
            GameObject go = new GameObject("SpeechRecognition");
            speechRecognition = go.AddComponent<SpeechRecognition>();
            yield return null;
        }

        if (speechRecognition.OnRecordingStarted != null)
            speechRecognition.OnRecordingStarted.AddListener(OnRecordingStarted);
        if (speechRecognition.OnRecordingStopped != null)
            speechRecognition.OnRecordingStopped.AddListener(OnRecordingStopped);
        if (speechRecognition.OnTranscriptionComplete != null)
            speechRecognition.OnTranscriptionComplete.AddListener(OnTranscriptionComplete);
        if (speechRecognition.OnError != null)
            speechRecognition.OnError.AddListener(OnError);

        HideAllUI();

        if (DisplayingUI != null)
        {
             Transform contentTransform = DisplayingUI.transform.Find("content");
             if(contentTransform != null) emailContent = contentTransform.GetComponent<TextMeshProUGUI>();
        }

        if (emailCanvas == null)
            emailCanvas = GetComponentInParent<EmailCanvas>();

        if (emailContentRef == null)
            emailContentRef = GetComponentInParent<EmailContent>();


        isInitialized = true;
        Debug.Log("[EmailRespond] Ready.");
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (emailCanvas != null)
        {
            if (emailCanvas.replying && !hasStartedReplyWorkflow)
            {
                StartReplyWorkflow();
            }
            else if (!emailCanvas.replying && hasStartedReplyWorkflow && !isGenerating)
            {
                 hasStartedReplyWorkflow = false;
                 HideAllUI();
                 if (isRecording) SpeechRecognition.StopRecordingAndTranscribe(); 
            }
        }

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.yKey.wasPressedThisFrame) SpeechRecognition.StartRecording();
            if (keyboard.uKey.wasPressedThisFrame) SpeechRecognition.StopRecordingAndTranscribe();
        }
    }

    public void StopRecording()
    {
        if (isRecording)
        {
            SpeechRecognition.StopRecordingAndTranscribe();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Collider")
        {
            StopRecording();
        }
    }

    private void StartReplyWorkflow()
    {
        hasStartedReplyWorkflow = true;
        Debug.Log("[EmailRespond] Reply activated - Starting recording immediately...");
        
        SpeechRecognition.StartRecording();
    }

    private void OnRecordingStarted()
    {
        isRecording = true;
        ShowDetectingUI(); 
        Debug.Log("[EmailRespond] Detecting speech...");
    }

    private void OnRecordingStopped()
    {
        isRecording = false;
        isGenerating = true;
        ShowGeneratingUI();
        Debug.Log("[EmailRespond] Generating email response...");
    }

    private void OnTranscriptionComplete(string text)
    {
        UpdateTranscription(text);

        string recipient = "";
        if (emailContentRef != null)
            recipient = emailContentRef.sender;
        
        GenerateEmail.Generate(text, recipient, OnEmailGenerated);
    }

    private void OnEmailGenerated(string generatedEmail)
    {
        isGenerating = false;
        if (!string.IsNullOrEmpty(generatedEmail))
        {
            isDisplayingDraft = true;
            currentDraftEmail = generatedEmail;
            ShowDisplayingUI();
            UpdateGeneratedEmail(generatedEmail);
            Debug.Log($"[EmailRespond] Email displayed:\n{generatedEmail}");
        }
        else
        {
            Debug.LogError("[EmailRespond] Failed to generate email");
            if (emailCanvas != null) emailCanvas.replying = false;
        }
    }

    public void SendDraft(GCloudPubSubManager pubSubManager)
    {
        if (!isDisplayingDraft || string.IsNullOrEmpty(currentDraftEmail))
        {
            Debug.LogWarning("[EmailRespond] No draft to send");
            return;
        }

        if (emailContentRef == null)
        {
            Debug.LogError("[EmailRespond] No email content reference - cannot get recipient");
            return;
        }

        string recipientEmail = emailContentRef.senderEmail;
        string subject = "Re: " + emailContentRef.subject;

        Debug.Log($"[EmailRespond] Sending email to: {recipientEmail}");
        
        pubSubManager.SendEmail(recipientEmail, subject, currentDraftEmail, (success, message) =>
        {
            if (success)
            {
                Debug.Log("[EmailRespond] Email sent successfully!");
                DiscardDraft();
            }
            else
            {
                Debug.LogError($"[EmailRespond] Failed to send email: {message}");
            }
        });
    }

    public void DiscardDraft()
    {
        Debug.Log("[EmailRespond] Discarding draft");
        isDisplayingDraft = false;
        currentDraftEmail = "";
        hasStartedReplyWorkflow = false;
        HideAllUI();
        if (emailCanvas != null) emailCanvas.replying = false;
    }

    private void OnError(string error)
    {
        isRecording = false;
        isGenerating = false;
        HideAllUI();
        Debug.LogError($"[EmailRespond] Error: {error}");
        hasStartedReplyWorkflow = false;
        if (emailCanvas != null) emailCanvas.replying = false;
    }

    private void HideAllUI()
    {
        if (DetectingUI != null) DetectingUI.SetActive(false);
        if (GeneratingUI != null) GeneratingUI.SetActive(false);
        if (DisplayingUI != null) DisplayingUI.SetActive(false);
    }

    private void ShowDetectingUI()
    {
        SetActiveUI(DetectingUI);
    }

    private void ShowGeneratingUI()
    {
        SetActiveUI(GeneratingUI);
    }

    private void ShowDisplayingUI()
    {
        SetActiveUI(DisplayingUI);
    }

    private void SetActiveUI(GameObject activeUI)
    {
        if (DetectingUI != null) DetectingUI.SetActive(DetectingUI == activeUI);
        if (GeneratingUI != null) GeneratingUI.SetActive(GeneratingUI == activeUI);
        if (DisplayingUI != null) DisplayingUI.SetActive(DisplayingUI == activeUI);
    }

    private void UpdateTranscription(string text)
    {
        if (transcriptionText != null)
        {
            transcriptionText.text = $"You said: \"{text}\"";
        }
    }

    private void UpdateGeneratedEmail(string email)
    {
        if (generatedEmailText != null)
        {
            generatedEmailText.text = email;
        }
        
        if (emailContent != null)
        {
            emailContent.text = email;
        }
    }

    private void OnDestroy()
    {
        if (speechRecognition != null)
        {
            speechRecognition.OnRecordingStarted.RemoveListener(OnRecordingStarted);
            speechRecognition.OnRecordingStopped.RemoveListener(OnRecordingStopped);
            speechRecognition.OnTranscriptionComplete.RemoveListener(OnTranscriptionComplete);
            speechRecognition.OnError.RemoveListener(OnError);
        }
    }
}