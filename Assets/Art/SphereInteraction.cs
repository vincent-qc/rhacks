using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class SphereInteraction : MonoBehaviour
{
    public GameObject aiCanvas;
    public GameObject fullCanvas;

    public float speed = 5f;

    private Vector3 originalPosition;
    private static bool isZoomed = false;
    private static bool isExpanded = false;
    private bool wasZoomed = false;
    private Vector3 selectedPosition = new Vector3(0f, 0f, -3f);

    private TextMeshProUGUI aiTitle;
    private TextMeshProUGUI aiContent;
    private TextMeshProUGUI aiDate;

    private TextMeshProUGUI fullTitle;
    private TextMeshProUGUI fullContent;
    private TextMeshProUGUI fullDate;

    private static string aiTitleInput;
    private static string aiContentInput;
    private static string aiDateInput;
    private static string expandTitleInput;
    private static string expandContentInput;
    private static string expandDateInput;

    void Start()
    {
        aiCanvas.SetActive(false);
        fullCanvas.SetActive(false);
        originalPosition = transform.position;

        aiTitle = aiCanvas.transform.Find("title").GetComponent<TextMeshProUGUI>();
        aiContent = aiCanvas.transform.Find("content").GetComponent<TextMeshProUGUI>();
        aiDate = aiCanvas.transform.Find("date").GetComponent<TextMeshProUGUI>();

        fullTitle = fullCanvas.transform.Find("title").GetComponent<TextMeshProUGUI>();
        fullContent = fullCanvas.transform.Find("content").GetComponent<TextMeshProUGUI>();
        fullDate = fullCanvas.transform.Find("date").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[Key.Space].isPressed)
        {
            OpenEmail(
                "AI Response",
                "Hello, I am your AI assistant. How can I help you today?",
                "2024-06-01",
                "Detailed AI Response",
                "Here is a more detailed response from your AI assistant, providing additional information and context to assist you further.",
                "2024-06-01"
            );
        }
        else
        {
            CloseEmail();
        }

        if (Keyboard.current != null && Keyboard.current[Key.W].isPressed)
        {
            OpenExpanded();
        }
        // isZoomed = Keyboard.current != null && Keyboard.current[Key.Space].isPressed;
        // isExpanded = Keyboard.current != null && isZoomed && Keyboard.current[Key.W].isPressed;

        if (isZoomed && !wasZoomed)
        {
            ChangeAIEmail(
                aiTitleInput,
                aiContentInput,
                aiDateInput
            );
            aiCanvas.SetActive(true);
            Debug.Log("Zooming In");
        }
        else if (!isZoomed && wasZoomed)
        {
            aiCanvas.SetActive(false);
            fullCanvas.SetActive(false);
            Debug.Log("Zooming Out");
        }

        if (isExpanded && isZoomed)
        {
            ChangeFullEmail(
                expandTitleInput,
                expandContentInput,
                expandDateInput
            );
            fullCanvas.SetActive(true);
            aiCanvas.SetActive(false);
            Debug.Log("Expanding Message");
        }

        wasZoomed = isZoomed;

        Vector3 targetPosition = isZoomed ? selectedPosition : originalPosition;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * speed
        );
    }

    public static void OpenEmail(string aiTitle, string aiContent, string aiDate, string expandTitle, string expandContent, string expandDate)
    {
        aiTitleInput = aiTitle;
        aiContentInput = aiContent;
        aiDateInput = aiDate;
        expandTitleInput = expandTitle;
        expandContentInput = expandContent;
        expandDateInput = expandDate;

        isZoomed = true;
    }

    public static void OpenExpanded()
    {
        isExpanded = true;
    }

    public static void CloseEmail()
    {
        isZoomed = false;
        isExpanded = false;
    }

    void ChangeAIEmail(string title, string content, string date)
    {
        aiTitle.text = title;
        aiContent.text = content;
        aiDate.text = date;
    }

    void ChangeFullEmail(string title, string content, string date)
    {
        fullTitle.text = title;
        fullContent.text = content;
        fullDate.text = date;
    }
}
