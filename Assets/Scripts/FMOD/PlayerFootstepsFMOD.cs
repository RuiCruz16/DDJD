using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(StudioEventEmitter))]
public class PlayerFootstepsFMOD : MonoBehaviour
{
    private StudioEventEmitter emitter;
    private bool isPlaying;

    private void Awake()
    {
        emitter = GetComponent<StudioEventEmitter>();
    }

    public void HandleMovement(bool isMoving, bool isGrounded, float speed)
    {
        bool shouldPlay = isMoving && isGrounded;

        if (shouldPlay && !isPlaying)
        {
            emitter.Play();
            isPlaying = true;
        }
        else if (!shouldPlay && isPlaying)
        {
            emitter.Stop();
            isPlaying = false;
        }
    }
}
