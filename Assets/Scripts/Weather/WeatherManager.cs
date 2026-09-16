using System.Collections;
using UnityEngine;

/// <summary>
/// Simple two-state weather system: Sunny and Rainy.
/// Every <see cref="checkInterval"/> seconds it rolls a <see cref="changeChance"/> chance
/// to switch the weather. When it changes it smoothly darkens the sky/fog/ambient/sun for
/// rain (and brightens back for sun) and toggles a rain particle system.
/// </summary>
public class WeatherManager : MonoBehaviour
{
    public enum Weather { Sunny, Rainy }

    [Header("Timing")]
    [Tooltip("How often (seconds) we roll for a weather change.")]
    public float checkInterval = 60f;
    [Range(0f, 1f)]
    [Tooltip("Chance (0-1) that the weather flips when we roll.")]
    public float changeChance = 0.3f;

    [Header("State")]
    public Weather currentWeather = Weather.Sunny;

    [Header("References")]
    [Tooltip("The main directional light (the 'sun'). Optional but recommended.")]
    public Light sunLight;
    [Tooltip("Rain particle system. Leave EMPTY to have one auto-built at runtime (recommended for URP).")]
    public ParticleSystem rainParticles;
    [Tooltip("Rain follows this transform (e.g. the raft/player) so it stays overhead. Optional.")]
    public Transform rainFollowTarget;
    [Tooltip("How high above the follow target the rain emitter sits.")]
    public float rainHeight = 25f;
    [Tooltip("How wide an area (meters) the rain covers around the target.")]
    public float rainArea = 50f;
    [Tooltip("Ocean surface height. Rain splashes spawn here when drops hit the water.")]
    public float waterLevel = 0f;
    [Tooltip("Looping rain FMOD audio. Plays while raining, stops when sunny. Optional.")]
    public WeatherRainFMOD rainAudio;

    [Header("Sunny Look")]
    public Color sunnyFogColor = new Color(0.6f, 0.75f, 0.85f);
    public Color sunnyAmbientColor = new Color(0.55f, 0.6f, 0.65f);
    public Color sunnyLightColor = new Color(1f, 0.96f, 0.84f);
    public float sunnyLightIntensity = 1.1f;
    public float sunnyFogDensity = 0.004f;

    [Header("Rainy Look")]
    public Color rainyFogColor = new Color(0.28f, 0.3f, 0.34f);
    public Color rainyAmbientColor = new Color(0.22f, 0.24f, 0.27f);
    public Color rainyLightColor = new Color(0.55f, 0.58f, 0.62f);
    public float rainyLightIntensity = 0.45f;
    public float rainyFogDensity = 0.02f;

    [Header("Transition")]
    [Tooltip("Seconds it takes to blend between sunny and rainy looks.")]
    public float transitionDuration = 4f;

    Coroutine transitionRoutine;

    void Start()
    {
        // If the rain audio reference wasn't wired in the Inspector, try to find one
        // automatically so the rain sound isn't silently skipped.
        if (rainAudio == null)
        {
            rainAudio = GetComponent<WeatherRainFMOD>();
            if (rainAudio == null)
                rainAudio = FindObjectOfType<WeatherRainFMOD>();

            if (rainAudio == null)
                Debug.LogWarning("[Weather] No WeatherRainFMOD assigned or found in the scene. " +
                                 "Rain audio will not play. Add a WeatherRainFMOD component and assign it to WeatherManager.rainAudio.");
            else
                Debug.Log("[Weather] Auto-linked rain audio to " + rainAudio.name);
        }

        // Turn on exponential fog; it's the cheapest way to make rain feel moody.
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fog = true;

        // If no rain system was wired in the editor, build one in code with a URP-safe
        // material. Hand-made particle systems render invisible in URP when their material
        // uses the legacy built-in shader, which is the usual reason rain "doesn't show".
        if (rainParticles == null)
            rainParticles = BuildRainParticles();

        // Snap instantly to the starting weather, then start rolling.
        ApplyWeatherInstant(currentWeather);
        StartCoroutine(WeatherRoutine());
    }

