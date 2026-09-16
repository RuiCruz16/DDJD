using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerSwimFMOD : MonoBehaviour
{
    [SerializeField] private EventReference swimLoopEvent;  // optional if you want to override emitter event

    private StudioEventEmitter emitter;
    private EventInstance instance;
    private bool isPlaying;

    public void HandleSwim(bool inWater, bool isMoving)
    {
        bool shouldPlay = inWater && isMoving;

        if (shouldPlay && !isPlaying)
        {
            instance = RuntimeManager.CreateInstance(swimLoopEvent.IsNull ? emitter.EventReference : swimLoopEvent);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            isPlaying = true;
        }
        else if (!shouldPlay && isPlaying)
        {
            instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isPlaying = false;
        }
    }

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
    }

    private void OnDestroy()
    {
        if (isPlaying)
            instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        instance.release();
    }
}