using UnityEngine;
using UnityEngine.InputSystem;


public class SphereInteraction : MonoBehaviour
{
    public Canvas aiCanvas;
    public Canvas expandedCanvas;

    public float speed = 5f;

    private Vector3 originalPosition;
    private bool zoomed = false;
    private bool expandMessage = false;
    private bool wasZoomed = false;
    private Vector3 selectedPosition = new Vector3(0f, 0f, -3f);

    void Start()
    {
        aiCanvas.enabled = false;
        expandedCanvas.enabled = false;
        originalPosition = transform.position;
    }

    void Update()
    {
        zoomed = Keyboard.current != null && Keyboard.current[Key.Space].isPressed;
        expandMessage = Keyboard.current != null && zoomed && Keyboard.current[Key.W].isPressed;

        if (zoomed && !wasZoomed)
        {
            aiCanvas.enabled = true;
            Debug.Log("Zooming In");
        }
        else if (!zoomed && wasZoomed)
        {
            aiCanvas.enabled = false;
            Debug.Log("Zooming Out");
        }

        if (expandMessage && zoomed)
        {
            expandedCanvas.enabled = true;
            aiCanvas.enabled = false;
            Debug.Log("Expanding Message");
        }
        else
        {
            expandedCanvas.enabled = false;
        }

        wasZoomed = zoomed;

        Vector3 targetPosition = zoomed ? selectedPosition : originalPosition;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * speed
        );
    }
}
