using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Email response workflow controller.
/// Manages UI states: Selecting -> Detecting -> Generating -> Displaying
/// Press Y to start recording email response, U to stop and generate.
/// </summary>
public class EmailRespond : MonoBehaviour
{
    [Header("UI States")]
    [SerializeField] private GameObject SelectingUI;
    [SerializeField] private GameObject DetectingUI;
    [SerializeField] private GameObject GeneratingUI;
    [SerializeField] private GameObject DisplayingUI;

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private TextMeshProUGUI generatedEmailText;

    private SpeechRecognition speechRecognition;
    private bool isInitialized = false;
    private string currentTranscription = "";

    private IEnumerator Start()
    {
        // Wait a frame to ensure SpeechRecognition has initialized
        yield return null;

        // Get or create SpeechRecognition instance
        speechRecognition = FindFirstObjectByType<SpeechRecognition>();
        if (speechRecognition == null)
        {
            GameObject go = new GameObject("SpeechRecognition");
            speechRecognition = go.AddComponent<SpeechRecognition>();
            yield return null;
        }

        // Subscribe to events
        if (speechRecognition.OnRecordingStarted != null)
            speechRecognition.OnRecordingStarted.AddListener(OnRecordingStarted);
        if (speechRecognition.OnRecordingStopped != null)
            speechRecognition.OnRecordingStopped.AddListener(OnRecordingStopped);
        if (speechRecognition.OnTranscriptionComplete != null)
            speechRecognition.OnTranscriptionComplete.AddListener(OnTranscriptionComplete);
        if (speechRecognition.OnError != null)
            speechRecognition.OnError.AddListener(OnError);

        isInitialized = true;
        ShowSelectingUI();
        Debug.Log("[EmailRespond] Ready. Press Y to start recording, U to stop.");
    }

    private void Update()
    {
        if (!isInitialized) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Start recording on Y
        if (keyboard.yKey.wasPressedThisFrame)
        {
            Debug.Log("[EmailRespond] Y pressed - Starting recording...");
            SpeechRecognition.StartRecording();
        }

        // Stop recording on U
        if (keyboard.uKey.wasPressedThisFrame)
        {
            Debug.Log("[EmailRespond] U pressed - Stopping recording...");
            SpeechRecognition.StopRecordingAndTranscribe();
        }
    }

    private void OnRecordingStarted()
    {
        ShowDetectingUI();
        Debug.Log("[EmailRespond] Detecting speech...");
    }

    private void OnRecordingStopped()
    {
        ShowGeneratingUI();
        Debug.Log("[EmailRespond] Generating email response...");
    }

    private void OnTranscriptionComplete(string text)
    {
        currentTranscription = text;
        UpdateTranscription(text);
        
        // Generate email will be called automatically by SpeechRecognition
        // But we'll also listen for the result here
        GenerateEmail.Generate(text, OnEmailGenerated);
    }

    private void OnEmailGenerated(string generatedEmail)
    {
        if (!string.IsNullOrEmpty(generatedEmail))
        {
            ShowDisplayingUI();
            UpdateGeneratedEmail(generatedEmail);
            Debug.Log($"[EmailRespond] Email displayed:\n{generatedEmail}");
        }
        else
        {
            ShowSelectingUI();
            Debug.LogError("[EmailRespond] Failed to generate email");
        }
    }

    private void OnError(string error)
    {
        ShowSelectingUI();
        Debug.LogError($"[EmailRespond] Error: {error}");
    }

    private void ShowSelectingUI()
    {
        SetActiveUI(SelectingUI);
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
        if (SelectingUI != null) SelectingUI.SetActive(SelectingUI == activeUI);
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
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (speechRecognition != null)
        {
            speechRecognition.OnRecordingStarted.RemoveListener(OnRecordingStarted);
            speechRecognition.OnRecordingStopped.RemoveListener(OnRecordingStopped);
            speechRecognition.OnTranscriptionComplete.RemoveListener(OnTranscriptionComplete);
            speechRecognition.OnError.RemoveListener(OnError);
        }
    }
}