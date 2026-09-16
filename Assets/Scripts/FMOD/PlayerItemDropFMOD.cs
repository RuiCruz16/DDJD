using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerItemDropFMOD : MonoBehaviour
{
    [SerializeField] private EventReference itemDropEvent;

    private StudioEventEmitter emitter;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleItemDrop()
    {
        Debug.Log("Item drop event triggered!");
        EventReference eventRef = itemDropEvent.IsNull ? emitter.EventReference : itemDropEvent;
        
        if (eventRef.IsNull)
        {
            Debug.LogError("No Item Drop event assigned!");
        }
        else
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            instance.release();
            Debug.Log("Item drop event started");
        }
    }
}
