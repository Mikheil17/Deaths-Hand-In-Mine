using UnityEngine;
using System.Collections;

public class PhoneRingingSystem : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float timerDuration = 10f; // Time before phone starts ringing
    [SerializeField] private bool startTimerOnStart = true;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip ringingClip; // First sound clip (ringing)
    [SerializeField] private AudioClip pickupClip; // Second sound clip (when picked up)
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float ringingVolume = 0.8f;
    [SerializeField] private float pickupVolume = 1f;
    [SerializeField] private int maxRingIterations = 2; // Play ringing clip exactly twice
    
    [Header("OVR Hand Detection")]
    [SerializeField] private bool detectLeftHand = true;
    [SerializeField] private bool detectRightHand = true;
    [SerializeField] private string leftHandAnchorName = "LeftHandAnchor";
    [SerializeField] private string rightHandAnchorName = "RightHandAnchor";
    [SerializeField] private float grabDistance = 0.3f; // Distance to detect grab
    
    [Header("Alternative Hand Detection")]
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;
    
    [Header("Pickup Settings")]
    [SerializeField] private float pickupDelay = 3f; // Wait time before playing pickup clip
    
    [Header("3D Audio Settings")]
    [SerializeField] private float ringingMinDistance = 1f; // Normal min distance for ringing
    [SerializeField] private float ringingMaxDistance = 500f; // Normal max distance for ringing
    [SerializeField] private float pickupMinDistance = 0.1f; // Close min distance for pickup
    [SerializeField] private float pickupMaxDistance = 0.2f; // Close max distance for pickup
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGrabDistance = true;
    
    // Private variables
    private float currentTimer = 0f;
    private bool isTimerRunning = false;
    private bool isRinging = false;
    private bool hasBeenPickedUp = false;
    private bool isPickupSequenceActive = false;
    private int currentRingIteration = 0;
    private bool canPlayPickupClip = false; // Only true during first two ring iterations
    
    // Coroutine references
    private Coroutine timerCoroutine;
    private Coroutine pickupCoroutine;
    private Coroutine ringingCoroutine;
    
    void Start()
    {
        DebugLog("PhoneRingingSystem: Script started");
        
        // Setup audio source
        SetupAudioSource();
        
        // Auto-find hand anchors if not assigned
        FindHandAnchors();
        
        // Ensure this GameObject has a collider for grab detection
        SetupCollider();
        
        // Start timer if enabled
        if (startTimerOnStart)
        {
            StartTimer();
        }
    }
    
    void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                DebugLog("PhoneRingingSystem: Created AudioSource component");
            }
        }
        
        // Configure audio source
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.loop = false; // We'll handle iterations manually
        
        // Set up 3D audio rolloff for ringing (normal distances)
        SetupRingingAudio();
        
        if (ringingClip == null)
        {
            Debug.LogWarning("PhoneRingingSystem: No ringing clip assigned!");
        }
        
        if (pickupClip == null)
        {
            Debug.LogWarning("PhoneRingingSystem: No pickup clip assigned!");
        }
    }
    
    void SetupCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Add a sphere collider for grab detection
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = grabDistance;
            DebugLog("PhoneRingingSystem: Added SphereCollider for grab detection");
        }
        else
        {
            col.isTrigger = true;
            DebugLog("PhoneRingingSystem: Existing collider set as trigger");
        }
    }
    
    void FindHandAnchors()
    {
        if (leftHandAnchor == null && detectLeftHand)
        {
            GameObject leftHand = GameObject.Find(leftHandAnchorName);
            if (leftHand != null)
            {
                leftHandAnchor = leftHand.transform;
                DebugLog($"PhoneRingingSystem: Found left hand anchor: {leftHand.name}");
            }
            else
            {
                DebugLog("PhoneRingingSystem: Could not find left hand anchor!");
            }
        }
        
        if (rightHandAnchor == null && detectRightHand)
        {
            GameObject rightHand = GameObject.Find(rightHandAnchorName);
            if (rightHand != null)
            {
                rightHandAnchor = rightHand.transform;
                DebugLog($"PhoneRingingSystem: Found right hand anchor: {rightHand.name}");
            }
            else
            {
                DebugLog("PhoneRingingSystem: Could not find right hand anchor!");
            }
        }
    }
    
    void SetupRingingAudio()
    {
        if (audioSource == null) return;
        
        // Configure 3D audio for ringing phase
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = ringingMinDistance;
        audioSource.maxDistance = ringingMaxDistance;
        
        DebugLog($"PhoneRingingSystem: Audio configured for ringing - Min: {ringingMinDistance}, Max: {ringingMaxDistance}, Rolloff: Logarithmic");
    }
    
    void SetupPickupAudio()
    {
        if (audioSource == null) return;
        
        // Configure 3D audio for pickup phase (close distances)
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = pickupMinDistance;
        audioSource.maxDistance = pickupMaxDistance;
        
        DebugLog($"PhoneRingingSystem: Audio configured for pickup - Min: {pickupMinDistance}, Max: {pickupMaxDistance}, Rolloff: Logarithmic");
    }
    
    void Update()
    {
        // Check for hand grab during ringing
        if (isRinging && !hasBeenPickedUp && !isPickupSequenceActive)
        {
            CheckForHandGrab();
        }
    }
    
    void CheckForHandGrab()
    {
        bool handDetected = false;
        
        // Check distance-based detection
        if (detectLeftHand && leftHandAnchor != null)
        {
            float leftDistance = Vector3.Distance(transform.position, leftHandAnchor.position);
            if (leftDistance <= grabDistance)
            {
                handDetected = true;
                DebugLog($"PhoneRingingSystem: Left hand grab detected! Distance: {leftDistance:F2}");
            }
        }
        
        if (detectRightHand && rightHandAnchor != null)
        {
            float rightDistance = Vector3.Distance(transform.position, rightHandAnchor.position);
            if (rightDistance <= grabDistance)
            {
                handDetected = true;
                DebugLog($"PhoneRingingSystem: Right hand grab detected! Distance: {rightDistance:F2}");
            }
        }
        
        if (handDetected)
        {
            OnPhoneGrabbed();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Backup collision-based detection
        if (isRinging && !hasBeenPickedUp && !isPickupSequenceActive)
        {
            if (IsHandAnchor(other))
            {
                DebugLog($"PhoneRingingSystem: Hand grab detected via collision: {other.name}");
                OnPhoneGrabbed();
            }
        }
    }
    
    bool IsHandAnchor(Collider other)
    {
        // Check by transform reference
        Transform otherTransform = other.transform;
        
        if (detectLeftHand && leftHandAnchor != null)
        {
            if (otherTransform == leftHandAnchor || otherTransform.IsChildOf(leftHandAnchor))
            {
                return true;
            }
        }
        
        if (detectRightHand && rightHandAnchor != null)
        {
            if (otherTransform == rightHandAnchor || otherTransform.IsChildOf(rightHandAnchor))
            {
                return true;
            }
        }
        
        // Check by name (fallback method)
        string objName = other.name.ToLower();
        
        if (detectLeftHand && objName.Contains("lefthand"))
        {
            return true;
        }
        
        if (detectRightHand && objName.Contains("righthand"))
        {
            return true;
        }
        
        // Check for general hand/controller names
        if ((detectLeftHand || detectRightHand) && 
            (objName.Contains("hand") || objName.Contains("controller") || objName.Contains("anchor")))
        {
            return true;
        }
        
        return false;
    }
    
    public void StartTimer()
    {
        if (isTimerRunning)
        {
            StopTimer();
        }
        
        timerCoroutine = StartCoroutine(TimerCoroutine());
        DebugLog($"PhoneRingingSystem: Timer started for {timerDuration} seconds");
    }
    
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        isTimerRunning = false;
        currentTimer = 0f;
        DebugLog("PhoneRingingSystem: Timer stopped");
    }
    
    IEnumerator TimerCoroutine()
    {
        isTimerRunning = true;
        currentTimer = 0f;
        
        while (currentTimer < timerDuration)
        {
            currentTimer += Time.deltaTime;
            yield return null;
        }
        
        isTimerRunning = false;
        DebugLog("PhoneRingingSystem: Timer completed! Starting phone ring...");
        
        // Start ringing when timer completes
        StartRinging();
    }
    
    void StartRinging()
    {
        if (isRinging || ringingClip == null || audioSource == null) return;
        
        isRinging = true;
        hasBeenPickedUp = false;
        currentRingIteration = 0;
        canPlayPickupClip = true; // Allow pickup during ring iterations
        
        DebugLog("PhoneRingingSystem: Phone started ringing sequence!");
        
        // Start the ringing sequence coroutine
        ringingCoroutine = StartCoroutine(RingingSequence());
    }
    
    void StopRinging()
    {
        if (!isRinging) return;
        
        isRinging = false;
        
        if (ringingCoroutine != null)
        {
            StopCoroutine(ringingCoroutine);
            ringingCoroutine = null;
        }
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            DebugLog("PhoneRingingSystem: Phone stopped ringing");
        }
    }
    
    IEnumerator RingingSequence()
    {
        for (int i = 0; i < maxRingIterations; i++)
        {
            if (!isRinging) yield break; // Exit if ringing was stopped
            
            currentRingIteration = i + 1;
            DebugLog($"PhoneRingingSystem: Playing ring iteration {currentRingIteration}/{maxRingIterations}");
            
            // Setup and play ringing clip
            audioSource.clip = ringingClip;
            audioSource.volume = ringingVolume;
            audioSource.loop = false;
            
            // Ensure ringing audio settings are applied
            SetupRingingAudio();
            
            audioSource.Play();
            
            // Wait for clip to finish playing
            while (audioSource.isPlaying && isRinging)
            {
                yield return null;
            }
            
            // If phone was picked up, exit the sequence
            if (hasBeenPickedUp)
            {
                yield break;
            }
        }
        
        // After both iterations complete without pickup, disable pickup ability
        if (isRinging)
        {
            canPlayPickupClip = false;
            isRinging = false;
            DebugLog("PhoneRingingSystem: Ringing sequence completed. Pickup no longer available.");
        }
    }
    
    void OnPhoneGrabbed()
    {
        if (hasBeenPickedUp || isPickupSequenceActive) return;
        
        // Only allow pickup during the first two ring iterations
        if (!canPlayPickupClip)
        {
            DebugLog("PhoneRingingSystem: Phone grab detected, but pickup period has ended.");
            return;
        }
        
        hasBeenPickedUp = true;
        isPickupSequenceActive = true;
        
        DebugLog($"PhoneRingingSystem: Phone grabbed by player during ring iteration {currentRingIteration}!");
        
        // Stop ringing immediately
        StopRinging();
        
        // Start pickup sequence with delay
        pickupCoroutine = StartCoroutine(PlayPickupSequence());
    }
    
    IEnumerator PlayPickupSequence()
    {
        DebugLog($"PhoneRingingSystem: Waiting {pickupDelay} seconds before playing pickup clip...");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(pickupDelay);
        
        // Play pickup clip if available
        if (pickupClip != null && audioSource != null)
        {
            audioSource.clip = pickupClip;
            audioSource.volume = pickupVolume;
            audioSource.loop = false; // Don't loop pickup clip
            
            // Configure close-range 3D audio for pickup
            SetupPickupAudio();
            
            audioSource.Play();
            
            DebugLog("PhoneRingingSystem: Playing pickup clip with close-range audio!");
        }
        
        isPickupSequenceActive = false;
    }
    
    // Public methods for external control
    public void ManualStartRinging()
    {
        StartRinging();
    }
    
    public void ManualStopRinging()
    {
        StopRinging();
    }
    
    public void SetRingingAudioDistances(float minDist, float maxDist)
    {
        ringingMinDistance = minDist;
        ringingMaxDistance = maxDist;
        
        // Apply immediately if currently ringing
        if (isRinging)
        {
            SetupRingingAudio();
        }
        
        DebugLog($"PhoneRingingSystem: Ringing audio distances updated - Min: {minDist}, Max: {maxDist}");
    }
    
    public void SetPickupAudioDistances(float minDist, float maxDist)
    {
        pickupMinDistance = minDist;
        pickupMaxDistance = maxDist;
        DebugLog($"PhoneRingingSystem: Pickup audio distances updated - Min: {minDist}, Max: {maxDist}");
    }
    
    public void ResetPhone()
    {
        StopTimer();
        StopRinging();
        
        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }
        
        if (ringingCoroutine != null)
        {
            StopCoroutine(ringingCoroutine);
            ringingCoroutine = null;
        }
        
        hasBeenPickedUp = false;
        isPickupSequenceActive = false;
        currentTimer = 0f;
        currentRingIteration = 0;
        canPlayPickupClip = false;
        
        DebugLog("PhoneRingingSystem: Phone system reset");
    }
    
    public void SetTimerDuration(float duration)
    {
        timerDuration = duration;
        DebugLog($"PhoneRingingSystem: Timer duration set to {duration} seconds");
    }
    
    public void SetPickupDelay(float delay)
    {
        pickupDelay = delay;
        DebugLog($"PhoneRingingSystem: Pickup delay set to {delay} seconds");
    }
    
    public void SetGrabDistance(float distance)
    {
        grabDistance = distance;
        
        // Update sphere collider if it exists
        SphereCollider sphereCol = GetComponent<SphereCollider>();
        if (sphereCol != null)
        {
            sphereCol.radius = distance;
        }
        
        DebugLog($"PhoneRingingSystem: Grab distance set to {distance}");
    }
    
    // Properties for external access
    public bool IsTimerRunning => isTimerRunning;
    public bool IsRinging => isRinging;
    public bool HasBeenPickedUp => hasBeenPickedUp;
    public bool CanPlayPickupClip => canPlayPickupClip;
    public int CurrentRingIteration => currentRingIteration;
    public int MaxRingIterations => maxRingIterations;
    public float RemainingTime => Mathf.Max(0f, timerDuration - currentTimer);
    public float TimerProgress => isTimerRunning ? currentTimer / timerDuration : 0f;
    
    void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGrabDistance) return;
        
        // Draw grab detection radius
        Gizmos.color = isRinging ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, grabDistance);
        
        // Draw lines to hands if detected
        if (leftHandAnchor != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, leftHandAnchor.position);
        }
        
        if (rightHandAnchor != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, rightHandAnchor.position);
        }
    }
}