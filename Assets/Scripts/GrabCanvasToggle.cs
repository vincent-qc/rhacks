using Oculus.Interaction;
using UnityEngine;

public class GrabCanvasToggle : MonoBehaviour
{
    [SerializeField] private Grabbable grabbable;
    [SerializeField] private GameObject aiCanvas;
    [SerializeField] private GameObject fullCanvas;

    void Start()
    {
        if (aiCanvas != null)
            aiCanvas.SetActive(false);
        
        if (fullCanvas != null)
            fullCanvas.SetActive(false);
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
