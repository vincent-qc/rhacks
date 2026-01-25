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

  private StateManager state;

  public bool focused = false;
  [SerializeField] private Rigidbody rb;
  [SerializeField] private Grabbable grabbable;

  
  private bool isGrabbed = false;

  private Vector3 targetSnapPosition;

  void Start() {
    GameObject stateManager = GameObject.Find("StateManager");
    this.state = stateManager.GetComponent<StateManager>();
    if (this.state == null) {
      UnityEngine.Debug.LogError("StateManager not found");
    }
    ApplyCategoryVisuals();
    if (grabbable == null) grabbable = GetComponent<Grabbable>();
    
    // Initialize targetSnapPosition if starting focused
    if (focused)
    {
        targetSnapPosition = transform.position;
    }
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

  public void Initialize(string sender, string subject, string body, string summary, AudioClip audioClip, EmailAITool.EmailCategory category, int priority)
  {
    this.sender = sender;
    this.subject = subject;
    this.body = body;
    this.summary = summary;
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
            // Spring back to fixed world position
            // If checking for snap resulted in a long distance, lerp fast. 
            // If we are just maintaining position, lerp smooth.
            transform.position = Vector3.Lerp(transform.position, targetSnapPosition, Time.deltaTime * 5.0f);
            
            // Ensure kinematic to prevent gravity fall
            if (rb != null && !rb.isKinematic) rb.isKinematic = true;

        }
        return;
    }
    
    if (state != null && grabbable != null)
    {
        if (state.isFist)
        {
            if (grabbable.enabled) grabbable.enabled = false;
        }
        else
        {
            if (!grabbable.enabled) grabbable.enabled = true;
        }
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
    
    // Determine stationary target position in world space
    targetSnapPosition = Camera.main.transform.TransformPoint(snapOffset);

    // Stop physics
    if (rb != null)
    {
      rb.isKinematic = true;
      rb.linearVelocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;
    }

    yield return null;
  }

  public void Eject()
  {
    focused = false;
    StopAllCoroutines();
    StartCoroutine(EjectRoutine());
  }

  private System.Collections.IEnumerator EjectRoutine()
  {
    yield return new WaitForFixedUpdate();

    if (rb != null)
    {
      rb.isKinematic = false;

      Vector3 ejectDirection = Camera.main.transform.forward + (Camera.main.transform.up * 0.5f);
      ejectDirection.Normalize();
      rb.AddForce(ejectDirection * 2.0f, ForceMode.Impulse);
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
    if(col.gameObject.name == "Collider") {
      if (state.isFist) {
        state.focusedSphere = null;
        Destroy(this.gameObject);
      }
    }
  }

  private void OnPointerEvent(PointerEvent evt)
  {
      switch (evt.Type)
      {
          case PointerEventType.Select:
              isGrabbed = true;
              StopAllCoroutines();
              break;

          case PointerEventType.Unselect:
              isGrabbed = false;
              if(focused) {
                Vector3 currentPos = this.gameObject.transform.position;
                float distance = Vector3.Distance(currentPos, targetSnapPosition);
                if (distance > 0.15f) {
                    state.focusedSphere = null;
                    Eject();
                    UnityEngine.Debug.Log("Ejecting");
                }
              }
              break;
    }
  }
}
