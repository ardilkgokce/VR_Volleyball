using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public enum VRInputDevice
{
    Controller,
    ViveTracker
}

[System.Serializable]
public class VRInputSettings
{
    public VRInputDevice inputDevice = VRInputDevice.Controller;
    public bool useTrackersForHands = false;
    public float trackerOffsetY = -0.05f; // Bilek pozisyonu için offset
}

public class VRInputManager : MonoBehaviour
{
    private static VRInputManager instance;
    public static VRInputManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VRInputManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VRInputManager");
                    instance = go.AddComponent<VRInputManager>();
                }
            }
            return instance;
        }
    }

    [Header("Input Settings")]
    public VRInputSettings settings = new VRInputSettings();

    [Header("Device References")]
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform leftTrackerTransform;
    public Transform rightTrackerTransform;

    [Header("Active Devices")]
    private InputDevice leftController;
    private InputDevice rightController;
    private InputDevice leftTracker;
    private InputDevice rightTracker;

    private bool hasLeftController = false;
    private bool hasRightController = false;
    private bool hasLeftTracker = false;
    private bool hasRightTracker = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        
        // Mevcut cihazları kontrol et
        CheckExistingDevices();
    }

    private void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    private void CheckExistingDevices()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach (var device in devices)
        {
            OnDeviceConnected(device);
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        Debug.Log($"Device connected: {device.name} - Characteristics: {device.characteristics}");

        // Controller kontrolü
        if ((device.characteristics & InputDeviceCharacteristics.Controller) != 0)
        {
            if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
            {
                leftController = device;
                hasLeftController = true;
                Debug.Log("Left controller connected");
            }
            else if ((device.characteristics & InputDeviceCharacteristics.Right) != 0)
            {
                rightController = device;
                hasRightController = true;
                Debug.Log("Right controller connected");
            }
        }
        // Tracker kontrolü (Generic tracker olarak tanımlanır)
        else if ((device.characteristics & InputDeviceCharacteristics.TrackedDevice) != 0 && 
                 device.name.ToLower().Contains("tracker"))
        {
            AssignTracker(device);
        }
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (device.Equals(leftController))
        {
            hasLeftController = false;
        }
        else if (device.Equals(rightController))
        {
            hasRightController = false;
        }
        else if (device.Equals(leftTracker))
        {
            hasLeftTracker = false;
        }
        else if (device.Equals(rightTracker))
        {
            hasRightTracker = false;
        }
    }

    private void AssignTracker(InputDevice tracker)
    {
        // İlk gelen tracker'ı sol, ikincisini sağ olarak ata
        // Gerçek uygulamada tracker serial numarası veya pozisyona göre atama yapılabilir
        if (!hasLeftTracker)
        {
            leftTracker = tracker;
            hasLeftTracker = true;
            Debug.Log("Left tracker assigned");
        }
        else if (!hasRightTracker)
        {
            rightTracker = tracker;
            hasRightTracker = true;
            Debug.Log("Right tracker assigned");
        }
    }

    public bool GetHandPosition(XRNode hand, out Vector3 position)
    {
        position = Vector3.zero;

        if (settings.inputDevice == VRInputDevice.ViveTracker && settings.useTrackersForHands)
        {
            return GetTrackerPosition(hand, out position);
        }
        else
        {
            return GetControllerPosition(hand, out position);
        }
    }

    public bool GetHandRotation(XRNode hand, out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (settings.inputDevice == VRInputDevice.ViveTracker && settings.useTrackersForHands)
        {
            return GetTrackerRotation(hand, out rotation);
        }
        else
        {
            return GetControllerRotation(hand, out rotation);
        }
    }

    public bool GetHandVelocity(XRNode hand, out Vector3 velocity)
    {
        velocity = Vector3.zero;

        if (settings.inputDevice == VRInputDevice.ViveTracker && settings.useTrackersForHands)
        {
            return GetTrackerVelocity(hand, out velocity);
        }
        else
        {
            return GetControllerVelocity(hand, out velocity);
        }
    }

    private bool GetControllerPosition(XRNode hand, out Vector3 position)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftController : rightController;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
        {
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    private bool GetControllerRotation(XRNode hand, out Quaternion rotation)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftController : rightController;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
        {
            return true;
        }
        rotation = Quaternion.identity;
        return false;
    }

    private bool GetControllerVelocity(XRNode hand, out Vector3 velocity)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftController : rightController;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
        {
            return true;
        }
        velocity = Vector3.zero;
        return false;
    }

    private bool GetTrackerPosition(XRNode hand, out Vector3 position)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftTracker : rightTracker;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
        {
            // Bilek offset'i uygula
            position.y += settings.trackerOffsetY;
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    private bool GetTrackerRotation(XRNode hand, out Quaternion rotation)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftTracker : rightTracker;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
        {
            return true;
        }
        rotation = Quaternion.identity;
        return false;
    }

    private bool GetTrackerVelocity(XRNode hand, out Vector3 velocity)
    {
        InputDevice device = hand == XRNode.LeftHand ? leftTracker : rightTracker;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceVelocity, out velocity))
        {
            return true;
        }
        velocity = Vector3.zero;
        return false;
    }

    public void SendHapticFeedback(XRNode hand, float amplitude, float duration)
    {
        if (settings.inputDevice == VRInputDevice.Controller)
        {
            InputDevice device = hand == XRNode.LeftHand ? leftController : rightController;
            if (device.isValid)
            {
                HapticCapabilities capabilities;
                if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0, amplitude, duration);
                }
            }
        }
        // Tracker'lar haptic feedback desteklemez
    }

    // Cihaz durumu sorgulama
    public bool IsDeviceConnected(XRNode hand)
    {
        if (settings.inputDevice == VRInputDevice.ViveTracker && settings.useTrackersForHands)
        {
            return hand == XRNode.LeftHand ? hasLeftTracker : hasRightTracker;
        }
        else
        {
            return hand == XRNode.LeftHand ? hasLeftController : hasRightController;
        }
    }

    public void SetInputDevice(VRInputDevice device, bool useTrackers)
    {
        settings.inputDevice = device;
        settings.useTrackersForHands = useTrackers;
        
        // UI veya diğer sistemlere değişiklik bildirimi
        OnInputDeviceChanged?.Invoke(device, useTrackers);
    }

    public event Action<VRInputDevice, bool> OnInputDeviceChanged;
}