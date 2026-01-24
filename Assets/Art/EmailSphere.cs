using System;
using UnityEngine;
using TMPro;
using System.Diagnostics;

public class EmailSphere : MonoBehaviour
{
    public String subject;
    public String body;
    public String sender;

    [SerializeField] private TextMeshProUGUI fullContent;
    [SerializeField] private TextMeshProUGUI aiContent;

    public void Initialize(string sender, string subject, string body)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;

        UpdateText();
    }

    private void UpdateText()
    {
        string content = $"From: {sender}\nSubject: {subject}\n\n{body}";
        UnityEngine.Debug.Log(content);
        if (fullContent != null) fullContent.text = content;
        if (aiContent != null) aiContent.text = content;
    }
}
