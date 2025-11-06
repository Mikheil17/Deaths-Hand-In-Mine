using UnityEngine;
using System.Collections;

public class RecordPlayer : MonoBehaviour
{
    public bool recordPlayerActive = false;

    GameObject disc;
    GameObject arm;

    // External vinyl object (assign in Inspector)
    public GameObject vinyl;

    // Audio source for playing music (assign in Inspector)
    public AudioSource audioSource;

    int mode;
    float armAngle;
    float discAngle;
    float discSpeed;

    void Awake()
    {
        disc = gameObject.transform.Find("teller").gameObject;
        arm = gameObject.transform.Find("arm").gameObject;
    }

    void Start()
    {
        mode = 0;
        armAngle = 0.0f;
        discAngle = 0.0f;
        discSpeed = 0.0f;
    }

    void Update()
    {
        // Mode 0: player off
        if (mode == 0)
        {
            if (recordPlayerActive)
                mode = 1;
        }
        // Mode 1: arm moving out / activation
        else if (mode == 1)
        {
            if (recordPlayerActive)
            {
                armAngle += Time.deltaTime * 30.0f;
                if (armAngle >= 30.0f)
                {
                    armAngle = 30.0f;
                    mode = 2; // arm fully moved, start spinning
                }
                discAngle += Time.deltaTime * discSpeed;
                discSpeed += Time.deltaTime * 80.0f;
            }
            else
            {
                mode = 3; // stop mode if turned off
            }
        }
        // Mode 2: running
        else if (mode == 2)
        {
            if (recordPlayerActive)
                discAngle += Time.deltaTime * discSpeed;
            else
                mode = 3;
        }
        // Mode 3: stopping
        else // mode == 3
        {
            if (!recordPlayerActive)
            {
                armAngle -= Time.deltaTime * 30.0f;
                if (armAngle <= 0.0f)
                    armAngle = 0.0f;

                discAngle += Time.deltaTime * discSpeed;
                discSpeed -= Time.deltaTime * 80.0f;
                if (discSpeed <= 0.0f)
                    discSpeed = 0.0f;

                if (discSpeed == 0.0f && armAngle == 0.0f)
                    mode = 0;
            }
            else
            {
                mode = 1;
            }
        }

        // Update arm and disc rotation
        arm.transform.localEulerAngles = new Vector3(0.0f, armAngle, 0.0f);
        disc.transform.localEulerAngles = new Vector3(0.0f, discAngle, 0.0f);

        // Rotate vinyl while keeping it positioned on the record player
        if (vinyl != null && recordPlayerActive && discSpeed > 0.0f)
        {
            // Keep vinyl positioned on the disc surface
            Vector3 discPosition = disc.transform.position;
            Vector3 discUp = disc.transform.up;
            
            // Position vinyl slightly above the disc surface
            vinyl.transform.position = discPosition + discUp * 0.01f;
            
            // Rotate vinyl around its local Y axis (or match disc rotation)
            vinyl.transform.Rotate(0f, discSpeed * Time.deltaTime, 0f, Space.Self);
            
            // Ensure vinyl rotation matches disc orientation
            Vector3 vinylEuler = vinyl.transform.eulerAngles;
            vinylEuler.y = discAngle;
            vinyl.transform.eulerAngles = vinylEuler;
        }

        // Audio control: play only when arm is fully moved and spinning (mode 2)
        if (audioSource != null)
        {
            if (mode == 2 && recordPlayerActive && discSpeed > 0.1f)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
            }
        }
    }
}
