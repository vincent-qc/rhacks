using UnityEngine;
using UnityEngine.InputSystem;


public class SphereInteraction : MonoBehaviour
{
    public GameObject aiCanvas;
    public GameObject expandedCanvas;

    public float speed = 5f;

    private Vector3 originalPosition;
    private bool zoomed = false;
    private bool expandMessage = false;
    private bool wasZoomed = false;
    private Vector3 selectedPosition = new Vector3(0f, 0f, -3f);

    void Start()
    {
        aiCanvas.SetActive(false);
        expandedCanvas.SetActive(false);
        originalPosition = transform.position;
    }

    void Update()
    {
        zoomed = Keyboard.current != null && Keyboard.current[Key.Space].isPressed;
        expandMessage = Keyboard.current != null && zoomed && Keyboard.current[Key.W].isPressed;

        if (zoomed && !wasZoomed)
        {
            aiCanvas.SetActive(true);
            Debug.Log("Zooming In");
        }
        else if (!zoomed && wasZoomed)
        {
            aiCanvas.SetActive(false);
            expandedCanvas.SetActive(false);
            Debug.Log("Zooming Out");
        }

        if (expandMessage && zoomed)
        {
            expandedCanvas.SetActive(true);
            aiCanvas.SetActive(false);
            Debug.Log("Expanding Message");
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
