using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class GCloudAuthManager : MonoBehaviour
{
  [SerializeField] private string clientId;
  [SerializeField] private string clientSecret;

  [Header("Events")]
  public UnityEvent<string, string> OnShowUserCode; // (code, url)
  public UnityEvent OnAuthenticated;
  public UnityEvent<string> OnAuthError;

  public string AccessToken { get; private set; }
  public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

  private const string TOKEN_PREFS_KEY = "gmail_access_token";
  private const string OAUTH_SCOPES = "https://www.googleapis.com/auth/gmail.readonly https://www.googleapis.com/auth/pubsub";

  private void Start()
  {
    LoadSavedToken();
  }

  public void StartAuthentication()
  {
    if (IsAuthenticated)
    {
      OnAuthenticated?.Invoke();
      return;
    }
    StartCoroutine(StartDeviceFlow());
  }

  public void SignOut()
  {
    AccessToken = null;
    PlayerPrefs.DeleteKey(TOKEN_PREFS_KEY);
    PlayerPrefs.Save();
    Debug.Log("[GmailAuth] Signed out");
  }

  private void LoadSavedToken()
  {
    if (PlayerPrefs.HasKey(TOKEN_PREFS_KEY))
    {
      AccessToken = PlayerPrefs.GetString(TOKEN_PREFS_KEY);
      Debug.Log("[GmailAuth] Loaded saved token");
      OnAuthenticated?.Invoke();
    }
  }

  private void SaveToken()
  {
    PlayerPrefs.SetString(TOKEN_PREFS_KEY, AccessToken);
    PlayerPrefs.Save();
    Debug.Log("[GmailAuth] Token saved");
  }

  private IEnumerator StartDeviceFlow()
  {
    Debug.Log("[GmailAuth] Starting device flow authentication...");

    string url = "https://oauth2.googleapis.com/device/code";

    WWWForm form = new WWWForm();
    form.AddField("client_id", clientId);
    form.AddField("scope", OAUTH_SCOPES);

    using (UnityWebRequest request = UnityWebRequest.Post(url, form))
    {
      yield return request.SendWebRequest();

      if (request.result != UnityWebRequest.Result.Success)
      {
        string error = $"Device code request failed: {request.error}";
        Debug.LogError($"[GmailAuth] {error}");
        OnAuthError?.Invoke(error);
        yield break;
      }

      DeviceCodeResponse response = JsonUtility.FromJson<DeviceCodeResponse>(request.downloadHandler.text);

      Debug.Log($"[GmailAuth] Go to: {response.verification_url}");
      Debug.Log($"[GmailAuth] Enter code: {response.user_code}");

      OnShowUserCode?.Invoke(response.user_code, response.verification_url);

      yield return StartCoroutine(PollForToken(response.device_code, response.interval));
    }
  }

  private IEnumerator PollForToken(string deviceCode, int interval)
  {
    string url = "https://oauth2.googleapis.com/token";
    float pollInterval = Mathf.Max(interval, 5);

    while (true)
    {
      yield return new WaitForSeconds(pollInterval);

      WWWForm form = new WWWForm();
      form.AddField("client_id", clientId);
      form.AddField("client_secret", clientSecret);
      form.AddField("device_code", deviceCode);
      form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");

      using (UnityWebRequest request = UnityWebRequest.Post(url, form))
      {
        yield return request.SendWebRequest();

        TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);

        if (!string.IsNullOrEmpty(response.access_token))
        {
          AccessToken = response.access_token;
          SaveToken();
          Debug.Log("[GmailAuth] Authentication successful!");
          OnAuthenticated?.Invoke();
          yield break;
        }
        else if (response.error == "authorization_pending")
        {
          Debug.Log("[GmailAuth] Waiting for user to complete authentication...");
        }
        else if (response.error == "slow_down")
        {
          pollInterval += 5;
        }
        else if (response.error == "expired_token")
        {
          OnAuthError?.Invoke("Device code expired. Please try again.");
          yield break;
        }
        else if (response.error == "access_denied")
        {
          OnAuthError?.Invoke("User denied access.");
          yield break;
        }
        else if (!string.IsNullOrEmpty(response.error))
        {
          OnAuthError?.Invoke($"Auth error: {response.error}");
          yield break;
        }
      }
    }
  }
}

[Serializable] public class DeviceCodeResponse { public string device_code; public string user_code; public string verification_url; public int interval; }
[Serializable] public class TokenResponse { public string access_token; public string error; public string error_description; }
