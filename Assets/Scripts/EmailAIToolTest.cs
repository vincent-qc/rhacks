using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Test script for EmailAITool.
/// Press 1 - Get Summary only
/// Press 2 - Get Category only
/// Press 3 - Full Analysis (summary + category + priority)
/// Press 4 - Test with different sample emails
/// </summary>
public class EmailAIToolTest : MonoBehaviour
{
    [Header("UI (Optional)")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Sample Emails")]
    [SerializeField] private int currentSampleIndex = 0;

    private EmailAITool emailAITool;
    private bool isInitialized = false;

    // Sample emails for testing
    private readonly string[][] sampleEmails = new string[][]
    {
        // Work email
        new string[] {
            "boss@company.com",
            "Urgent: Q4 Report Due Tomorrow",
            "Hi team,\n\nJust a reminder that the Q4 financial report is due tomorrow at 5 PM. Please ensure all your sections are completed and submitted to the shared drive by EOD today for final review.\n\nThanks,\nJohn"
        },
        // Promotional email
        new string[] {
            "deals@amazon.com",
            "Flash Sale: 50% Off Electronics!",
            "Don't miss our biggest sale of the year! Get up to 50% off on all electronics including laptops, tablets, and smartphones. Use code FLASH50 at checkout. Offer valid for 24 hours only!"
        },
        // Personal email
        new string[] {
            "mom@gmail.com",
            "Dinner this Sunday?",
            "Hey sweetie,\n\nWould you like to come over for dinner this Sunday? I'm making your favorite lasagna. Let me know if you can make it!\n\nLove,\nMom"
        },
        // Finance email
        new string[] {
            "alerts@bank.com",
            "Your Monthly Statement is Ready",
            "Dear Customer,\n\nYour monthly account statement for December 2024 is now available. Your current balance is $3,542.18. Log in to view your full statement and transaction history."
        },
        // Shopping/Order email
        new string[] {
            "orders@shop.com",
            "Your order has shipped!",
            "Great news! Your order #12345 has been shipped and is on its way. Expected delivery: January 28, 2025. Track your package using the link below."
        }
    };

    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        yield return null;

        emailAITool = FindFirstObjectByType<EmailAITool>();
        if (emailAITool == null)
        {
            GameObject go = new GameObject("EmailAITool");
            emailAITool = go.AddComponent<EmailAITool>();
            yield return null;
        }

        // Subscribe to events
        if (emailAITool.OnSummaryComplete != null)
            emailAITool.OnSummaryComplete.AddListener(OnSummaryReceived);
        if (emailAITool.OnCategoryComplete != null)
            emailAITool.OnCategoryComplete.AddListener(OnCategoryReceived);
        if (emailAITool.OnAnalysisComplete != null)
            emailAITool.OnAnalysisComplete.AddListener(OnAnalysisReceived);
        if (emailAITool.OnError != null)
            emailAITool.OnError.AddListener(OnError);

        isInitialized = true;
        UpdateStatus("Ready! Press 1=Summary, 2=Category, 3=Full Analysis, 4=Next Sample");
        ShowCurrentSample();

        Debug.Log("[EmailAITest] Ready. Controls:");
        Debug.Log("  1 - Get Summary");
        Debug.Log("  2 - Get Category");
        Debug.Log("  3 - Full Analysis");
        Debug.Log("  4 - Next Sample Email");
    }

    private void Update()
    {
        if (!isInitialized) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Press 1 - Get Summary
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            TestGetSummary();
        }

        // Press 2 - Get Category
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            TestGetCategory();
        }

        // Press 3 - Full Analysis
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            TestFullAnalysis();
        }

        // Press 4 - Next sample email
        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            NextSample();
        }
    }

    private string GetCurrentEmailText()
    {
        var sample = sampleEmails[currentSampleIndex];
        return $"From: {sample[0]}\nSubject: {sample[1]}\n\n{sample[2]}";
    }

    private void TestGetSummary()
    {
        UpdateStatus("Getting summary...");
        Debug.Log("[EmailAITest] Testing GetSummary...");

        string emailText = GetCurrentEmailText();
        EmailAITool.GetSummary(emailText);
    }

    private void TestGetCategory()
    {
        UpdateStatus("Getting category...");
        Debug.Log("[EmailAITest] Testing GetCategory...");

        string emailText = GetCurrentEmailText();
        EmailAITool.GetCategory(emailText);
    }

    private void TestFullAnalysis()
    {
        UpdateStatus("Analyzing email...");
        Debug.Log("[EmailAITest] Testing Full Analysis...");

        var sample = sampleEmails[currentSampleIndex];
        EmailAITool.AnalyzeEmail(sample[0], sample[1], sample[2]);
    }

    private void NextSample()
    {
        currentSampleIndex = (currentSampleIndex + 1) % sampleEmails.Length;
        ShowCurrentSample();
    }

    private void ShowCurrentSample()
    {
        var sample = sampleEmails[currentSampleIndex];
        string display = $"Sample {currentSampleIndex + 1}/{sampleEmails.Length}\n" +
                        $"From: {sample[0]}\n" +
                        $"Subject: {sample[1]}\n" +
                        $"---\n{sample[2]}";

        UpdateResult(display);
        Debug.Log($"[EmailAITest] Current sample:\n{GetCurrentEmailText()}");
    }

    // Event handlers
    private void OnSummaryReceived(string summary)
    {
        UpdateStatus("Summary received! Speaking...");
        UpdateResult($"SUMMARY:\n{summary}");
        Debug.Log($"[EmailAITest] Summary: {summary}");

        // Speak the summary using ElevenLabs TTS
        Audio.PlaySound(summary);
    }

    private void OnCategoryReceived(EmailAITool.EmailCategory category)
    {
        UpdateStatus("Category received!");
        UpdateResult($"CATEGORY: {category}");
        Debug.Log($"[EmailAITest] Category: {category}");
    }

    private void OnAnalysisReceived(EmailAITool.EmailAnalysis analysis)
    {
        UpdateStatus("Analysis complete!");
        string result = $"FULL ANALYSIS:\n" +
                       $"Summary: {analysis.summary}\n" +
                       $"Category: {analysis.category}\n" +
                       $"Reason: {analysis.categoryReason}\n" +
                       $"Priority: {analysis.priority}/5";
        UpdateResult(result);
        Debug.Log($"[EmailAITest] Analysis:\n{result}");
    }

    private void OnError(string error)
    {
        UpdateStatus($"Error!");
        UpdateResult($"ERROR: {error}");
        Debug.LogError($"[EmailAITest] Error: {error}");
    }

    private void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void UpdateResult(string result)
    {
        if (resultText != null)
            resultText.text = result;
    }

    private void OnDestroy()
    {
        if (emailAITool != null)
        {
            emailAITool.OnSummaryComplete.RemoveListener(OnSummaryReceived);
            emailAITool.OnCategoryComplete.RemoveListener(OnCategoryReceived);
            emailAITool.OnAnalysisComplete.RemoveListener(OnAnalysisReceived);
            emailAITool.OnError.RemoveListener(OnError);
        }
    }
}
