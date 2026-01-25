using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.Collections;
using System.Text;
using System;
// using CandyCoded.Env;

public class Audio : MonoBehaviour
{
    private static Audio instance;
    private AudioSource audioSource;

    private static string ELEVEN_LABS_API_KEY;
    private static string ELEVEN_LABS_VOICE_ID;

    void Awake()
    {
        ELEVEN_LABS_API_KEY = "sk_59e17c5ce27e383d0803ee1dbc6cbd596631efae4fd1307a";
        ELEVEN_LABS_VOICE_ID = "Gfpl8Yo74Is0W6cPUWWT";
        Debug.Log($"API Key: {ELEVEN_LABS_API_KEY}");
        Debug.Log($"Voice ID: {ELEVEN_LABS_VOICE_ID}");

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
        EnsureInstance();
        instance.StartCoroutine(instance.GenerateAndPlaySound(aiSummary));
    }

    /// <summary>
    /// Generate audio from text and return the AudioClip via callback (for caching).
    /// </summary>
    public static void GenerateAudio(string text, Action<AudioClip> callback)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.GenerateAudioClip(text, callback));
    }

    /// <summary>
    /// Play an already-generated AudioClip.
    /// </summary>
    public static void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        EnsureInstance();
        instance.audioSource.clip = clip;
        instance.audioSource.Play();
    }

    /// <summary>
    /// Check if a specific AudioClip is currently playing.
    /// </summary>
    public static bool IsPlayingClip(AudioClip clip)
    {
        if (clip == null) return false;
        EnsureInstance();
        return instance.audioSource.isPlaying && instance.audioSource.clip == clip;
    }

    /// <summary>
    /// Play an AudioClip only if it's not already playing (resume-safe).
    /// </summary>
    public static void PlayClipIfNotPlaying(AudioClip clip)
    {
        if (clip == null) return;
        if (IsPlayingClip(clip)) return;
        PlayClip(clip);
    }

    private static void EnsureInstance()
    {
        if (instance == null)
        {
            var go = new GameObject("Audio");
            go.hideFlags = HideFlags.None;
            go.AddComponent<Audio>();
            // Awake will set 'instance', add AudioSource, and mark DontDestroyOnLoad
        }
    }

    private IEnumerator GenerateAndPlaySound(string text)
    {
        Debug.Log("Generating audio...");

        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{ELEVEN_LABS_VOICE_ID}";

        string jsonBody = JsonUtility.ToJson(new ElevenLabsRequest
        {
            text = text,
            model_id = "eleven_turbo_v2",
            output_format = "mp3_44100_128",
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
            www.downloadHandler = new DownloadHandlerAudioClip("", AudioType.MPEG);

            www.SetRequestHeader("xi-api-key", ELEVEN_LABS_API_KEY);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ElevenLabs Error: {www.responseCode}");
                Debug.LogError(www.downloadHandler.text);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            if (clip == null)
            {
                Debug.LogError("Failed to create AudioClip from response");
                yield break;
            }

            audioSource.clip = clip;
            Debug.Log("Playing audio...");
            audioSource.Play();
        }
    }

    private IEnumerator GenerateAudioClip(string text, Action<AudioClip> callback)
    {
        Debug.Log("Generating audio clip for caching...");

        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{ELEVEN_LABS_VOICE_ID}";

        string jsonBody = JsonUtility.ToJson(new ElevenLabsRequest
        {
            text = text,
            model_id = "eleven_turbo_v2",
            output_format = "mp3_44100_128",
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
            www.downloadHandler = new DownloadHandlerAudioClip("", AudioType.MPEG);

            www.SetRequestHeader("xi-api-key", ELEVEN_LABS_API_KEY);
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ElevenLabs Error: {www.responseCode}");
                Debug.LogError(www.downloadHandler.text);
                callback?.Invoke(null);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            
            if (clip != null)
            {
                clip.LoadAudioData();
            }

            if (clip == null)
            {
                Debug.LogError("Failed to create AudioClip from response");
                callback?.Invoke(null);
                yield break;
            }

            Debug.Log("Audio clip generated and cached.");
            callback?.Invoke(clip);
        }
    }

    [Serializable]
    private class ElevenLabsRequest
    {
        public string text;
        public string model_id;
        public string output_format;
        public VoiceSettings voice_settings;
    }

    [Serializable]
    private class VoiceSettings
    {
        public float stability;
        public float similarity_boost;
    }
}
