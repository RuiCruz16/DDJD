using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerDamageFMOD : MonoBehaviour
{
    [SerializeField] private EventReference damageEvent;

    private StudioEventEmitter emitter;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleDamage()
    {
        Debug.Log("Damage event triggered!");
        EventReference eventRef = damageEvent.IsNull ? emitter.EventReference : damageEvent;
        
        if (eventRef.IsNull)
        {
            Debug.LogError("No Damage event assigned!");
        }
        else
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            instance.release();
            Debug.Log("Damage event started");
        }
    }
}
