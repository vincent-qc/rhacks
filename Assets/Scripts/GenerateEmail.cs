using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AI-powered email generator that converts brief text into full professional emails.
/// Uses Google Gemini API.
/// </summary>
public class GenerateEmail : MonoBehaviour
{
    private static GenerateEmail instance;

    [SerializeField] private string apiKey = "AIzaSyBkQjOaGwrD5qv5d5xi90pYT5JhHNU0PLg";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static void EnsureInstance()
    {
        if (instance == null)
        {
            var go = new GameObject("GenerateEmail");
            go.AddComponent<GenerateEmail>();
        }
    }

    /// <summary>
    /// Generate a full email from brief text.
    /// </summary>
    public static void Generate(string briefText, string recipient, Action<string> callback = null)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.GenerateEmailInternal(recipient, "Vincent", briefText, callback));
    }

    private IEnumerator GenerateEmailInternal(string senderName, string userName, string briefText, Action<string> callback)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[GenerateEmail] API Key not configured");
            callback?.Invoke(null);
            yield break;
        }

        string prompt = $@"Convert the following brief text into a complete, professional email with proper formatting.

Brief text: {briefText}

Generate a complete email with:
- Appropriate greeting (e.g., ""Hi {senderName},"" or ""Dear {senderName},"")
- Well-structured body paragraphs
- Professional closing signature with this format:

Best regards,
{userName}

Make it natural and professional. Return ONLY the email body (no subject line).";

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        GeminiRequest requestBody = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    parts = new GeminiPart[] { new GeminiPart { text = prompt } }
                }
            },
            generationConfig = new GeminiGenerationConfig
            {
                temperature = 0.7f,
                maxOutputTokens = 1000
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
                string generatedEmail = ParseGeminiResponse(responseText);
                callback?.Invoke(generatedEmail);
            }
            else
            {
                Debug.LogError($"[GenerateEmail] API error: {request.responseCode}");
                callback?.Invoke(null);
            }
        }
    }

    private string ParseGeminiResponse(string json)
    {
        try
        {
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
            if (response.candidates != null && response.candidates.Length > 0)
            {
                var candidate = response.candidates[0];
                if (candidate.content != null && candidate.content.parts != null && candidate.content.parts.Length > 0)
                {
                    return candidate.content.parts[0].text.Trim();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GenerateEmail] Error parsing response: {e.Message}");
        }
        return null;
    }

    #region Gemini API Data Classes

    [Serializable]
    private class GeminiRequest
    {
        public GeminiContent[] contents;
        public GeminiGenerationConfig generationConfig;
    }

    [Serializable]
    private class GeminiContent
    {
        public GeminiPart[] parts;
    }

    [Serializable]
    private class GeminiPart
    {
        public string text;
    }

    [Serializable]
    private class GeminiGenerationConfig
    {
        public float temperature;
        public int maxOutputTokens;
    }

    [Serializable]
    private class GeminiResponse
    {
        public GeminiCandidate[] candidates;
    }

    [Serializable]
    private class GeminiCandidate
    {
        public GeminiContent content;
    }

    #endregion
}
