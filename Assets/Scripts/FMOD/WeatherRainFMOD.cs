using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// Plays a looping rain ambience while the weather is rainy.
/// <see cref="WeatherManager"/> calls <see cref="StartRain"/> / <see cref="StopRain"/>
/// when the weather changes. Mirrors the pattern used by GameAmbienceFMOD.
/// </summary>
public class WeatherRainFMOD : MonoBehaviour
{
    [SerializeField] private EventReference rainEvent;

    [Tooltip("If true, the rain ambience tracks the FMOD listener so it never attenuates to silence as the player/raft moves. Leave on for global rain ambience.")]
    [SerializeField] private bool followListener = true;

    private EventInstance rainInstance;
    private bool isPlaying = false;

    public void StartRain()
    {
        if (isPlaying)
            return;

        if (rainEvent.IsNull)
        {
            Debug.LogError("No Rain event assigned on WeatherRainFMOD! Assign the rain FMOD event in the Inspector.");
            return;
        }

        rainInstance = RuntimeManager.CreateInstance(rainEvent);
        UpdateRainPosition();
        rainInstance.start();
        isPlaying = true;
        Debug.Log("Rain loop playing");
    }

    private void Update()
    {
        // Keep the (3D) rain ambience on top of the listener so distance attenuation
        // never fades it out. Harmless for 2D events.
        if (isPlaying && followListener)
            UpdateRainPosition();
    }

    private void UpdateRainPosition()
    {
        Vector3 pos = transform.position;

        // Anchor the rain at the active FMOD listener (usually the main camera) when following.
        if (followListener && Camera.main != null)
            pos = Camera.main.transform.position;

        rainInstance.set3DAttributes(RuntimeUtils.To3DAttributes(pos));
    }

    public void StopRain()
    {
        if (!isPlaying)
            return;

        rainInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        rainInstance.release();
        isPlaying = false;
        Debug.Log("Rain loop stopped");
    }

    private void OnDestroy()
    {
        if (isPlaying)
        {
            rainInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            rainInstance.release();
        }
    }
}