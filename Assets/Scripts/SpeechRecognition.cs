using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Speech-to-Text using Google Cloud Speech API.
/// Records audio from the microphone and transcribes it to text.
/// Uses API Key authentication (simpler than OAuth).
/// </summary>
public class SpeechRecognition : MonoBehaviour
{
    private static SpeechRecognition instance;

    [Header("Google Cloud Settings")]
    [Tooltip("Your Google Cloud API Key. Get it from: console.cloud.google.com/apis/credentials")]
    [SerializeField] private string apiKey = "AIzaSyBkQjOaGwrD5qv5d5xi90pYT5JhHNU0PLg";

    [Header("Recording Settings")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingSeconds = 30;
    [SerializeField] private string languageCode = "en-US";

    [Header("Events")]
    public UnityEvent<string> OnTranscriptionComplete = new UnityEvent<string>();
    public UnityEvent<string> OnError = new UnityEvent<string>();
    public UnityEvent OnRecordingStarted = new UnityEvent();
    public UnityEvent OnRecordingStopped = new UnityEvent();

    // State
    private bool _isRecording = false;
    private AudioClip _recordingClip;
    private string _microphoneDevice;

    public bool IsRecording => _isRecording;
    public bool IsReady => !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";

    #region Singleton & Initialization

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMicrophone();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[SpeechRecognition] API Key not set! Please set your Google Cloud API key in the Inspector or via SetApiKey()");
        }
        else
        {
            Debug.Log("[SpeechRecognition] Ready with API Key authentication.");
        }
    }

    private static void EnsureInstance()
    {
        if (instance == null)
        {
            var go = new GameObject("SpeechRecognition");
            go.AddComponent<SpeechRecognition>();
        }
    }

    private void InitializeMicrophone()
    {
        string[] devices = Microphone.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("[SpeechRecognition] No microphone devices found!");
            return;
        }

        _microphoneDevice = devices[0];
        Debug.Log($"[SpeechRecognition] Using microphone: {_microphoneDevice}");

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[SpeechRecognition] Available mic [{i}]: {devices[i]}");
        }
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Set the Google Cloud API key at runtime.
    /// </summary>
    public static void SetApiKey(string key)
    {
        EnsureInstance();
        instance.apiKey = key;
        Debug.Log("[SpeechRecognition] API Key updated.");
    }

    /// <summary>
    /// Start recording audio from the microphone.
    /// </summary>
    public static void StartRecording()
    {
        EnsureInstance();
        instance.StartRecordingInternal();
    }

    /// <summary>
    /// Stop recording and transcribe the audio.
    /// Returns the transcribed text via the OnTranscriptionComplete event.
    /// </summary>
    public static void StopRecordingAndTranscribe()
    {
        EnsureInstance();
        instance.StopRecordingAndTranscribeInternal();
    }

    /// <summary>
    /// Stop recording and transcribe the audio with a callback.
    /// </summary>
    public static void StopRecordingAndTranscribe(Action<string> callback)
    {
        EnsureInstance();
        instance.StopRecordingAndTranscribeInternal(callback);
    }

    /// <summary>
    /// Cancel the current recording without transcribing.
    /// </summary>
    public static void CancelRecording()
    {
        EnsureInstance();
        instance.CancelRecordingInternal();
    }

    /// <summary>
    /// Check if currently recording.
    /// </summary>
    public static bool IsCurrentlyRecording()
    {
        return instance != null && instance._isRecording;
    }

    #endregion

    #region Recording Implementation

    private void StartRecordingInternal()
    {
        if (_isRecording)
        {
            Debug.LogWarning("[SpeechRecognition] Already recording!");
            return;
        }

        if (string.IsNullOrEmpty(_microphoneDevice))
        {
            Debug.LogError("[SpeechRecognition] No microphone device available!");
            OnError?.Invoke("No microphone device available");
            return;
        }

        if (!IsReady)
        {
            Debug.LogError("[SpeechRecognition] API Key not configured!");
            OnError?.Invoke("API Key not configured");
            return;
        }

        _recordingClip = Microphone.Start(_microphoneDevice, false, maxRecordingSeconds, sampleRate);
        _isRecording = true;

        Debug.Log("[SpeechRecognition] Recording started...");
        OnRecordingStarted?.Invoke();
    }

    private void StopRecordingAndTranscribeInternal(Action<string> callback = null)
    {
        if (!_isRecording)
        {
            Debug.LogWarning("[SpeechRecognition] Not currently recording!");
            callback?.Invoke(null);
            return;
        }

        int recordingPosition = Microphone.GetPosition(_microphoneDevice);
        Microphone.End(_microphoneDevice);
        _isRecording = false;

        Debug.Log($"[SpeechRecognition] Recording stopped. Samples recorded: {recordingPosition}");
        OnRecordingStopped?.Invoke();

        if (recordingPosition == 0)
        {
            Debug.LogWarning("[SpeechRecognition] No audio recorded!");
            OnError?.Invoke("No audio recorded");
            callback?.Invoke(null);
            return;
        }

        AudioClip trimmedClip = TrimAudioClip(_recordingClip, recordingPosition);
        StartCoroutine(TranscribeAudio(trimmedClip, callback));
    }

    private void CancelRecordingInternal()
    {
        if (!_isRecording) return;

        Microphone.End(_microphoneDevice);
        _isRecording = false;
        _recordingClip = null;

        Debug.Log("[SpeechRecognition] Recording cancelled.");
        OnRecordingStopped?.Invoke();
    }

    private AudioClip TrimAudioClip(AudioClip clip, int samples)
    {
        float[] data = new float[samples * clip.channels];
        clip.GetData(data, 0);

        AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", samples, clip.channels, clip.frequency, false);
        trimmedClip.SetData(data, 0);

        return trimmedClip;
    }

    #endregion

    #region Transcription

    private IEnumerator TranscribeAudio(AudioClip clip, Action<string> callback = null)
    {
        Debug.Log("[SpeechRecognition] Converting audio to WAV...");

        byte[] wavBytes = WavUtility.FromAudioClip(clip);
        string audioBase64 = Convert.ToBase64String(wavBytes);

        Debug.Log($"[SpeechRecognition] Sending {wavBytes.Length} bytes to Google Speech API...");

        // Use API key in URL instead of Bearer token
        string url = $"https://speech.googleapis.com/v1/speech:recognize?key={apiKey}";

        SpeechRecognitionRequest requestBody = new SpeechRecognitionRequest
        {
            config = new RecognitionConfig
            {
                encoding = "LINEAR16",
                sampleRateHertz = clip.frequency,
                languageCode = languageCode,
                enableAutomaticPunctuation = true,
                model = "default"
            },
            audio = new RecognitionAudio
            {
                content = audioBase64
            }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"[SpeechRecognition] Response: {responseText}");

                string transcription = ParseTranscriptionResponse(responseText);

                if (!string.IsNullOrEmpty(transcription))
                {
                    Debug.Log($"[SpeechRecognition] Transcription: {transcription}");
                    OnTranscriptionComplete?.Invoke(transcription);
                    callback?.Invoke(transcription);
                }
                else
                {
                    Debug.LogWarning("[SpeechRecognition] No transcription returned (possibly silence or unclear audio)");
                    OnError?.Invoke("No speech detected");
                    callback?.Invoke(null);
                }
            }
            else
            {
                string error = $"Speech API error: {request.responseCode} - {request.downloadHandler.text}";
                Debug.LogError($"[SpeechRecognition] {error}");
                OnError?.Invoke(error);
                callback?.Invoke(null);
            }
        }
    }

    private string ParseTranscriptionResponse(string json)
    {
        try
        {
            SpeechRecognitionResponse response = JsonUtility.FromJson<SpeechRecognitionResponse>(json);

            if (response.results != null && response.results.Length > 0)
            {
                if (response.results[0].alternatives != null && response.results[0].alternatives.Length > 0)
                {
                    return response.results[0].alternatives[0].transcript;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeechRecognition] Error parsing response: {e.Message}");
        }

        return null;
    }

    #endregion

    #region API Data Classes

    [Serializable]
    private class SpeechRecognitionRequest
    {
        public RecognitionConfig config;
        public RecognitionAudio audio;
    }

    [Serializable]
    private class RecognitionConfig
    {
        public string encoding;
        public int sampleRateHertz;
        public string languageCode;
        public bool enableAutomaticPunctuation;
        public string model;
    }

    [Serializable]
    private class RecognitionAudio
    {
        public string content;
    }

    [Serializable]
    private class SpeechRecognitionResponse
    {
        public SpeechRecognitionResult[] results;
    }

    [Serializable]
    private class SpeechRecognitionResult
    {
        public SpeechRecognitionAlternative[] alternatives;
    }

    [Serializable]
    private class SpeechRecognitionAlternative
    {
        public string transcript;
        public float confidence;
    }

    #endregion
}
