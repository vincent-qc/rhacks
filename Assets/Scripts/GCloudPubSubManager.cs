using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class GCloudPubSubManager : MonoBehaviour
{
    private Credentials _credentials;

    [SerializeField] private string topicName = "gmail-notifications";
    [SerializeField] private string subscriptionName = "gmail-sub";

    [SerializeField] private float pollInterval = 5f;
    [SerializeField] private int maxMessagesPerPoll = 10;
    [SerializeField] private bool autoStartPolling = true;

    [Header("Events")]
    public UnityEvent<EmailData> OnNewEmail;
    public UnityEvent<string> OnError;

    // Internal state
    private string _accessToken;
    private bool _isPolling = false;
    private bool _watchActive = false;
    private Coroutine _pollingCoroutine;
    private Coroutine _watchRenewalCoroutine;
    private string _lastHistoryId;

    // Computed properties using loaded credentials
    private string TopicPath => _credentials != null ? $"projects/{_credentials.project_id}/topics/{topicName}" : "";
    private string SubscriptionPath => _credentials != null ? $"projects/{_credentials.project_id}/subscriptions/{subscriptionName}" : "";

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    [Serializable]
    public class Credentials
    {
        public string client_id;
        public string client_secret;
        public string project_id;
        public string refresh_token;
    }

    [Serializable]
    public class EmailData
    {
        public string id;
        public string subject;
        public string from;
        public string snippet;
        public string date;
    }

    private void Start()
    {
        StartCoroutine(LoadCredentialsAndLogin());
    }

    private void OnDestroy()
    {
        StopPolling();
    }

    private IEnumerator LoadCredentialsAndLogin()
    {
        // 1. Load Credentials from StreamingAssets
        string filePath = Path.Combine(Application.streamingAssetsPath, "credentials.json");
        
        // On Android, streamingAssetsPath starts with "jar:file://", which UnityWebRequest handles correctly.
        // On Editor/Standalone, it's a normal path, but UnityWebRequest expects "file://" prefix for local files sometimes, 
        // though standard path often works. Adding file:// protocol is safer for cross-platform consistency if not present.
        if (!filePath.Contains("://"))
        {
            filePath = "file://" + filePath;
        }

        Debug.Log($"[GmailPubSub] Loading credentials from: {filePath}");

        using (UnityWebRequest request = UnityWebRequest.Get(filePath))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GmailPubSub] Failed to load credentials: {request.error}");
                yield break;
            }

            try
            {
                _credentials = JsonUtility.FromJson<Credentials>(request.downloadHandler.text);
                Debug.Log($"[GmailPubSub] Credentials loaded for project: {_credentials.project_id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GmailPubSub] Error parsing credentials JSON: {e.Message}");
                yield break;
            }
        }

        // 2. Refresh Access Token using loaded credentials
        yield return StartCoroutine(RefreshAccessToken());
    }

    private IEnumerator RefreshAccessToken()
    {
        if (_credentials == null)
        {
            Debug.LogError("[GmailPubSub] Cannot refresh token: Credentials not loaded.");
            yield break;
        }

        string url = "https://oauth2.googleapis.com/token";
        WWWForm form = new WWWForm();
        form.AddField("client_id", _credentials.client_id);
        form.AddField("client_secret", _credentials.client_secret);
        form.AddField("refresh_token", _credentials.refresh_token);
        form.AddField("grant_type", "refresh_token");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                _accessToken = response.access_token;
                Debug.Log("[GmailPubSub] Access Token refreshed successfully.");
                OnAuthenticationComplete();
            }
            else
            {
                string error = $"Failed to refresh token: {request.error} - {request.downloadHandler.text}";
                Debug.LogError($"[GmailPubSub] {error}");
                OnError?.Invoke(error);
            }
        }
    }

    private void OnAuthenticationComplete()
    {
        Debug.Log("[GmailPubSub] Authentication complete, fetching recent emails and setting up watch...");
        StartCoroutine(FetchAndPrintRecentEmails(10));
        StartCoroutine(SetupGmailWatch());
    }

    public IEnumerator FetchAndPrintRecentEmails(int count)
    {
        if (!IsAuthenticated) yield break;

        string listUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults={count}";
        using (UnityWebRequest request = CreateGetRequest(listUrl))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GmailPubSub] Failed to list recent messages: {request.error}");
                yield break;
            }

            MessageListResponse listResponse = JsonUtility.FromJson<MessageListResponse>(request.downloadHandler.text);
            if (listResponse.messages != null)
            {
                foreach (var messageRef in listResponse.messages)
                {
                    yield return StartCoroutine(FetchEmailById(messageRef.id));
                }
            }
        }
    }



    public IEnumerator SetupGmailWatch()
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("[GmailPubSub] Not authenticated!");
            yield break;
        }

        string url = "https://gmail.googleapis.com/gmail/v1/users/me/watch";

        WatchRequest watchRequest = new WatchRequest
        {
            topicName = TopicPath,
            labelIds = new string[] { "INBOX" }
        };

        string jsonBody = JsonUtility.ToJson(watchRequest);

        using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                WatchResponse response = JsonUtility.FromJson<WatchResponse>(request.downloadHandler.text);
                _lastHistoryId = response.historyId;
                _watchActive = true;

                Debug.Log($"[GmailPubSub] Watch setup successful! Expires: {response.expiration}");

                if (_watchRenewalCoroutine != null)
                {
                    StopCoroutine(_watchRenewalCoroutine);
                }
                _watchRenewalCoroutine = StartCoroutine(ScheduleWatchRenewal());

                if (autoStartPolling)
                {
                    StartPolling();
                }
            }
            else
            {
                string error = $"Watch setup failed: {request.error} - {request.downloadHandler.text}";
                Debug.LogError($"[GmailPubSub] {error}");
                OnError?.Invoke(error);
            }
        }
    }

    public IEnumerator StopGmailWatch()
    {
        if (!IsAuthenticated) yield break;

        string url = "https://gmail.googleapis.com/gmail/v1/users/me/stop";

        using (UnityWebRequest request = CreatePostRequest(url, ""))
        {
            yield return request.SendWebRequest();
            _watchActive = false;
            Debug.Log("[GmailPubSub] Watch stopped");
        }
    }

    private IEnumerator ScheduleWatchRenewal()
    {
        float renewalInterval = 6 * 24 * 60 * 60; // 6 days

        while (_watchActive)
        {
            yield return new WaitForSeconds(renewalInterval);

            if (IsAuthenticated && _watchActive)
            {
                Debug.Log("[GmailPubSub] Renewing Gmail watch...");
                yield return StartCoroutine(SetupGmailWatch());
            }
        }
    }

    public void StartPolling()
    {
        if (_isPolling)
        {
            Debug.LogWarning("[GmailPubSub] Already polling");
            return;
        }

        _isPolling = true;
        _pollingCoroutine = StartCoroutine(PollPubSub());
        Debug.Log("[GmailPubSub] Started polling for notifications");
    }

    public void StopPolling()
    {
        _isPolling = false;

        if (_pollingCoroutine != null)
        {
            StopCoroutine(_pollingCoroutine);
            _pollingCoroutine = null;
        }

        Debug.Log("[GmailPubSub] Stopped polling");
    }

    private IEnumerator PollPubSub()
    {
        string url = $"https://pubsub.googleapis.com/v1/{SubscriptionPath}:pull";

        while (_isPolling)
        {
            if (!IsAuthenticated)
            {
                // Try to refresh if auth is lost? For now just wait.
                yield return new WaitForSeconds(pollInterval);
                continue;
            }

            Debug.Log("POLLING");

            PullRequest pullRequest = new PullRequest { maxMessages = maxMessagesPerPoll };
            string jsonBody = JsonUtility.ToJson(pullRequest);

            using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("POLL SUCCESSFUL");
                    string responseText = request.downloadHandler.text;

                    if (responseText.Contains("receivedMessages"))
                    {
                        PullResponse response = JsonUtility.FromJson<PullResponse>(responseText);

                        if (response.receivedMessages != null && response.receivedMessages.Length > 0)
                        {
                            Debug.Log($"[GmailPubSub] Received {response.receivedMessages.Length} notification(s)");

                            foreach (var msg in response.receivedMessages)
                            {
                                yield return StartCoroutine(ProcessNotification(msg));
                            }
                        }
                    }
                }
                else if (request.responseCode != 200)
                {
                    Debug.LogWarning($"[GmailPubSub] Poll error: {request.error}");
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator ProcessNotification(ReceivedMessage msg)
    {
        string jsonData;
        try
        {
            byte[] data = Convert.FromBase64String(msg.message.data);
            jsonData = Encoding.UTF8.GetString(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GmailPubSub] Error decoding notification: {e.Message}");
            yield break;
        }

        Debug.Log($"[GmailPubSub] Notification data: {jsonData}");

        yield return StartCoroutine(FetchLatestEmail());
        yield return StartCoroutine(AcknowledgeMessage(msg.ackId));
    }

    private IEnumerator AcknowledgeMessage(string ackId)
    {
        string url = $"https://pubsub.googleapis.com/v1/{SubscriptionPath}:acknowledge";
        string jsonBody = $"{{\"ackIds\": [\"{ackId}\"]}}";

        using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[GmailPubSub] Message acknowledged");
            }
        }
    }

    public IEnumerator FetchLatestEmail()
    {
        if (!IsAuthenticated) yield break;

        string listUrl = "https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=1";

        using (UnityWebRequest request = CreateGetRequest(listUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GmailPubSub] Failed to list messages: {request.error}");
                yield break;
            }

            MessageListResponse listResponse = JsonUtility.FromJson<MessageListResponse>(request.downloadHandler.text);

            if (listResponse.messages == null || listResponse.messages.Length == 0)
            {
                Debug.Log("[GmailPubSub] No messages found");
                yield break;
            }

            yield return StartCoroutine(FetchEmailById(listResponse.messages[0].id));
        }
    }

    public IEnumerator FetchEmailById(string messageId)
    {
        string url = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}";

        using (UnityWebRequest request = CreateGetRequest(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Message message = JsonUtility.FromJson<Message>(request.downloadHandler.text);
                EmailData emailData = ParseEmailData(message);

                Debug.Log($"Sender Name: {emailData.from}");
                Debug.Log($"[GmailPubSub] New email - Subject: {emailData.subject}, From: {emailData.from}");

                OnNewEmail?.Invoke(emailData);
            }
            else
            {
                Debug.LogError($"[GmailPubSub] Failed to fetch message: {request.error}");
            }
        }
    }

    private EmailData ParseEmailData(Message message)
    {
        EmailData email = new EmailData
        {
            id = message.id,
            snippet = message.snippet
        };

        if (message.payload?.headers != null)
        {
            foreach (var header in message.payload.headers)
            {
                switch (header.name.ToLower())
                {
                    case "subject":
                        email.subject = header.value;
                        break;
                    case "from":
                        email.from = header.value;
                        break;
                    case "date":
                        email.date = header.value;
                        break;
                }
            }
        }

        return email;
    }

    /// <summary>
    /// Send an email using Gmail API
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body content</param>
    /// <param name="callback">Callback when send completes (success/failure)</param>
    public void SendEmail(string to, string subject, string body, Action<bool, string> callback = null)
    {
        StartCoroutine(SendEmailCoroutine(to, subject, body, callback));
    }

    private IEnumerator SendEmailCoroutine(string to, string subject, string body, Action<bool, string> callback)
    {
        if (!IsAuthenticated)
        {
            string error = "Not authenticated. Cannot send email.";
            Debug.LogError($"[GmailPubSub] {error}");
            callback?.Invoke(false, error);
            yield break;
        }

        // Create RFC 2822 formatted email message
        string emailMessage = CreateRFC2822Message(to, subject, body);
        
        // Base64 URL-safe encoding (Gmail API requirement)
        string encodedMessage = Base64UrlEncode(emailMessage);

        // Create JSON request body
        string jsonBody = $"{{\"raw\": \"{encodedMessage}\"}}";

        string url = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

        using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[GmailPubSub] Email sent successfully!");
                callback?.Invoke(true, "Email sent successfully");
            }
            else
            {
                string error = $"Failed to send email: {request.error} - {request.downloadHandler.text}";
                Debug.LogError($"[GmailPubSub] {error}");
                callback?.Invoke(false, error);
            }
        }
    }

    /// <summary>
    /// Create RFC 2822 formatted email message
    /// </summary>
    private string CreateRFC2822Message(string to, string subject, string body)
    {
        StringBuilder sb = new StringBuilder();
        
        // Headers
        sb.AppendLine($"To: {to}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine("Content-Type: text/plain; charset=utf-8");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine(); // Empty line separates headers from body
        
        // Body
        sb.Append(body);

        return sb.ToString();
    }

    /// <summary>
    /// Base64 URL-safe encoding (Gmail API requires this format)
    /// </summary>
    private string Base64UrlEncode(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        string base64 = Convert.ToBase64String(inputBytes);
        
        // Make it URL-safe: replace +/= with -_
        return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
    }

    private UnityWebRequest CreateGetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
        return request;
    }

    private UnityWebRequest CreatePostRequest(string url, string jsonBody)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        if (!string.IsNullOrEmpty(jsonBody))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        }
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
}

// Data Classes
[Serializable] public class TokenResponse { public string access_token; public string expires_in; public string token_type; }

// Pub/Sub types
[Serializable] public class PullRequest { public int maxMessages; }
[Serializable] public class PullResponse { public ReceivedMessage[] receivedMessages; }
[Serializable] public class ReceivedMessage { public string ackId; public PubSubMessage message; }
[Serializable] public class PubSubMessage { public string data; public string messageId; }

// Gmail Watch types
[Serializable] public class WatchRequest { public string topicName; public string[] labelIds; }
[Serializable] public class WatchResponse { public string historyId; public string expiration; }

// Gmail Message types
[Serializable] public class MessageListResponse { public MessageRef[] messages; }
[Serializable] public class MessageRef { public string id; public string threadId; }
[Serializable] public class Message { public string id; public string snippet; public MessagePayload payload; }
[Serializable] public class MessagePayload { public Header[] headers; }
[Serializable] public class Header { public string name; public string value; }
