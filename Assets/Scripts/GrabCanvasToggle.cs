using Oculus.Interaction;
using UnityEngine;

public class GrabCanvasToggle : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GameObject aiCanvas;
    [SerializeField] private GameObject fullCanvas;
    [SerializeField] private AudioClip grabSoundEffect;
    [SerializeField] private GameObject profileImage;

    [SerializeField] private EmailSphere emailSphere;
  [SerializeField] private GameObject replySphere;
  [SerializeField] private GameObject deleteSphere;

  private AudioSource audioSource;

    void Start()
    {
        if (aiCanvas != null)
            aiCanvas.SetActive(false);

        if (fullCanvas != null) 
            fullCanvas.SetActive(false);

        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
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

    private bool isGrabbed = false;

    private void Update()
    {
        bool isFocused = emailSphere != null && emailSphere.focused;

        if (isFocused)
        {
            // FOCUSED STATE: Show Full Canvas & Action Spheres, Hide AI Canvas
            if (aiCanvas != null) aiCanvas.SetActive(false);
            if (fullCanvas != null) fullCanvas.SetActive(true);
            if (replySphere != null) replySphere.SetActive(true);
            if (deleteSphere != null) deleteSphere.SetActive(true);
        }
        else if (isGrabbed)
        {
            // GRABBED BUT NOT FOCUSED: Show AI Canvas, Hide Full Canvas & Action Spheres
            if (aiCanvas != null) aiCanvas.SetActive(true);
            if (fullCanvas != null) fullCanvas.SetActive(false);
            if (replySphere != null) replySphere.SetActive(false);
            if (deleteSphere != null) deleteSphere.SetActive(false);
        }
        else
        {
            // IDLE: Hide All
            if (aiCanvas != null) aiCanvas.SetActive(false);
            if (fullCanvas != null) fullCanvas.SetActive(false);
            if (replySphere != null) replySphere.SetActive(false);
            if (deleteSphere != null) deleteSphere.SetActive(false);
        }
    }

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                isGrabbed = true;

                // Play grab sound effect
                if (grabSoundEffect != null && audioSource != null)
                {
                    audioSource.PlayOneShot(grabSoundEffect);
                }

                // Play the cached AI summary audio
                if (emailSphere != null)
                {
                    EmailAudio emailAudio = emailSphere.GetComponent<EmailAudio>();
                    if (emailAudio != null)
                    {
                        emailAudio.PlayCachedAudio();
                    }
                }
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                isGrabbed = false;
                break;
        }
    }
}
