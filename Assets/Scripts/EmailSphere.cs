using UnityEngine;
using TMPro;
using System.Diagnostics;

public class EmailSphere : MonoBehaviour
{
  public string subject;
  public string body;
  public string sender;
  public EmailAITool.EmailCategory category;
  public int priority;

  [SerializeField] private TextMeshProUGUI fullContent;
  [SerializeField] private TextMeshProUGUI aiContent;


  [Header("Snapping Configuration")]
  [SerializeField] private float snapVelocityThreshold = 0.2f;
  [SerializeField] private float snapAngleThreshold = 45.0f;
  [SerializeField] private Vector3 snapOffset = new Vector3(0, -0.25f, 0.5f);
  [SerializeField] private float snapDuration = 0.8f;
  [SerializeField] private StateManager state;

  public bool focused = false;
  [SerializeField] private Rigidbody rb;

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

    EmailAudio emailAudio = GetComponent<EmailAudio>();
    if (emailAudio == null) emailAudio = gameObject.AddComponent<EmailAudio>();
    emailAudio.SetAudioClip(audioClip);

    UpdateText();
  }

  public void Initialize(string sender, string subject, string body, AudioClip audioClip, EmailAITool.EmailCategory category, int priority)
  {
    this.sender = sender;
    this.subject = subject;
    this.body = body;
    this.category = category;
    this.priority = priority;

    EmailAudio emailAudio = GetComponent<EmailAudio>();
    if (emailAudio == null) emailAudio = gameObject.AddComponent<EmailAudio>();
    emailAudio.SetAudioClip(audioClip);

    UpdateText();
    ApplyCategoryVisuals();
  }

  private void ApplyCategoryVisuals()
  {
    // TODO: Implement visual changes based on category and priority
    // Example: Change sphere color based on category
    // Renderer renderer = GetComponent<Renderer>();
    // if (renderer != null)
    // {
    //     switch (category)
    //     {
    //         case EmailAITool.EmailCategory.Work:
    //             renderer.material.color = Color.blue;
    //             break;
    //         case EmailAITool.EmailCategory.Personal:
    //             renderer.material.color = Color.green;
    //             break;
    //         case EmailAITool.EmailCategory.Promotions:
    //             renderer.material.color = Color.yellow;
    //             break;
    //         case EmailAITool.EmailCategory.Social:
    //             renderer.material.color = Color.magenta;
    //             break;
    //         default:
    //             renderer.material.color = Color.white;
    //             break;
    //     }
    // }
  }
  private void Update()
  {
    Vector3 lookTarget = Camera.main.transform.position;
    lookTarget.y -= 0.25f; // Look slightly below eye level
    this.gameObject.transform.LookAt(lookTarget);
    if (focused) return;
    CheckForSnap();
  }

  private void CheckForSnap()
  {
    if (rb == null || Camera.main == null) return;

    // Check if there is already a focused sphere
    if (state.focusedSphere != null)
    {
        // If it's this sphere, we don't need to do anything
        if (state.focusedSphere == this.gameObject) return;
    }

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
    // If another sphere is currently focused, eject it
    if (state.focusedSphere != null && state.focusedSphere != this.gameObject)
    {
      EmailSphere current = state.focusedSphere.GetComponent<EmailSphere>();
      if (current != null)
      {
        current.Eject();
      }
    }

    // Set as focused sphere
    focused = true;
    state.focusedSphere = this.gameObject;

    if (rb != null)
    {
      rb.isKinematic = true;
      rb.linearVelocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;
    }

    // Capture starting world position
    Vector3 startPos = transform.position;

    // We will update targetPos dynamically in the loop to chase the camera
    float elapsed = 0f;

    while (elapsed < snapDuration)
    {
      elapsed += Time.deltaTime;
      float t = elapsed / snapDuration;
      float easeT = 1f - Mathf.Pow(1f - t, 3f);

      // Determine where the "slot" is in world space right now
      Vector3 currentTargetPos = Camera.main.transform.TransformPoint(snapOffset);

      transform.position = Vector3.Lerp(startPos, currentTargetPos, easeT);
      yield return null;
    }

    // Ensure we end exactly at the final target position relative to the camera at that moment
    transform.position = Camera.main.transform.TransformPoint(snapOffset);
  }

  public void Eject()
  {
    focused = false;

    // Stop any ongoing snap coroutines on this object
    StopAllCoroutines();

    if (rb != null)
    {
      rb.isKinematic = false;

      // Calculate ejection direction: Forward from camera + slightly up
      Vector3 ejectDirection = Camera.main.transform.forward + (Camera.main.transform.up * 0.5f);
      ejectDirection.Normalize();

      // Apply force
      rb.AddForce(ejectDirection * 2.0f, ForceMode.Impulse);

      // Add some random torque for effect
      rb.AddTorque(UnityEngine.Random.insideUnitSphere * 1.0f, ForceMode.Impulse);
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
