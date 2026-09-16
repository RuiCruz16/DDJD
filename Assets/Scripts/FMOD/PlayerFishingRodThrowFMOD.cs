using System.Collections;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerFishingRodThrowFMOD : MonoBehaviour
{
    [SerializeField] private EventReference fishingRodThrowEvent;

    [Tooltip("Optional extra delay (in seconds) before the splash sound plays after the rod hits the water. Leave at 0 to play immediately on impact.")]
    [SerializeField] private float playbackDelay = 0f;

    private StudioEventEmitter emitter;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
        if (emitter == null)
        {
            Debug.LogError("StudioEventEmitter NOT found on this GameObject");
        }
    }

    public void HandleFishingRodThrow()
    {
        Debug.Log("Fishing Rod throw event triggered!");

        if (playbackDelay > 0f)
        {
            StartCoroutine(PlayAfterDelay(playbackDelay));
        }
        else
        {
            PlayThrowSound();
        }
    }

    private IEnumerator PlayAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayThrowSound();
    }

    private void PlayThrowSound()
    {
        EventReference eventRef = fishingRodThrowEvent.IsNull ? emitter.EventReference : fishingRodThrowEvent;

        if (eventRef.IsNull)
        {
            Debug.LogError("No Fishing Rod Throw event assigned!");
        }
        else
        {
            EventInstance instance = RuntimeManager.CreateInstance(eventRef);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.start();
            instance.release();
            Debug.Log("Fishing Rod throw event started");
        }
    }
}