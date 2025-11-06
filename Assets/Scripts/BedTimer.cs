using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BedTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float timerDuration = 10f; // Changed to 10 seconds for testing
    
    void Awake()
    {
        Debug.Log("BEDTIMER AWAKE - SCRIPT IS WORKING!");
        print("BEDTIMER PRINT - SCRIPT IS WORKING!");
    }
    
    [Header("Target Collider")]
    [SerializeField] private BoxCollider targetBoxCollider;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip timerFinishedSound;
    
    private float currentTime;
    private bool timerActive = false;
    private bool timerStarted = false;
    
    void Start()
    {
        Debug.Log("BedTimer: Script started successfully!");
        currentTime = timerDuration;
        
        // Make sure the box collider is disabled at start
        if (targetBoxCollider != null)
        {
            targetBoxCollider.enabled = false;
            Debug.Log("BedTimer: Target collider disabled at start");
        }
        else
        {
            Debug.LogError("BedTimer: No target collider assigned!");
        }
        
        Debug.Log($"BedTimer: Ready to start timer. Duration set to {timerDuration} seconds");
        
        // TEMPORARY AUTO-START FOR TESTING - REMOVE THIS AFTER TESTING
        StartTimer();
    }
    
    void Update()
    {
        if (timerActive && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            
            // Debug: Show remaining time every second
            if (Mathf.FloorToInt(currentTime) != Mathf.FloorToInt(currentTime + Time.deltaTime))
            {
                Debug.Log($"Timer: {Mathf.FloorToInt(currentTime)} seconds remaining");
            }
            
            // Check if timer has finished
            if (currentTime <= 0)
            {
                TimerFinished();
            }
        }
    }
    
    private void TimerFinished()
    {
        timerActive = false;
        
        // Play the timer finished sound
        if (audioSource != null && timerFinishedSound != null)
        {
            audioSource.PlayOneShot(timerFinishedSound);
        }
        else if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource assigned to BedTimer!");
        }
        else if (timerFinishedSound == null)
        {
            Debug.LogWarning("No timer finished sound clip assigned to BedTimer!");
        }
        
        // Enable the box collider
        if (targetBoxCollider != null)
        {
            targetBoxCollider.enabled = true;
            Debug.Log("Timer finished! Box collider enabled.");
        }
        else
        {
            Debug.LogWarning("No BoxCollider assigned to BedTimer!");
        }
    }
    
    // Public method to start the timer (called from NextLevel script)
    public void StartTimer()
    {
        if (!timerStarted)
        {
            timerStarted = true;
            timerActive = true;
            Debug.Log($"Bed timer started! Duration: {timerDuration} seconds");
        }
        else
        {
            Debug.Log("Timer already started!");
        }
    }
    
    // Optional: Reset the timer if you want to restart it
    // Optional: Reset the timer if you want to restart it
    public void ResetTimer()
    {
        currentTime = timerDuration;
        timerActive = false;
        timerStarted = false;
        
        if (targetBoxCollider != null)
        {
            targetBoxCollider.enabled = false;
        }
    }
    
    // Public method to check if timer is complete
    public bool IsTimerComplete()
    {
        return timerStarted && !timerActive && currentTime <= 0;
    }
}