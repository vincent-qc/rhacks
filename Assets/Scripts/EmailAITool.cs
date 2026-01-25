using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// AI-powered email tool for summarization and categorization.
/// Uses Google Gemini API.
/// </summary>
public class EmailAITool : MonoBehaviour
{
    private static EmailAITool instance;

    [Header("Gemini API Settings")]
    [Tooltip("Your Google Cloud API Key")]
    [SerializeField] private string apiKey = "AIzaSyBkQjOaGwrD5qv5d5xi90pYT5JhHNU0PLg";
    [SerializeField] private string model = "gemini-pro";

    [Header("Events")]
    public UnityEvent<string> OnSummaryComplete = new UnityEvent<string>();
    public UnityEvent<EmailCategory> OnCategoryComplete = new UnityEvent<EmailCategory>();
    public UnityEvent<EmailAnalysis> OnAnalysisComplete = new UnityEvent<EmailAnalysis>();
    public UnityEvent<string> OnError = new UnityEvent<string>();

    public bool IsReady => !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_GEMINI_API_KEY";

    #region Email Category Enum

    public enum EmailCategory
    {
        Work,           // Work-related emails, meetings, projects
        Personal,       // Personal correspondence from friends/family
        Promotions,     // Marketing, sales, advertisements
        Social,         // Social media notifications
        Other,        // Service updates, newsletters
    }

    #endregion

    #region Email Analysis Result

    [Serializable]
    public class EmailAnalysis
    {
        public string summary;
        public EmailCategory category;
        public string categoryReason;
        public int priority; // 1-5, 5 being most urgent
    }

    #endregion

    #region Singleton

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

