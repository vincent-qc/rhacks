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

    private AudioClip cachedAudioClip;

    public void Initialize(string sender, string subject, string body)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;

        UpdateText();
    }

    public void Initialize(string sender, string subject, string body, AudioClip audioClip)
    {
        this.sender = sender;
        this.subject = subject;
        this.body = body;
        this.cachedAudioClip = audioClip;

        UpdateText();
    }

    public void SetAudioClip(AudioClip clip)
    {
        cachedAudioClip = clip;
    }

    public void PlayCachedAudio()
    {
        if (cachedAudioClip != null)
        {
            Audio.PlayClipIfNotPlaying(cachedAudioClip);
        }
        else
        {
            UnityEngine.Debug.LogWarning("No cached audio clip available for this email sphere.");
        }
    }

    public bool HasCachedAudio()
    {
        return cachedAudioClip != null;
    }

    private void UpdateText()
    {
        string content = $"From: {sender}\nSubject: {subject}\n\n{body}";
        UnityEngine.Debug.Log(content);
        if (fullContent != null) fullContent.text = content;
        if (aiContent != null) aiContent.text = content;
    }
}
