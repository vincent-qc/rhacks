using UnityEngine;
using TMPro;
using System.Diagnostics;

public class EmailSphere : MonoBehaviour
{
  public string subject;
  public string body;
  public string sender;

  [SerializeField] private TextMeshProUGUI fullContent;
  [SerializeField] private TextMeshProUGUI aiContent;


  [Header("Snapping Configuration")]
  [SerializeField] private float snapVelocityThreshold = 0.2f;
  [SerializeField] private float snapAngleThreshold = 45.0f;
  [SerializeField] private Vector3 snapOffset = new Vector3(0, -1.8f, 1.8f);
  [SerializeField] private float snapDuration = 0.8f;
  [SerializeField] private StateManager state;

  public bool focused = false;
  [SerializeField] private Rigidbody rb;

  private AudioClip cachedAudioClip;

  void Start() {
    GameObject stateManager = GameObject.Find("StateManager");
    this.state = stateManager.GetComponent<StateManager>();
    if (this.state == null) {
      UnityEngine.Debug.LogError("StateManager not found");
    }
  }

  public void Initialize(string sender, string subject, string body)
  {
    this.sender = sender;
    this.subject = subject;
    this.body = body;

    UpdateText();
  }

  public void Initialize(string sender, string subject, string body, AudioClip audioClip)
  {
    this.sender = sender;
    this.subject = subject;
    this.body = body;
    this.cachedAudioClip = audioClip;

    UpdateText();
  }

  public void SetAudioClip(AudioClip clip)
  {
    cachedAudioClip = clip;
  }

  public void PlayCachedAudio()
  {
    if (cachedAudioClip != null)
    {
      Audio.PlayClipIfNotPlaying(cachedAudioClip);
    }
    else
    {
      UnityEngine.Debug.LogWarning("No cached audio clip available for this email sphere.");
    }
  }

  public bool HasCachedAudio()
  {
    return cachedAudioClip != null;
  }
  private void Update()
  {
    this.gameObject.transform.LookAt(Camera.main.transform);
    if (focused) return;
    CheckForSnap();
  }

  private void CheckForSnap()
  {
    if (rb == null || Camera.main == null) return;
    if (state.activeSphere != null) return;

    // Set as active sphere
    state.activeSphere = this.gameObject;

    // Speed check
    if (rb.linearVelocity.magnitude < snapVelocityThreshold) return;

    // Direction check
    Vector3 toCamera = Camera.main.transform.position - transform.position;
    float angle = Vector3.Angle(rb.linearVelocity, toCamera);

    if (angle < snapAngleThreshold)
    {
      StartCoroutine(SnapToReader());
    }
  }

  private System.Collections.IEnumerator SnapToReader()
  {
    focused = true;

    if (rb != null)
    {
      rb.isKinematic = true;
      rb.linearVelocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;
    }

    Transform camTransform = Camera.main.transform;
    transform.SetParent(camTransform);

    Vector3 startPos = transform.localPosition;
    Vector3 targetPos = snapOffset;

    float elapsed = 0f;

    while (elapsed < snapDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / snapDuration;

      float easeT = 1f - Mathf.Pow(1f - t, 3f);

      transform.localPosition = Vector3.Lerp(startPos, targetPos, easeT);
      yield return null;
    }
  }

  private void UpdateText()
  {
    string content = $"From: {sender}\nSubject: {subject}\n\n{body}";
    UnityEngine.Debug.Log(content);
    if (fullContent != null) fullContent.text = content;
    if (aiContent != null) aiContent.text = content;
  }
}
