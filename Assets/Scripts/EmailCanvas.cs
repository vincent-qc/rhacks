using Oculus.Interaction;
using UnityEngine;

public class EmailCanvas : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    
    [Header("Canvases")]
    [SerializeField] private GameObject aiCanvas;
    [SerializeField] private GameObject fullCanvas;
    [SerializeField] private GameObject replyCanvas;
    
    [Header("Action Spheres")]
    [SerializeField] private GameObject replySphere;
    [SerializeField] private GameObject deleteSphere;

    [Header("Configuration")]
    [SerializeField] private AudioClip grabSoundEffect;
    [SerializeField] private GameObject profileImage;

    [SerializeField] private EmailSphere emailSphere;

    // State Variables
    public bool replying = false;
    private bool isGrabbed = false;
    private AudioSource audioSource;

    void Start()
    {
        InitializeComponents();
        SetCanvasVisibility(CanvasMode.Hidden);
    }

    private void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
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

    private void Update()
    {
        UpdateCanvasState();
    }

    private void UpdateCanvasState()
    {
        bool isFocused = emailSphere != null && emailSphere.focused;

        // Reset replying state if we lose focus
        if (!isFocused && replying) 
            replying = false;

        if (isFocused)
        {
            if (replying)
                SetCanvasVisibility(CanvasMode.Reply);
            else
                SetCanvasVisibility(CanvasMode.Full);
        }
        else
        {
            if (isGrabbed)
                SetCanvasVisibility(CanvasMode.AI);
            else
                SetCanvasVisibility(CanvasMode.Hidden);
        }
    }

    private enum CanvasMode
    {
        Hidden,
        AI,
        Full,
        Reply
    }

    private void SetCanvasVisibility(CanvasMode mode)
    {
        SetActiveIfExists(aiCanvas, mode == CanvasMode.AI);
        SetActiveIfExists(fullCanvas, mode == CanvasMode.Full);
        SetActiveIfExists(replyCanvas, mode == CanvasMode.Reply);

        // Action spheres are visible only in Full mode (Focused but not replying)
        bool showActions = (mode == CanvasMode.Full);
        SetActiveIfExists(replySphere, showActions);
        SetActiveIfExists(deleteSphere, showActions);
    }
    
    private void SetActiveIfExists(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
            obj.SetActive(active);
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnGrab();
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                OnRelease();
                break;
        }
    }

    private void OnGrab()
    {
        isGrabbed = true;
        PlayGrabSound();
        PlaySummaryAudio();
    }

    private void OnRelease()
    {
        isGrabbed = false;
    }
    
    private void PlayGrabSound()
    {
        if (grabSoundEffect != null && audioSource != null)
            audioSource.PlayOneShot(grabSoundEffect);
    }
    
    private void PlaySummaryAudio()
    {
        if (emailSphere != null)
        {
            EmailAudio emailAudio = emailSphere.GetComponent<EmailAudio>();
            if (emailAudio != null)
                emailAudio.PlayCachedAudio();
        }
    }
}
