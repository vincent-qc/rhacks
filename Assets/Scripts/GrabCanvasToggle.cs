using Oculus.Interaction;
using UnityEngine;

public class GrabCanvasToggle : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GameObject aiCanvas;
    [SerializeField] private GameObject fullCanvas;
    [SerializeField] private AudioClip grabSoundEffect;
    
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

    private void OnPointerEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                if (aiCanvas != null)
                    aiCanvas.SetActive(true);
                
                // Play grab sound effect
                if (grabSoundEffect != null && audioSource != null)
                {
                    audioSource.PlayOneShot(grabSoundEffect);
                }
                break;

            case PointerEventType.Unselect:
            case PointerEventType.Cancel:
                if (aiCanvas != null)
                    aiCanvas.SetActive(false);
                if (fullCanvas != null)
                    fullCanvas.SetActive(false);
                break;
        }
    }
}