    private void Start()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[EmailAITool] Gemini API Key not set!");
        }
        else
        {
            Debug.Log("[EmailAITool] Ready with Gemini API.");
        }
    }

    private static void EnsureInstance()
    {
        if (instance == null)
        {
            var go = new GameObject("EmailAITool");
            go.AddComponent<EmailAITool>();
        }
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Set the Gemini API key at runtime.
    /// </summary>
    public static void SetApiKey(string key)
    {
        EnsureInstance();
        instance.apiKey = key;
    }

    /// <summary>
    /// Generate a summary of the email text.
    /// </summary>
    public static void GetSummary(string emailText, Action<string> callback = null)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.GetSummaryInternal(emailText, callback));
    }

    /// <summary>
    /// Categorize the email into a category.
    /// </summary>
    public static void GetCategory(string emailText, Action<EmailCategory> callback = null)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.GetCategoryInternal(emailText, callback));
    }

    /// <summary>
    /// Get full analysis: summary, category, and priority.
    /// </summary>
    public static void AnalyzeEmail(string emailText, Action<EmailAnalysis> callback = null)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.AnalyzeEmailInternal(emailText, callback));
    }

    /// <summary>
    /// Convenience method: Analyze email from sender, subject, and body.
    /// </summary>
    public static void AnalyzeEmail(string sender, string subject, string body, Action<EmailAnalysis> callback = null)
    {
        string emailText = $"From: {sender}\nSubject: {subject}\n\n{body}";
        AnalyzeEmail(emailText, callback);
    }

    #endregion

    #region Internal Implementation

    private IEnumerator GetSummaryInternal(string emailText, Action<string> callback)
    {
        if (!IsReady)
        {
            string error = "Gemini API Key not configured";
            Debug.LogError($"[EmailAITool] {error}");
            OnError?.Invoke(error);
            callback?.Invoke(null);
            yield break;
        }

        string prompt = $"Summarize this email in 1-2 concise sentences. Focus on the key action items or information:\n\n{emailText}";

        yield return StartCoroutine(CallGemini(prompt, (response) =>
        {
            if (response != null)
            {
                Debug.Log($"[EmailAITool] Summary: {response}");
                OnSummaryComplete?.Invoke(response);
                callback?.Invoke(response);
            }
            else
            {
                callback?.Invoke(null);
            }
        }));
    }

    private IEnumerator GetCategoryInternal(string emailText, Action<EmailCategory> callback)
    {
        if (!IsReady)
        {
            string error = "Gemini API Key not configured";
            Debug.LogError($"[EmailAITool] {error}");
            OnError?.Invoke(error);
            callback?.Invoke(EmailCategory.Other);
            yield break;
        }

        string categories = string.Join(", ", Enum.GetNames(typeof(EmailCategory)));
        string prompt = $"Categorize this email into exactly ONE of these categories: {categories}\n\nRespond with ONLY the category name, nothing else.\n\nEmail:\n{emailText}";

        yield return StartCoroutine(CallGemini(prompt, (response) =>
        {
            EmailCategory category = ParseCategory(response);
            Debug.Log($"[EmailAITool] Category: {category}");
            OnCategoryComplete?.Invoke(category);
            callback?.Invoke(category);
        }));
    }

    private IEnumerator AnalyzeEmailInternal(string emailText, Action<EmailAnalysis> callback)
    {
        if (!IsReady)
        {
            string error = "Gemini API Key not configured";
            Debug.LogError($"[EmailAITool] {error}");
            OnError?.Invoke(error);
            callback?.Invoke(null);
            yield break;
        }

        string categories = string.Join(", ", Enum.GetNames(typeof(EmailCategory)));
        string prompt = $@"Analyze this email and respond in this exact JSON format (no markdown, just raw JSON):
{{
  ""summary"": ""1-2 sentence summary"",
  ""category"": ""one of: {categories}"",
  ""categoryReason"": ""brief reason for category choice"",
  ""priority"": 1-5 number where 5 is most urgent
}}

Email:
{emailText}";

        yield return StartCoroutine(CallGemini(prompt, (response) =>
        {
            EmailAnalysis analysis = ParseAnalysis(response);
            if (analysis != null)
            {
                Debug.Log($"[EmailAITool] Analysis - Summary: {analysis.summary}, Category: {analysis.category}, Priority: {analysis.priority}");
                OnAnalysisComplete?.Invoke(analysis);
                callback?.Invoke(analysis);
            }
            else
            {
                callback?.Invoke(null);
            }
        }));
    }

    private IEnumerator CallGemini(string prompt, Action<string> callback)
    {
        // Hardcoded for debugging
        string useModel = "gemini-2.5-flash";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{useModel}:generateContent?key={apiKey}";
        Debug.Log($"[EmailAITool] Calling URL: {url}");

        // Build Gemini request body
        GeminiRequest requestBody = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    parts = new GeminiPart[]
                    {
                        new GeminiPart { text = prompt }
                    }
                }
            },
            generationConfig = new GeminiGenerationConfig
            {
                temperature = 0.3f,
                maxOutputTokens = 1000
            }
        };

        string jsonBody = JsonUtility.ToJson(requestBody);
        Debug.Log($"[EmailAITool] Sending request to Gemini...");

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
                Debug.Log($"[EmailAITool] Raw response: {responseText}");
                string content = ParseGeminiResponse(responseText);
                callback?.Invoke(content);
            }
            else
            {
                string error = $"Gemini API error: {request.responseCode} - {request.downloadHandler.text}";
                Debug.LogError($"[EmailAITool] {error}");
                OnError?.Invoke(error);
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
            Debug.LogError($"[EmailAITool] Error parsing Gemini response: {e.Message}");
        }
        return null;
    }

    private EmailCategory ParseCategory(string categoryString)
    {
        if (string.IsNullOrEmpty(categoryString))
            return EmailCategory.Other;

        categoryString = categoryString.Trim();

        if (Enum.TryParse<EmailCategory>(categoryString, true, out EmailCategory result))
        {
            return result;
        }

        // Try to find partial match
        foreach (EmailCategory cat in Enum.GetValues(typeof(EmailCategory)))
        {
            if (categoryString.ToLower().Contains(cat.ToString().ToLower()))
            {
                return cat;
            }
        }

        return EmailCategory.Other;
    }

    private EmailAnalysis ParseAnalysis(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            // Clean up potential markdown formatting
            json = json.Trim();
            if (json.StartsWith("```json"))
                json = json.Substring(7);
            if (json.StartsWith("```"))
                json = json.Substring(3);
            if (json.EndsWith("```"))
                json = json.Substring(0, json.Length - 3);
            json = json.Trim();

            AnalysisResponse response = JsonUtility.FromJson<AnalysisResponse>(json);

            return new EmailAnalysis
            {
                summary = response.summary,
                category = ParseCategory(response.category),
                categoryReason = response.categoryReason,
                priority = Mathf.Clamp(response.priority, 1, 5)
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"[EmailAITool] Error parsing analysis: {e.Message}\nJSON: {json}");
            return null;
        }
    }

    #endregion

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

    [Serializable]
    private class AnalysisResponse
    {
        public string summary;
        public string category;
        public string categoryReason;
        public int priority;
    }

    #endregion
}
