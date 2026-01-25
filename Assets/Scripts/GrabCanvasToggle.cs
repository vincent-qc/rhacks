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
        bool shouldShow = isGrabbed || (emailSphere != null && emailSphere.focused);

        if (aiCanvas != null && aiCanvas.activeSelf != shouldShow)
        {
            aiCanvas.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            if (fullCanvas != null && fullCanvas.activeSelf)
                fullCanvas.SetActive(false);
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
