using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class Audio : MonoBehaviour
{
    private static Audio instance;
    private AudioSource audioSource;

    // Set your Eleven Labs API key here
    private const string ELEVEN_LABS_API_KEY = "your_api_key_here";
    private const string ELEVEN_LABS_VOICE_ID = "21m00Tcm4TlvDq8ikWAM"; // Rachel voice (default)

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void PlaySound(string aiSummary)
    {
        if (instance != null)
        {
            instance.StartCoroutine(instance.GenerateAndPlaySound(aiSummary));
        }
        else
        {
            Debug.LogError("Audio instance not found. Make sure Audio component is in the scene.");
        }
    }

    private IEnumerator GenerateAndPlaySound(string text)
    {
        Debug.Log("Generating audio for: " + text);

        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{ELEVEN_LABS_VOICE_ID}";

        // Create JSON request body
        string jsonBody = JsonUtility.ToJson(new ElevenLabsRequest
        {
            text = text,
            model_id = "eleven_monolingual_v1",
            voice_settings = new VoiceSettings
            {
                stability = 0.5f,
                similarity_boost = 0.75f
            }
        });

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("xi-api-key", ELEVEN_LABS_API_KEY);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error generating audio: " + www.error);
                Debug.LogError("Response: " + www.downloadHandler.text);
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

                if (audioClip != null)
                {
                    Debug.Log("Audio generated successfully. Playing now...");
                    audioSource.clip = audioClip;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogError("Failed to create audio clip from response");
                }
            }
        }
    }

    [Serializable]
    private class ElevenLabsRequest
    {
        public string text;
        public string model_id;
        public VoiceSettings voice_settings;
    }

    [Serializable]
    private class VoiceSettings
    {
        public float stability;
        public float similarity_boost;
    }
}
