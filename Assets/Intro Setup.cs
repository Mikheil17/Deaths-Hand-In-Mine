using UnityEngine;
using UnityEngine.Video;

public class IntroSetup : MonoBehaviour
{
    [Tooltip("The GameObject that contains the VideoPlayer (starts disabled).")]
    public GameObject intro;

    [Tooltip("Seconds to wait before showing and playing the intro.")]
    public float delay = 10f;

    // We'll fill this at runtime (safe even if intro is inactive)
    private VideoPlayer videoPlayer;
    private bool subscribedLoop = false;
    private bool subscribedPrepared = false;

    void Start()
    {
        if (intro == null)
        {
            Debug.LogError("[IntroSetup] 'intro' is not assigned in the Inspector.");
            return;
        }

        // Ensure the Quad (this object) is active so coroutines run
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("[IntroSetup] This script's GameObject is inactive. Enable it so the timer can run.");
            return;
        }

        // Make sure intro starts disabled
        intro.SetActive(false);

        // Try to find VideoPlayer component inside the intro object (works even if inactive)
        videoPlayer = intro.GetComponentInChildren<VideoPlayer>(true);
        if (videoPlayer == null)
        {
            Debug.LogWarning("[IntroSetup] No VideoPlayer found on 'intro' or its children. Will try to locate it again when enabling.");
        }

        StartCoroutine(StartAfterDelay());
    }

    private System.Collections.IEnumerator StartAfterDelay()
    {
        Debug.Log($"[IntroSetup] Waiting {delay} seconds before showing intro...");
        yield return new WaitForSeconds(delay);

        // Enable the intro object
        intro.SetActive(true);
        Debug.Log("[IntroSetup] Intro object enabled.");

        // If we didn't find the VideoPlayer earlier, try again now
        if (videoPlayer == null)
        {
            videoPlayer = intro.GetComponentInChildren<VideoPlayer>(true);
            if (videoPlayer == null)
            {
                Debug.LogError("[IntroSetup] Still no VideoPlayer found inside 'intro'. Aborting play.");
                yield break;
            }
        }

        // Attach events (avoid double subscription)
        if (!subscribedLoop)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
            subscribedLoop = true;
        }

        // If video is already prepared, just play. Otherwise prepare then play when ready.
        if (videoPlayer.isPrepared)
        {
            Debug.Log("[IntroSetup] VideoPlayer already prepared -> Play()");
            videoPlayer.Play();
        }
        else
        {
            // Subscribe to prepareCompleted once
            if (!subscribedPrepared)
            {
                videoPlayer.prepareCompleted += OnPrepareCompleted;
                subscribedPrepared = true;
            }

            // If a clip or URL is missing, warn and attempt to Play anyway (VideoPlayer will log too)
            if (videoPlayer.clip == null && string.IsNullOrEmpty(videoPlayer.url))
            {
                Debug.LogWarning("[IntroSetup] VideoPlayer has no clip or url assigned.");
            }

            Debug.Log("[IntroSetup] Preparing VideoPlayer...");
            videoPlayer.Prepare();

            // Safety: fallback in case prepareCompleted doesn't fire (timeout)
            float timeout = 5f; // seconds
            float timer = 0f;
            while (!videoPlayer.isPrepared && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            if (videoPlayer.isPrepared)
            {
                // If prepareCompleted fired, OnPrepareCompleted already called Play().
                if (!videoPlayer.isPlaying)
                {
                    Debug.Log("[IntroSetup] Prepare timeout loop finished but player not playing -> Starting Play()");
                    videoPlayer.Play();
                }
            }
            else
            {
                Debug.LogWarning("[IntroSetup] VideoPlayer did not prepare within timeout. Attempting Play() anyway.");
                videoPlayer.Play();
            }
        }
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        Debug.Log("[IntroSetup] VideoPlayer prepareCompleted -> Play()");
        vp.Play();

        // Unsubscribe prepare handler to avoid repeated calls
        vp.prepareCompleted -= OnPrepareCompleted;
        subscribedPrepared = false;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("[IntroSetup] Video ended -> starting 5-second rest before disabling.");
        StartCoroutine(RestThenDisable());
    }

    private System.Collections.IEnumerator RestThenDisable()
    {
        yield return new WaitForSeconds(5f); // 5-second pause
        intro.SetActive(false);
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            if (subscribedLoop)
                videoPlayer.loopPointReached -= OnVideoEnd;
            if (subscribedPrepared)
                videoPlayer.prepareCompleted -= OnPrepareCompleted;
        }
    }
}
