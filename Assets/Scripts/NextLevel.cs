using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevel : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private Transform ovrCameraRig;
    [SerializeField] private Collider triggerCollider;
    
    [Header("Timer Reference")]
    [SerializeField] private BedTimer bedTimer;
    [SerializeField] private bool waitForBedTimer = false; // Checkbox to control bed timer dependency
    
    [Header("Scene Transition")]
    [SerializeField] private bool useSceneTransition = true;
    [SerializeField] private string playerTag = "Player"; // Tag to identify player
    
    [Header("Eye Closing Animation")]
    [SerializeField] private float eyeCloseDelay = 0.5f; // Delay before eyes start closing
    [SerializeField] private float eyeCloseDuration = 2f; // How long the closing animation takes
    [SerializeField] private Color fadeColor = Color.black; // Color to fade to
    
    private bool timerStarted = false;
    private bool sceneTransitionTriggered = false;
    private GameObject eyeCloseOverlay;
    private Material eyeCloseMaterial;
    private Camera vrCamera;
    private bool animationInProgress = false;
    
    void Start()
    {
        Debug.Log("NextLevel: Script started successfully!");
        
        // Ensure trigger collider is set up properly
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            Debug.Log("NextLevel: Trigger collider setup complete");
        }
        else
        {
            Debug.LogError("NextLevel: No trigger collider assigned!");
        }
        
        // Setup eye closing overlay
        SetupEyeCloseOverlay();
        
        Debug.Log($"NextLevel: Setup complete. Current scene index: {SceneManager.GetActiveScene().buildIndex}");
    }
    
    void Update()
    {
        // Check if OVR Camera Rig is inside the trigger and start eye closing animation
        if (!sceneTransitionTriggered && !animationInProgress && IsOVRCameraRigInside())
        {
            StartEyeClosingAnimation();
        }
        
        // If we're waiting for bed timer and player is in trigger, keep checking timer status
        if (!sceneTransitionTriggered && !animationInProgress && waitForBedTimer && 
            bedTimer != null && IsOVRCameraRigInside() && bedTimer.IsTimerComplete())
        {
            Debug.Log("NextLevel: Bed timer completed while player in trigger, starting animation");
            StartEyeClosingAnimation();
        }
    }
    
    private bool IsOVRCameraRigInside()
    {
        if (ovrCameraRig == null || triggerCollider == null)
        {
            Debug.LogWarning("NextLevel: Missing OVR Camera Rig or Trigger Collider reference!");
            return false;
        }
            
        // Check if the OVR Camera Rig position is inside the trigger collider bounds
        bool isInside = triggerCollider.bounds.Contains(ovrCameraRig.position);
        
        if (isInside)
        {
            Debug.Log("NextLevel: Camera rig detected inside trigger bounds!");
        }
        
        return isInside;
    }
    
    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentSceneIndex + 1;
        
        Debug.Log($"NextLevel: Attempting to load next scene. Current: {currentSceneIndex}, Next: {nextIndex}, Total scenes: {SceneManager.sceneCountInBuildSettings}");
        
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("Loading next scene index: " + nextIndex);
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.LogWarning("No next scene to load! This is the last scene in build order.");
        }
    }
    
    private Camera FindVRCamera()
    {
        // Try to find OVR Center Eye Anchor camera first
        GameObject centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            Camera cam = centerEye.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        
        // Fallback to main camera
        return Camera.main;
    }
    
    private void SetupEyeCloseOverlay()
    {
        // Find the VR camera
        vrCamera = FindVRCamera();
        
        if (vrCamera == null)
        {
            Debug.LogError("NextLevel: Could not find VR camera for eye closing animation!");
            return;
        }
        
        // Create a quad to display the eye closing overlay
        eyeCloseOverlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        eyeCloseOverlay.name = "Eye Close Overlay";
        
        // Remove the collider (we don't need it)
        Destroy(eyeCloseOverlay.GetComponent<Collider>());
        
        // Position it in front of the camera
        eyeCloseOverlay.transform.SetParent(vrCamera.transform);
        eyeCloseOverlay.transform.localPosition = new Vector3(0, 0, 0.3f);
        eyeCloseOverlay.transform.localRotation = Quaternion.identity;
        
        // Scale it to cover VR field of view completely
        float distance = 0.3f;
        float height = 2.0f * distance * Mathf.Tan(vrCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * vrCamera.aspect;
        eyeCloseOverlay.transform.localScale = new Vector3(width * 1.5f, height * 1.5f, 1f);
        
        // Create material for the overlay
        eyeCloseMaterial = new Material(Shader.Find("Sprites/Default"));
        if (eyeCloseMaterial.shader == null || eyeCloseMaterial.shader.name == "Hidden/InternalErrorShader")
        {
            eyeCloseMaterial = new Material(Shader.Find("Mobile/Diffuse"));
        }
        
        eyeCloseMaterial.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Start transparent
        
        // Ensure the material renders properly in VR
        eyeCloseMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        eyeCloseMaterial.SetInt("_ZWrite", 0);
        eyeCloseMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        
        // Apply material to quad
        eyeCloseOverlay.GetComponent<Renderer>().material = eyeCloseMaterial;
        
        // Initially hide the overlay
        eyeCloseOverlay.SetActive(false);
        
        Debug.Log("Eye closing overlay setup complete");
    }
    
    private void StartEyeClosingAnimation()
    {
        // Check if we need to wait for bed timer
        if (waitForBedTimer && bedTimer != null)
        {
            if (!bedTimer.IsTimerComplete())
            {
                Debug.Log("NextLevel: Waiting for bed timer to complete before proceeding...");
                return; // Don't start animation yet, timer not complete
            }
            else
            {
                Debug.Log("NextLevel: Bed timer complete, proceeding with scene transition");
            }
        }
        else if (waitForBedTimer && bedTimer == null)
        {
            Debug.LogWarning("NextLevel: Wait for bed timer is enabled but no BedTimer reference found!");
        }
        
        sceneTransitionTriggered = true;
        animationInProgress = true;
        
        Debug.Log("Starting eye closing animation");
        
        // Show the overlay
        if (eyeCloseOverlay != null)
        {
            eyeCloseOverlay.SetActive(true);
        }
        
        // Start the animation coroutine
        StartCoroutine(EyeCloseAnimationCoroutine());
    }
    
    private IEnumerator EyeCloseAnimationCoroutine()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(eyeCloseDelay);
        
        // Fade to black over the specified duration
        float elapsedTime = 0f;
        
        while (elapsedTime < eyeCloseDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / eyeCloseDuration);
            
            if (eyeCloseMaterial != null)
            {
                eyeCloseMaterial.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            }
            
            yield return null;
        }
        
        // Ensure final alpha is 1
        if (eyeCloseMaterial != null)
        {
            eyeCloseMaterial.color = fadeColor;
        }
        
        Debug.Log("Eye closing animation complete, loading next scene");
        
        // Small delay to ensure animation is visible
        yield return new WaitForSeconds(0.1f);
        
        // Load the next scene
        LoadNextScene();
    }
    
    // Collision detection for scene transition
    void OnTriggerEnter(Collider other)
    {
        if (!useSceneTransition || sceneTransitionTriggered)
            return;
            
        // Check if the collider belongs to the player
        if (IsPlayer(other))
        {
            sceneTransitionTriggered = true;
            LoadNextScene();
        }
    }
    
    private bool IsPlayer(Collider other)
    {
        // Check by tag
        if (other.CompareTag(playerTag))
            return true;
            
        // Check if it's part of OVR Camera Rig
        if (ovrCameraRig != null && other.transform.IsChildOf(ovrCameraRig))
            return true;
            
        // Check common VR player names
        string objName = other.name.ToLower();
        return objName.Contains("player") || 
               objName.Contains("ovrcamerarig") || 
               objName.Contains("centereyeanchor") ||
               objName.Contains("trackingspace");
    }
    

}