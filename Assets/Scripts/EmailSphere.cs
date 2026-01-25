using UnityEngine;
using TMPro;
using System.Diagnostics;
using Oculus.Interaction;

public class EmailSphere : MonoBehaviour
{
  public string subject;
  public string body;
  public string sender;
  public EmailAITool.EmailCategory category;
  public int priority;

  [SerializeField] private TextMeshProUGUI fullContent;
  [SerializeField] private TextMeshProUGUI aiContent;

  [Header("Category Materials")]
  [SerializeField] private Material workMaterial;
  [SerializeField] private Material personalMaterial;
  [SerializeField] private Material promotionsMaterial;
  [SerializeField] private Material socialMaterial;
  [SerializeField] private Material otherMaterial;


  [Header("Snapping Configuration")]
  [SerializeField] private float snapVelocityThreshold = 0.2f;
  [SerializeField] private float snapAngleThreshold = 45.0f;
  [SerializeField] private Vector3 snapOffset = new Vector3(0, -0.25f, 0.5f);
  [SerializeField] private float snapDuration = 0.8f;
  private StateManager state;

  public bool focused = false;
  [SerializeField] private Rigidbody rb;
  [SerializeField] private Grabbable grabbable;
  [SerializeField] private float flickVelocityThreshold = 0.15f;
  
  private bool isGrabbed = false;
  private Coroutine snapCoroutine;

  void Start() {
    GameObject stateManager = GameObject.Find("StateManager");
    this.state = stateManager.GetComponent<StateManager>();
    if (this.state == null) {
      UnityEngine.Debug.LogError("StateManager not found");
    }
    ApplyCategoryVisuals();
    if (grabbable == null) grabbable = GetComponent<Grabbable>();
  }

  void OnEnable()
  {
      if (grabbable != null)
          grabbable.WhenPointerEventRaised += OnPointerEvent;
  }

  void OnDisable()
  {
      if (grabbable != null)
          grabbable.WhenPointerEventRaised -= OnPointerEvent;
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
    MeshRenderer sphereRenderer = GetComponent<MeshRenderer>();
    if (sphereRenderer == null)
    {
      UnityEngine.Debug.LogWarning("EmailSphere: No MeshRenderer found on this object!");
      return;
    }

    Material materialToApply = GetMaterialForCategory(category);
    if (materialToApply != null)
    {
      sphereRenderer.material = materialToApply;
    }
    else
    {
      UnityEngine.Debug.LogWarning($"EmailSphere: No material assigned for category {category}");
    }
  }

  private Material GetMaterialForCategory(EmailAITool.EmailCategory cat)
  {
    switch (cat)
    {
      case EmailAITool.EmailCategory.Work:
        return workMaterial;
      case EmailAITool.EmailCategory.Personal:
        return personalMaterial;
      case EmailAITool.EmailCategory.Promotions:
        return promotionsMaterial;
      case EmailAITool.EmailCategory.Social:
        return socialMaterial;
      case EmailAITool.EmailCategory.Other:
      default:
        return otherMaterial;
    }
  }
  private void Update()
  {
    Vector3 lookTarget = Camera.main.transform.position;
    lookTarget.y -= 0.25f; // Look slightly below eye level
    
    // Only look at camera if we are not being grabbed (let user rotate it)
    if (!isGrabbed) 
    {
        this.gameObject.transform.LookAt(lookTarget);
    }

    if (focused) 
    {
        if (!isGrabbed)
        {
            // Spring back to focused position
            Vector3 targetPos = Camera.main.transform.TransformPoint(snapOffset);
            
            // If checking for snap resulted in a long distance, lerp fast. 
            // If we are just maintaining position, lerp smooth.
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            
            // Ensure kinematic to prevent gravity fall
            if (rb != null && !rb.isKinematic) rb.isKinematic = true;
        }
        return;
    }
    
    CheckForSnap();
  }

  private void CheckForSnap()
  {
    if (rb == null || Camera.main == null || isGrabbed) return;

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

    // Stop physics
    if (rb != null)
    {
      rb.isKinematic = true;
      rb.linearVelocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;
    }

    // We let Update() handle the movement to the snap position now
    yield return null;
  }

  public void Eject()
  {
    focused = false;
    // grabbable remains enabled

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

  void OnTriggerEnter(Collider col)
  {
    UnityEngine.Debug.Log($"Collided with: {col.gameObject.name}");
  }

  private void OnPointerEvent(PointerEvent evt)
  {
      switch (evt.Type)
      {
          case PointerEventType.Select:
              isGrabbed = true;
              StopAllCoroutines(); // Stop any initial snap coroutine if happening
              break;

          case PointerEventType.Unselect:
              isGrabbed = false;
              
              if (focused && rb != null)
              {
                  // Check for flick
                  // Use Rigidbody velocity which should have been imparted by the grabber release
                  // Note: SDK usually applies velocity to RB on Unselect.
                  // We check next frame or assume it's set.
                  // Actually, just checking directly might work if SDK order allows.
                  // If not, we might need a small delay, but let's try direct first.
                  
                  float speed = rb.linearVelocity.magnitude;
                  Vector3 dir = rb.linearVelocity.normalized;
                  Vector3 camFwd = Camera.main.transform.forward;
                  
                  bool isFlickAway = speed > flickVelocityThreshold && Vector3.Dot(dir, camFwd) > 0.0f;
                  
                  if (isFlickAway)
                  {
                      Eject();
                  }
                  else
                  {
                      // Spring back
                      rb.isKinematic = true;
                      rb.linearVelocity = Vector3.zero;
                  }
              }
              break;
      }
  }
}
