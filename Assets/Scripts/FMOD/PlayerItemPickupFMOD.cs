using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerItemPickupFMOD : MonoBehaviour
{
    [SerializeField] private EventReference itemPickupEvent;

    private StudioEventEmitter emitter;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleItemPickup()
    {
        Debug.Log("Item pickup event triggered!");
        EventReference eventRef = itemPickupEvent.IsNull ? emitter.EventReference : itemPickupEvent;
        
        if (eventRef.IsNull)
        {
            Debug.LogError("No Item Pickup event assigned!");
        }
        else
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            instance.release();
            Debug.Log("Item pickup event started");
        }
    }
}
