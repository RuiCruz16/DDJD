using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class GameAmbienceFMOD : MonoBehaviour
{
    [SerializeField] private EventReference ambienceEvent;

    private StudioEventEmitter emitter;
    private EventInstance ambienceInstance;
    private bool isPlaying = false;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    private void Start()
    {
        StartAmbience();
    }

    public void StartAmbience()
    {
        if (isPlaying)
            return;

        Debug.Log("Game ambience started!");
        EventReference eventRef = ambienceEvent.IsNull ? emitter.EventReference : ambienceEvent;
        
        if (eventRef.IsNull)
        {
            Debug.LogError("No Ambience event assigned!");
            return;
        }

        ambienceInstance = RuntimeManager.CreateInstance(eventRef);
        ambienceInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        ambienceInstance.start();
        isPlaying = true;
        Debug.Log("Game ambience playing");
    }

    public void PauseAmbience()
    {
        if (!isPlaying)
            return;

        ambienceInstance.setPaused(true);
        Debug.Log("Game ambience paused");
    }

    public void ResumeAmbience()
    {
        if (!isPlaying)
            return;

        ambienceInstance.setPaused(false);
        Debug.Log("Game ambience resumed");
    }

    public void StopAmbience()
    {
        if (!isPlaying)
            return;

        ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        ambienceInstance.release();
        isPlaying = false;
        Debug.Log("Game ambience stopped");
    }

    private void OnDestroy()
    {
        if (isPlaying)
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            ambienceInstance.release();
        }
    }
}
