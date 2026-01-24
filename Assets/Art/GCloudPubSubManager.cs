using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class GCloudPubSubManager : MonoBehaviour
{
  [SerializeField] private string projectId = "your-project-id";
  [SerializeField] private string topicName = "gmail-notifications";
  [SerializeField] private string subscriptionName = "gmail-sub";

  [Header("Polling Settings")]
  [SerializeField] private float pollInterval = 5f;
  [SerializeField] private int maxMessagesPerPoll = 10;
  [SerializeField] private bool autoStartPolling = true;

  [Header("References")]
  [SerializeField] private GCloudAuthManager authManager;

  [Header("Events")]
  public UnityEvent<EmailData> OnNewEmail;
  public UnityEvent<string> OnError;

  // Internal state
  private bool _isPolling = false;
  private bool _watchActive = false;
  private Coroutine _pollingCoroutine;
  private Coroutine _watchRenewalCoroutine;
  private string _lastHistoryId;

  private string TopicPath => $"projects/{projectId}/topics/{topicName}";
  private string SubscriptionPath => $"projects/{projectId}/subscriptions/{subscriptionName}";

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
    if (authManager == null)
    {
      authManager = GetComponent<GCloudAuthManager>();
    }

    if (authManager != null)
    {
      authManager.OnAuthenticated.AddListener(OnAuthenticationComplete);
    }
  }

  private void OnDestroy()
  {
    StopPolling();

    if (authManager != null)
    {
      authManager.OnAuthenticated.RemoveListener(OnAuthenticationComplete);
    }
  }

  private void OnAuthenticationComplete()
  {
    Debug.Log("[GmailPubSub] Authentication complete, setting up watch...");
    StartCoroutine(SetupGmailWatch());
  }

  /// <summary>
  /// Sets up Gmail push notifications to your Pub/Sub topic.
  /// </summary>
  public IEnumerator SetupGmailWatch()
  {
    if (!authManager.IsAuthenticated)
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

        // Schedule watch renewal (watch expires after ~7 days)
        if (_watchRenewalCoroutine != null)
        {
          StopCoroutine(_watchRenewalCoroutine);
        }
        _watchRenewalCoroutine = StartCoroutine(ScheduleWatchRenewal());

        // Start polling if auto-start is enabled
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

  /// <summary>
  /// Stop Gmail push notifications.
  /// </summary>
  public IEnumerator StopGmailWatch()
  {
    if (!authManager.IsAuthenticated)
    {
      yield break;
    }

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
    // Renew watch every 6 days (before 7-day expiration)
    float renewalInterval = 6 * 24 * 60 * 60; // 6 days in seconds

    while (_watchActive)
    {
      yield return new WaitForSeconds(renewalInterval);

      if (authManager.IsAuthenticated && _watchActive)
      {
        Debug.Log("[GmailPubSub] Renewing Gmail watch...");
        yield return StartCoroutine(SetupGmailWatch());
      }
    }
  }

  /// <summary>
  /// Start polling Pub/Sub for notifications.
  /// </summary>
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

  /// <summary>
  /// Stop polling Pub/Sub.
  /// </summary>
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
      if (!authManager.IsAuthenticated)
      {
        yield return new WaitForSeconds(pollInterval);
        continue;
      }

      PullRequest pullRequest = new PullRequest { maxMessages = maxMessagesPerPoll };
      string jsonBody = JsonUtility.ToJson(pullRequest);

      using (UnityWebRequest request = CreatePostRequest(url, jsonBody))
      {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
          string responseText = request.downloadHandler.text;

          // Only process if we got messages
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
    // Decode the base64 message data
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

    // Fetch the new email
    yield return StartCoroutine(FetchLatestEmail());

    // Acknowledge the message
    yield return StartCoroutine(AcknowledgeMessage(msg.ackId));
  }

  private IEnumerator AcknowledgeMessage(string ackId)
  {
    string url = $"https://pubsub.googleapis.com/v1/{SubscriptionPath}:acknowledge";

    // Create JSON manually for array
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

  /// <summary>
  /// Fetch the latest email from Gmail.
  /// </summary>
  public IEnumerator FetchLatestEmail()
  {
    if (!authManager.IsAuthenticated)
    {
      yield break;
    }

    // Step 1: Get latest message ID
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

      string messageId = listResponse.messages[0].id;

      // Step 2: Get full message details
      yield return StartCoroutine(FetchEmailById(messageId));
    }
  }

  /// <summary>
  /// Fetch a specific email by ID.
  /// </summary>
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
  /// Manually check for new emails (useful for testing).
  /// </summary>
  public void CheckForNewEmails()
  {
    StartCoroutine(FetchLatestEmail());
  }

  // Helper methods for creating requests
  private UnityWebRequest CreateGetRequest(string url)
  {
    UnityWebRequest request = UnityWebRequest.Get(url);
    request.SetRequestHeader("Authorization", $"Bearer {authManager.AccessToken}");
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
    request.SetRequestHeader("Authorization", $"Bearer {authManager.AccessToken}");
    request.SetRequestHeader("Content-Type", "application/json");

    return request;
  }
}

// Pub/Sub types
[Serializable] public class PullRequest { public int maxMessages; }
[Serializable] public class PullResponse { public ReceivedMessage[] receivedMessages; }
[Serializable] public class ReceivedMessage { public string ackId; public PubSubMessage message; }
[Serializable] public class PubSubMessage { public string data; public string messageId; }

// Gmail Watch types
[Serializable] public class WatchRequest { public string topicName; public string[] labelIds; }
[Serializable] public class WatchResponse { public string historyId; public string expiration; }
[Serializable] public class GmailNotification { public string emailAddress; public string historyId; }

// Gmail Message types
[Serializable] public class MessageListResponse { public MessageRef[] messages; }
[Serializable] public class MessageRef { public string id; public string threadId; }
[Serializable] public class Message { public string id; public string snippet; public MessagePayload payload; }
[Serializable] public class MessagePayload { public Header[] headers; }
[Serializable] public class Header { public string name; public string value; }
