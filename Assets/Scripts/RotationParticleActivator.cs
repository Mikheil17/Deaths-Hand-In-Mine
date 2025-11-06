using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationParticleActivator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float minXRotation = 30f; // Minimum X rotation in degrees (positive or negative)
    [SerializeField] private float maxXRotation = 60f; // Maximum X rotation in degrees (positive or negative)
    [SerializeField] private bool useAbsoluteRotation = false; // If true, checks absolute rotation; if false, checks relative to start
    
    [Header("Particle System")]
    [SerializeField] private ParticleSystem targetParticleSystem; // Particle system to activate
    [SerializeField] private bool createParticleSystemIfNull = true; // Auto-create particle system if none assigned
    
    [Header("Activation Settings")]
    [SerializeField] private bool continuousToggle = true; // If true, continuously enables/disables based on rotation
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Vector3 initialRotation;
    private bool particleSystemActive = false;
    
    void Start()
    {
        // Store initial rotation for relative calculations
        initialRotation = transform.eulerAngles;
        
        // Setup particle system if needed
        SetupParticleSystem();
        
        if (showDebugInfo)
        {
            Debug.Log($"RotationParticleActivator: Initialized on {gameObject.name}");
            Debug.Log($"Initial rotation: {initialRotation}");
            Debug.Log($"X rotation range: {minXRotation}° to {maxXRotation}°");
        }
    }
    
    void Update()
    {
        CheckRotationAndActivateParticles();
    }
    
    private void SetupParticleSystem()
    {
        // If no particle system assigned and auto-create is enabled
        if (targetParticleSystem == null && createParticleSystemIfNull)
        {
            GameObject particleObj = new GameObject("Auto-Created Particle System");
            
            // Set position to object center and parent to this object
            particleObj.transform.position = transform.position;
            particleObj.transform.SetParent(transform);
            
            // Add and configure particle system
            targetParticleSystem = particleObj.AddComponent<ParticleSystem>();
            
            // Basic particle system configuration
            var main = targetParticleSystem.main;
            main.startLifetime = 2f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = Color.yellow;
            main.maxParticles = 100;
            
            var emission = targetParticleSystem.emission;
            emission.rateOverTime = 50f;
            
            var shape = targetParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
            
            // Start enabled initially (will be disabled when in range)
            particleObj.SetActive(true);
            
            if (showDebugInfo)
            {
                Debug.Log("RotationParticleActivator: Auto-created particle system at object center");
            }
        }
    }
    
    private void CheckRotationAndActivateParticles()
    {
        if (targetParticleSystem == null) return;
        
        float currentXRotation;
        
        // Calculate rotation based on settings
        if (useAbsoluteRotation)
        {
            // Use absolute world rotation
            currentXRotation = NormalizeAngle(transform.eulerAngles.x);
        }
        else
        {
            // Use relative rotation from start
            float currentAbsolute = NormalizeAngle(transform.eulerAngles.x);
            float initialNormalized = NormalizeAngle(initialRotation.x);
            currentXRotation = NormalizeAngle(currentAbsolute - initialNormalized);
        }
        
        // Check if current rotation is within the specified range
        bool inTargetRange = IsRotationInRange(currentXRotation, minXRotation, maxXRotation);
        
        // Handle activation logic - Enable/Disable based on rotation range
        if (continuousToggle)
        {
            if (!inTargetRange && !particleSystemActive)
            {
                EnableParticleSystem();
            }
            else if (inTargetRange && particleSystemActive)
            {
                DisableParticleSystem();
            }
        }
        
        // Debug info
        if (showDebugInfo && Time.frameCount % 30 == 0) // Update debug every 30 frames to avoid spam
        {
            Debug.Log($"Current X rotation: {currentXRotation:F1}°, Range: {minXRotation:F1}° to {maxXRotation:F1}°, " +
                     $"In range: {inTargetRange}, Particles active: {particleSystemActive} (inverted logic)");
        }
    }
    
    private void EnableParticleSystem()
    {
        if (targetParticleSystem != null)
        {
            targetParticleSystem.gameObject.SetActive(true);
            particleSystemActive = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"RotationParticleActivator: Particle system enabled! X rotation: {GetCurrentXRotation():F1}°");
            }
        }
    }
    
    private void DisableParticleSystem()
    {
        if (targetParticleSystem != null)
        {
            targetParticleSystem.gameObject.SetActive(false);
            particleSystemActive = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"RotationParticleActivator: Particle system disabled! X rotation: {GetCurrentXRotation():F1}°");
            }
        }
    }
    
    // Normalize angle to -180 to 180 range for easier calculations
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
    
    // Check if rotation is within the specified range, handling angle wrapping
    private bool IsRotationInRange(float currentRotation, float minRotation, float maxRotation)
    {
        // Normalize all angles
        currentRotation = NormalizeAngle(currentRotation);
        minRotation = NormalizeAngle(minRotation);
        maxRotation = NormalizeAngle(maxRotation);
        
        // If min > max, the range crosses the -180/180 boundary
        if (minRotation > maxRotation)
        {
            // Range crosses boundary (e.g., 170° to -170°)
            return currentRotation >= minRotation || currentRotation <= maxRotation;
        }
        else
        {
            // Normal range (e.g., 30° to 60°)
            return currentRotation >= minRotation && currentRotation <= maxRotation;
        }
    }
    
    // Public methods for external control
    public void ForceEnable()
    {
        EnableParticleSystem();
    }
    
    public void ForceDisable()
    {
        DisableParticleSystem();
    }
    
    public void SetRotationRange(float newMinRotation, float newMaxRotation)
    {
        minXRotation = newMinRotation;
        maxXRotation = newMaxRotation;
        if (showDebugInfo)
        {
            Debug.Log($"RotationParticleActivator: Rotation range changed to {minXRotation}° - {maxXRotation}°");
        }
    }
    
    public void SetMinRotation(float newMinRotation)
    {
        minXRotation = newMinRotation;
        if (showDebugInfo)
        {
            Debug.Log($"RotationParticleActivator: Min rotation changed to {minXRotation}°");
        }
    }
    
    public void SetMaxRotation(float newMaxRotation)
    {
        maxXRotation = newMaxRotation;
        if (showDebugInfo)
        {
            Debug.Log($"RotationParticleActivator: Max rotation changed to {maxXRotation}°");
        }
    }
    
    // Get current rotation info
    public float GetCurrentXRotation()
    {
        if (useAbsoluteRotation)
        {
            return NormalizeAngle(transform.eulerAngles.x);
        }
        else
        {
            float currentAbsolute = NormalizeAngle(transform.eulerAngles.x);
            float initialNormalized = NormalizeAngle(initialRotation.x);
            return NormalizeAngle(currentAbsolute - initialNormalized);
        }
    }
    
    public bool IsInTargetRange()
    {
        return IsRotationInRange(GetCurrentXRotation(), minXRotation, maxXRotation);
    }
    
    public float GetMinRotation()
    {
        return minXRotation;
    }
    
    public float GetMaxRotation()
    {
        return maxXRotation;
    }
}