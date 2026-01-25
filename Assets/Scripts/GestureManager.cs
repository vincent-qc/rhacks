using System.Diagnostics;
using UnityEngine;

public class EmailGestures : MonoBehaviour
{
  [SerializeField] StateManager state;

  // Update is called once per frame
  void Update()
    {
        
    }
    
    public void HandleSwipeLeft()
    {
      UnityEngine.Debug.Log("SWIPE");
    }

    public void StartFist()
    {
      state.isFist = true;
    }

    public void EndFist()
    {
      state.isFist = false;
    }
}
