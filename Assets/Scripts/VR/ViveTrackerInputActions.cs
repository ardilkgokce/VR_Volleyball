using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

// Custom Input Action setup for Vive Trackers
[CreateAssetMenu(fileName = "ViveTrackerInputActions", menuName = "VR/Vive Tracker Input Actions")]
public class ViveTrackerInputActions : ScriptableObject
{
    // Tracker position actions
    public InputActionReference leftTrackerPosition;
    public InputActionReference rightTrackerPosition;
    
    // Tracker rotation actions
    public InputActionReference leftTrackerRotation;
    public InputActionReference rightTrackerRotation;
    
    // Tracker tracking state
    public InputActionReference leftTrackerTrackingState;
    public InputActionReference rightTrackerTrackingState;
    
    // Helper method to create input actions at runtime
    public static InputActionMap CreateTrackerActionMap()
    {
        var actionMap = new InputActionMap("ViveTrackers");
        
        // Left Tracker Actions
        var leftPosition = actionMap.AddAction("LeftTrackerPosition", InputActionType.Value);
        leftPosition.AddBinding("<XRController>{LeftHand}/devicePosition");
        leftPosition.AddBinding("<ViveTracker>/devicePosition");
        
        var leftRotation = actionMap.AddAction("LeftTrackerRotation", InputActionType.Value);
        leftRotation.AddBinding("<XRController>{LeftHand}/deviceRotation");
        leftRotation.AddBinding("<ViveTracker>/deviceRotation");
        
        // Right Tracker Actions
        var rightPosition = actionMap.AddAction("RightTrackerPosition", InputActionType.Value);
        rightPosition.AddBinding("<XRController>{RightHand}/devicePosition");
        rightPosition.AddBinding("<ViveTracker>/devicePosition");
        
        var rightRotation = actionMap.AddAction("RightTrackerRotation", InputActionType.Value);
        rightRotation.AddBinding("<XRController>{RightHand}/deviceRotation");
        rightRotation.AddBinding("<ViveTracker>/deviceRotation");
        
        return actionMap;
    }
}