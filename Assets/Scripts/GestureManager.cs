using System.Diagnostics;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    [SerializeField] StateManager state;
    [SerializeField] GCloudPubSubManager pubSubManager;

    void Update()
    {
        
    }
    
    public void HandleSwipeLeft()
    {
        UnityEngine.Debug.Log("SWIPING LEFT");
        if (state.focusedSphere == null) return;
        
        EmailCanvas emailCanvas = state.focusedSphere.GetComponent<EmailCanvas>();
        EmailRespond emailRespond = state.focusedSphere.GetComponentInChildren<EmailRespond>();
        
        if (emailRespond != null && emailRespond.IsDisplayingDraft)
        {
            UnityEngine.Debug.Log("SENDING EMAIL");
            
            if (pubSubManager == null)
            {
                pubSubManager = FindFirstObjectByType<GCloudPubSubManager>();
            }
            
            if (pubSubManager != null)
            {
                emailRespond.SendDraft(pubSubManager);
            }
            else
            {
                UnityEngine.Debug.LogError("[GestureManager] Cannot send email - GCloudPubSubManager not found!");
            }
        }
        else if (emailCanvas != null)
        {
            emailCanvas.replying = true;
        }
    }

    public void HandleSwipeRight()
    {
        UnityEngine.Debug.Log("SWIPING RIGHT");
        if (state.focusedSphere == null) return;
        
        EmailRespond emailRespond = state.focusedSphere.GetComponentInChildren<EmailRespond>();
        
        if (emailRespond != null && emailRespond.IsDisplayingDraft)
        {
            UnityEngine.Debug.Log("DISCARDING DRAFT");
            emailRespond.DiscardDraft();
        }
    }

    public void HandleCrunch()
    {
        UnityEngine.Debug.Log("SCRUNCH");
        if (state.focusedSphere == null) return;
        Destroy(state.focusedSphere);
        state.focusedSphere = null;
    }
}
