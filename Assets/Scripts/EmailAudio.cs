using UnityEngine;

public class EmailAudio : MonoBehaviour
{
    private AudioClip cachedAudioClip;

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
}
