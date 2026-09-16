using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerDiveFMOD : MonoBehaviour
{
    [SerializeField] private EventReference diveEvent;

    private StudioEventEmitter emitter;
    private bool wasInWater = false;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleDive(bool inWater)
    {
        // Detect transition from air to water
        if (inWater && !wasInWater)
        {
            Debug.Log("Dive triggered!");
            EventReference eventRef = diveEvent.IsNull ? emitter.EventReference : diveEvent;
            
            if (eventRef.IsNull)
            {
                Debug.LogError("No Dive event assigned!");
            }
            else
            {
                EventInstance instance = RuntimeManager.CreateInstance(eventRef);
                instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                instance.start();
                instance.release();
                Debug.Log("Dive event started");
            }
        }

        wasInWater = inWater;
    }
}