    IEnumerator WeatherRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (Random.value <= changeChance)
            {
                Weather next = currentWeather == Weather.Sunny ? Weather.Rainy : Weather.Sunny;
                SetWeather(next);
            }
        }
    }

    void Update()
    {
        // Keep the rain emitter hovering above the target.
        if (rainParticles != null && rainFollowTarget != null)
        {
            Vector3 pos = rainFollowTarget.position;
            pos.y += rainHeight;
            rainParticles.transform.position = pos;
        }
    }

    public void SetWeather(Weather weather)
    {
        currentWeather = weather;
        Debug.Log("[Weather] Changing to " + weather);

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionTo(weather));

        // Start/stop rain right away so it ramps in with the look.
        if (rainParticles != null)
        {
            if (weather == Weather.Rainy)
                rainParticles.Play();
            else
                rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // Start/stop the looping rain audio alongside the visuals.
        if (rainAudio != null)
        {
            if (weather == Weather.Rainy)
                rainAudio.StartRain();
            else
                rainAudio.StopRain();
        }
    }

    IEnumerator TransitionTo(Weather weather)
    {
        bool rainy = weather == Weather.Rainy;

        Color fromFog = RenderSettings.fogColor;
        Color fromAmbient = RenderSettings.ambientLight;
        float fromFogDensity = RenderSettings.fogDensity;
        Color fromLight = sunLight != null ? sunLight.color : Color.white;
        float fromIntensity = sunLight != null ? sunLight.intensity : 1f;

        Color toFog = rainy ? rainyFogColor : sunnyFogColor;
        Color toAmbient = rainy ? rainyAmbientColor : sunnyAmbientColor;
        float toFogDensity = rainy ? rainyFogDensity : sunnyFogDensity;
        Color toLight = rainy ? rainyLightColor : sunnyLightColor;
        float toIntensity = rainy ? rainyLightIntensity : sunnyLightIntensity;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = transitionDuration > 0f ? Mathf.Clamp01(t / transitionDuration) : 1f;

            RenderSettings.fogColor = Color.Lerp(fromFog, toFog, k);
            RenderSettings.ambientLight = Color.Lerp(fromAmbient, toAmbient, k);
            RenderSettings.fogDensity = Mathf.Lerp(fromFogDensity, toFogDensity, k);

            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(fromLight, toLight, k);
                sunLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, k);
            }

            yield return null;
        }

        ApplyWeatherInstant(weather);
        transitionRoutine = null;
    }

    /// <summary>
    /// Builds a simple downward rain particle system at runtime with a URP-compatible
    /// material so it actually renders. Starts stopped; SetWeather controls play/stop.
    /// </summary>
    ParticleSystem BuildRainParticles()
    {
        GameObject go = new GameObject("Rain (auto)");
        go.transform.SetParent(transform, false);
        // Point emission straight down.
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startLifetime = 1.3f;
        main.startSpeed = 35f;
        main.startSize = 0.03f;                                   // thinner drops
        main.startColor = new Color(0.42f, 0.47f, 0.52f, 0.22f);  // darker, desaturated to match the moody ocean
        main.gravityModifier = 1f;
        main.maxParticles = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 350f;                             // fewer drops

        // Flat box overhead so rain falls over a wide area around the target.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainArea, rainArea, 1f);

        // Stretch the billboards into thin streaks so they read as rain.
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 2.5f;                              // shorter streaks

        // URP-safe unlit particle material (built-in particle shaders render invisibly in URP).
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default"); // safe fallback
        Material mat = new Material(shader);
        mat.color = new Color(0.45f, 0.5f, 0.55f, 0.3f);
        renderer.material = mat;

        // Kill drops when they reach the ocean surface and spawn a splash there.
        ParticleSystem splash = BuildSplashParticles(go.transform);

        GameObject planeGO = new GameObject("WaterCollisionPlane");
        planeGO.transform.SetParent(transform, false);
        planeGO.transform.position = new Vector3(0f, waterLevel, 0f);
        // Plane collision uses the transform's local +Y as the surface normal.

        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.Planes;
        collision.SetPlane(0, planeGO.transform);
        collision.dampen = 1f;
        collision.bounce = 0f;
        collision.lifetimeLoss = 1f;                              // remove drop on impact

        var sub = ps.subEmitters;
        sub.enabled = true;
        sub.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision,
            ParticleSystemSubEmitterProperties.InheritNothing);

        Debug.Log("[Weather] Auto-built rain particle system (URP material) with water splashes.");
        return ps;
    }

    /// <summary>
    /// Small splash burst spawned where each raindrop hits the water. Driven as a
    /// collision sub-emitter of the rain system, so it only plays on impact.
    /// </summary>
    ParticleSystem BuildSplashParticles(Transform parent)
    {
        GameObject go = new GameObject("Splash (auto)");
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startLifetime = 0.35f;
        main.startSpeed = 1.2f;
        main.startSize = 0.12f;
        main.startColor = new Color(0.5f, 0.56f, 0.6f, 0.32f);    // muted, low-contrast against the dark water
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        // No continuous emission; the collision sub-emitter emits a tiny burst per hit.
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)2, (short)3)
        });

        // Spray outward and slightly up like a droplet ring.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        renderer.material = new Material(shader) { color = new Color(0.5f, 0.56f, 0.6f, 0.32f) };

        return ps;
    }

    void ApplyWeatherInstant(Weather weather)
    {
        bool rainy = weather == Weather.Rainy;

        RenderSettings.fogColor = rainy ? rainyFogColor : sunnyFogColor;
        RenderSettings.ambientLight = rainy ? rainyAmbientColor : sunnyAmbientColor;
        RenderSettings.fogDensity = rainy ? rainyFogDensity : sunnyFogDensity;

        if (sunLight != null)
        {
            sunLight.color = rainy ? rainyLightColor : sunnyLightColor;
            sunLight.intensity = rainy ? rainyLightIntensity : sunnyLightIntensity;
        }

        if (rainParticles != null)
        {
            if (rainy) rainParticles.Play();
            else rainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (rainAudio != null)
        {
            if (rainy) rainAudio.StartRain();
            else rainAudio.StopRain();
        }
    }
}