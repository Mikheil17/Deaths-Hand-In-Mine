using UnityEngine;
using UnityEngine.Video;

public class VRFullScreenVideo : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip videoClip;
    
    [Header("Display Settings")]
    public float quadDistance = 10f;
    public float quadWidth = 30f;
    public float quadHeight = 20f;
    public bool stretchToFit = true;
    public Material customMaterial; // Assign a material in Inspector
    
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private GameObject videoQuad;
    private Material videoMaterial;
    
    void Start()
    {
        CreateVideoQuad();
        SetupVideoPlayer();
        PlayVideo();
    }
    
    void CreateVideoQuad()
    {
        // Create a large quad
        videoQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        videoQuad.name = "FullScreenVideoQuad";
        
        // Remove the collider (we don't need it)
        Destroy(videoQuad.GetComponent<Collider>());
        
        // Use custom material if provided, otherwise create one
        if (customMaterial != null)
        {
            videoMaterial = new Material(customMaterial);
            Debug.Log("Using custom material: " + customMaterial.shader.name);
        }
        else
        {
            // Try to find a working shader
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            if (shader == null) shader = Shader.Find("UI/Default");
            
            videoMaterial = new Material(shader);
            Debug.Log("Created material with shader: " + shader.name);
        }
        
        videoQuad.GetComponent<Renderer>().material = videoMaterial;
        
        // Set custom width and height to stretch
        videoQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);
        
        Debug.Log("Video quad created with dimensions: " + quadWidth + "x" + quadHeight);
    }
    
    void SetupVideoPlayer()
    {
        // Add VideoPlayer component
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        
        if (videoClip == null)
        {
            Debug.LogError("No video clip assigned!");
            return;
        }
        
        // Create render texture with exact video dimensions
        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        
        // Configure video player
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.waitForFirstFrame = true;
        
        // Assign texture to material
        videoMaterial.mainTexture = renderTexture;
        
        // Events
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
        
        videoPlayer.Prepare();
        
        Debug.Log("VideoPlayer setup complete");
    }
    
    void Update()
    {
        // Keep the quad in front of the camera
        if (videoQuad != null && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            videoQuad.transform.position = cam.position + cam.forward * quadDistance;
            videoQuad.transform.rotation = Quaternion.LookRotation(videoQuad.transform.position - cam.position);
        }
    }
    
    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video prepared - Duration: " + vp.length + " seconds");
    }
    
    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Video error: " + message);
    }
    
    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            Debug.Log("Playing video");
        }
    }
    
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        
        if (videoQuad != null)
        {
            videoQuad.SetActive(false);
        }
    }
    
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        if (videoQuad != null)
        {
            Destroy(videoQuad);
        }
    }
}