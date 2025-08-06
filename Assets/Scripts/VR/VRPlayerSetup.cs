using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class VRPlayerSetup : MonoBehaviour
{
    [Header("VR Rig References")]
    public XROrigin xrOrigin;
    public Transform leftHandAnchor;
    public Transform rightHandAnchor;
    
    [Header("Tracker Prefabs")]
    public GameObject trackerPrefab; // Tracker controller prefab'i
    
    [Header("Input Configuration")]
    public bool useViveTrackers = false;
    
    private VRHandController leftHandController;
    private VRHandController rightHandController;
    private VRTrackerController leftTrackerController;
    private VRTrackerController rightTrackerController;
    
    void Start()
    {
        // XR Origin'i bul
        if (xrOrigin == null)
            xrOrigin = GetComponentInChildren<XROrigin>();
            
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found!");
            return;
        }
        
        // Hand anchor'ları bul
        if (leftHandAnchor == null || rightHandAnchor == null)
        {
            var controllers = xrOrigin.GetComponentsInChildren<XRController>();
            foreach (var controller in controllers)
            {
                if (controller.controllerNode == XRNode.LeftHand)
                    leftHandAnchor = controller.transform;
                else if (controller.controllerNode == XRNode.RightHand)
                    rightHandAnchor = controller.transform;
            }
        }
        
        // VRInputManager'ı kontrol et
        if (VRInputManager.Instance == null)
        {
            GameObject inputManagerGO = new GameObject("VRInputManager");
            inputManagerGO.AddComponent<VRInputManager>();
        }
        
        // Input cihazına göre setup yap
        SetupInputDevice();
    }
    
    void SetupInputDevice()
    {
        if (useViveTrackers)
        {
            SetupViveTrackers();
        }
        else
        {
            SetupControllers();
        }
        
        // VRInputManager'a bildir
        VRInputManager.Instance.SetInputDevice(
            useViveTrackers ? VRInputDevice.ViveTracker : VRInputDevice.Controller,
            useViveTrackers
        );
    }
    
    void SetupControllers()
    {
        // Sol el controller setup
        if (leftHandAnchor != null)
        {
            leftHandController = leftHandAnchor.GetComponentInChildren<VRHandController>();
            if (leftHandController == null)
            {
                GameObject handGO = new GameObject("LeftHandCollider");
                handGO.transform.SetParent(leftHandAnchor);
                handGO.transform.localPosition = Vector3.zero;
                handGO.transform.localRotation = Quaternion.identity;
                
                leftHandController = handGO.AddComponent<VRHandController>();
                leftHandController.isLeftHand = true;
            }
            
            VRInputManager.Instance.leftHandTransform = leftHandController.transform;
        }
        
        // Sağ el controller setup
        if (rightHandAnchor != null)
        {
            rightHandController = rightHandAnchor.GetComponentInChildren<VRHandController>();
            if (rightHandController == null)
            {
                GameObject handGO = new GameObject("RightHandCollider");
                handGO.transform.SetParent(rightHandAnchor);
                handGO.transform.localPosition = Vector3.zero;
                handGO.transform.localRotation = Quaternion.identity;
                
                rightHandController = handGO.AddComponent<VRHandController>();
                rightHandController.isLeftHand = false;
            }
            
            VRInputManager.Instance.rightHandTransform = rightHandController.transform;
        }
    }
    
    void SetupViveTrackers()
    {
        // Tracker prefab yoksa basit bir GameObject oluştur
        if (trackerPrefab == null)
        {
            // Sol tracker
            GameObject leftTracker = new GameObject("LeftViveTracker");
            leftTracker.transform.SetParent(transform);
            leftTrackerController = leftTracker.AddComponent<VRTrackerController>();
            leftTrackerController.isLeftTracker = true;
            
            // VRHandController ekle (tracker'ı el gibi kullanmak için)
            leftHandController = leftTracker.AddComponent<VRHandController>();
            leftHandController.isLeftHand = true;
            
            // Sağ tracker
            GameObject rightTracker = new GameObject("RightViveTracker");
            rightTracker.transform.SetParent(transform);
            rightTrackerController = rightTracker.AddComponent<VRTrackerController>();
            rightTrackerController.isLeftTracker = false;
            
            // VRHandController ekle
            rightHandController = rightTracker.AddComponent<VRHandController>();
            rightHandController.isLeftHand = false;
        }
        else
        {
            // Prefab'dan oluştur
            GameObject leftTracker = Instantiate(trackerPrefab, transform);
            leftTracker.name = "LeftViveTracker";
            leftTrackerController = leftTracker.GetComponent<VRTrackerController>();
            if (leftTrackerController != null)
                leftTrackerController.isLeftTracker = true;
                
            GameObject rightTracker = Instantiate(trackerPrefab, transform);
            rightTracker.name = "RightViveTracker";
            rightTrackerController = rightTracker.GetComponent<VRTrackerController>();
            if (rightTrackerController != null)
                rightTrackerController.isLeftTracker = false;
        }
        
        // VRInputManager'a tracker transform'larını bildir
        if (leftTrackerController != null)
            VRInputManager.Instance.leftTrackerTransform = leftTrackerController.transform;
        if (rightTrackerController != null)
            VRInputManager.Instance.rightTrackerTransform = rightTrackerController.transform;
    }
    
    public void SwitchToControllers()
    {
        useViveTrackers = false;
        
        // Tracker'ları devre dışı bırak
        if (leftTrackerController != null)
            leftTrackerController.gameObject.SetActive(false);
        if (rightTrackerController != null)
            rightTrackerController.gameObject.SetActive(false);
            
        // Controller'ları aktif et
        if (leftHandController != null)
            leftHandController.gameObject.SetActive(true);
        if (rightHandController != null)
            rightHandController.gameObject.SetActive(true);
            
        SetupControllers();
    }
    
    public void SwitchToTrackers()
    {
        useViveTrackers = true;
        
        // Controller'ları devre dışı bırak (anchor'lardaki)
        if (leftHandAnchor != null)
        {
            var controller = leftHandAnchor.GetComponentInChildren<VRHandController>();
            if (controller != null && controller != leftHandController)
                controller.gameObject.SetActive(false);
        }
        if (rightHandAnchor != null)
        {
            var controller = rightHandAnchor.GetComponentInChildren<VRHandController>();
            if (controller != null && controller != rightHandController)
                controller.gameObject.SetActive(false);
        }
        
        SetupViveTrackers();
    }
}