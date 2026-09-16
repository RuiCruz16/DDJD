using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerBlockPlaceFMOD : MonoBehaviour
{
    [SerializeField] private EventReference blockPlaceEvent;

    private StudioEventEmitter emitter;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleBlockPlace()
    {
        Debug.Log("Block place event triggered!");
        EventReference eventRef = blockPlaceEvent.IsNull ? emitter.EventReference : blockPlaceEvent;
        
        if (eventRef.IsNull)
        {
            Debug.LogError("No Block Place event assigned!");
        }
        else
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            instance.release();
            Debug.Log("Block place event started");
        }
    }
}
