using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class VRTrackerController : MonoBehaviour
{
    [Header("Tracker Settings")]
    public bool isLeftTracker = true;
    public Vector3 positionOffset = new Vector3(0, -0.05f, 0); // Bilek offset'i
    public Vector3 rotationOffset = Vector3.zero;
    
    [Header("Tracker Device")]
    public string trackerSerialNumber = ""; // Opsiyonel: Belirli bir tracker'ı takip etmek için
    
    private Transform myTransform;
    private InputDevice trackerDevice;
    private bool hasValidDevice = false;
    
    // For tracking velocity
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    
    void Start()
    {
        myTransform = transform;
        previousPosition = myTransform.position;
        
        // Try to find tracker device
        FindTrackerDevice();
        
        // Listen for device changes
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }
    
    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
    
    void FindTrackerDevice()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        
        foreach (var device in devices)
        {
            if (IsTrackerDevice(device))
            {
                if (string.IsNullOrEmpty(trackerSerialNumber) || device.serialNumber == trackerSerialNumber)
                {
                    trackerDevice = device;
                    hasValidDevice = true;
                    Debug.Log($"Tracker found: {device.name} - Serial: {device.serialNumber}");
                    break;
                }
            }
        }
    }
    
    bool IsTrackerDevice(InputDevice device)
    {
        // Check if device is a tracker
        return device.name.ToLower().Contains("tracker") || 
               ((device.characteristics & InputDeviceCharacteristics.TrackedDevice) != 0 && 
                (device.characteristics & InputDeviceCharacteristics.Controller) == 0);
    }
    
    void OnDeviceConnected(InputDevice device)
    {
        if (!hasValidDevice && IsTrackerDevice(device))
        {
            if (string.IsNullOrEmpty(trackerSerialNumber) || device.serialNumber == trackerSerialNumber)
            {
                trackerDevice = device;
                hasValidDevice = true;
                Debug.Log($"Tracker connected: {device.name}");
            }
        }
    }
    
    void OnDeviceDisconnected(InputDevice device)
    {
        if (hasValidDevice && device.Equals(trackerDevice))
        {
            hasValidDevice = false;
            Debug.Log($"Tracker disconnected: {device.name}");
        }
    }
    
    void Update()
    {
        if (hasValidDevice && trackerDevice.isValid)
        {
            // Get tracker position and rotation
            Vector3 devicePosition;
            Quaternion deviceRotation;
            
            if (trackerDevice.TryGetFeatureValue(CommonUsages.devicePosition, out devicePosition))
            {
                myTransform.position = devicePosition + positionOffset;
            }
            
            if (trackerDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out deviceRotation))
            {
                myTransform.rotation = deviceRotation * Quaternion.Euler(rotationOffset);
            }
        }
        
        // Update velocity calculation
        currentVelocity = (myTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = myTransform.position;
    }
    
    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }
    
    public void SetTrackerSerialNumber(string serialNumber)
    {
        trackerSerialNumber = serialNumber;
        FindTrackerDevice(); // Re-search for the specific tracker
    }
    
    public bool IsTrackerConnected()
    {
        return hasValidDevice && trackerDevice.isValid;
    }
    
    public InputDevice GetTrackerDevice()
    {
        return trackerDevice;
    }
    
    void OnDrawGizmosSelected()
    {
        // Show tracker position
        Gizmos.color = isLeftTracker ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        
        // Show velocity
        if (Application.isPlaying && currentVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 0.5f);
        }
    }
}