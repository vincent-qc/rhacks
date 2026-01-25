using System.Diagnostics;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
  [SerializeField] StateManager state;

  // Update is called once per frame
  void Update()
    {
        
    }
    
    public void HandleSwipeLeft()
    {
    UnityEngine.Debug.Log("SWIPING LEFT");
    if (state.focusedSphere == null) return;
      EmailCanvas emailCanvas = state.focusedSphere.GetComponent<EmailCanvas>();
      if (emailCanvas != null)
      {
          emailCanvas.replying = true;
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
