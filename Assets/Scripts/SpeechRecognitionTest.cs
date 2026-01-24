using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Test script for SpeechRecognition.
/// Press Y to start recording, U to stop and transcribe.
/// </summary>
public class SpeechRecognitionTest : MonoBehaviour
{
    [Header("UI References (Optional)")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI transcriptionText;

    private SpeechRecognition speechRecognition;
    private bool isInitialized = false;

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
            // Wait another frame for Awake to complete
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
        UpdateStatus("Ready. Press Y to start recording, U to stop.");
        Debug.Log("[SpeechTest] Ready. Press Y to start recording, U to stop.");
    }

    private void Update()
    {
        if (!isInitialized) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Start recording on Y
        if (keyboard.yKey.wasPressedThisFrame)
        {
            Debug.Log("[SpeechTest] Y pressed - Starting recording...");
            SpeechRecognition.StartRecording();
        }

        // Stop recording on U
        if (keyboard.uKey.wasPressedThisFrame)
        {
            Debug.Log("[SpeechTest] U pressed - Stopping recording...");
            SpeechRecognition.StopRecordingAndTranscribe();
        }
    }

    private void OnRecordingStarted()
    {
        UpdateStatus("Recording... Press U to stop.");
        Debug.Log("[SpeechTest] Recording started!");
    }

    private void OnRecordingStopped()
    {
        UpdateStatus("Processing...");
        Debug.Log("[SpeechTest] Recording stopped, processing...");
    }

    private void OnTranscriptionComplete(string text)
    {
        UpdateStatus("Ready. Press Y to start recording, U to stop.");
        UpdateTranscription(text);
        Debug.Log($"[SpeechTest] Transcription: {text}");
    }

    private void OnError(string error)
    {
        UpdateStatus($"Error: {error}");
        Debug.LogError($"[SpeechTest] Error: {error}");
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
        {
            statusText.text = status;
        }
    }

    private void UpdateTranscription(string text)
    {
        if (transcriptionText != null)
        {
            transcriptionText.text = $"You said: \"{text}\"";
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
