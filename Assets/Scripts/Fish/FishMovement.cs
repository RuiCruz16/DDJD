using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 2f;
    [Tooltip("How close the fish needs to get to the raft before it starts wandering around it.")]
    public float orbitRadius = 5f;

    [Header("Wandering")]
    [Tooltip("Min/max distance from the raft when picking a random waypoint.")]
    public float minWanderRadius = 3f;
    public float maxWanderRadius = 7f;
    [Tooltip("How close to a waypoint counts as reached.")]
    public float waypointReachedDistance = 0.5f;
    [Tooltip("Max time the fish can spend going to a single waypoint before picking a new one.")]
    public float maxTimePerWaypoint = 5f;
    [Tooltip("Total time the fish wanders around the raft before leaving.")]
    public float totalWanderTime = 18f;
    [Tooltip("How sharply the fish turns toward its waypoint. Higher = snappier.")]
    public float turnSpeed = 2.5f;
    [Tooltip("Strength of the side-to-side wiggle while swimming.")]
    public float wanderNoiseStrength = 0.8f;
    [Tooltip("How fast the wiggle pattern changes.")]
    public float wanderNoiseFrequency = 0.6f;
    [Tooltip("Chance (0-1) of pausing briefly after reaching a waypoint.")]
    [Range(0f, 1f)] public float pauseChance = 0.25f;
    public Vector2 pauseDurationRange = new Vector2(0.5f, 1.5f);
    [Tooltip("Speed multiplier randomly applied per waypoint to vary pace.")]
    public Vector2 speedJitterRange = new Vector2(0.7f, 1.2f);

    [Header("Floating")]
    public float waterLevel = 0f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 2f;

    private enum Phase { Approach, Wander, Pause, Leave }

    private Transform raft;
    private Phase phase = Phase.Approach;
    private Vector3 moveDirection;
    private Vector3 currentWaypoint;
    private float wanderElapsed;
    private float waypointElapsed;
    private float pauseTimer;
    private float currentSpeedMul = 1f;
    private float randomOffset;
    private float noiseSeed;

    public void SetMoveDirection(Vector3 dir) => moveDirection = dir;

    void Start()
    {
        raft = GameObject.FindGameObjectWithTag("Raft")?.transform;
        randomOffset = Random.Range(0f, 100f);
        noiseSeed = Random.Range(0f, 1000f);

        if (raft == null) phase = Phase.Leave;
    }

    void Update()
    {
        // Skip movement when carried (e.g. parented to fishing hook).
        if (transform.parent != null) return;

        switch (phase)
        {
            case Phase.Approach: ApproachRaft(); break;
            case Phase.Wander:   WanderAroundRaft(); break;
            case Phase.Pause:    PauseAtWaypoint(); break;
            case Phase.Leave:    LeaveRaft(); break;
        }

        ApplyWaterBobbing();
    }

    void ApproachRaft()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        FaceDirection(moveDirection);

        if (Vector3.Distance(transform.position, raft.position) <= orbitRadius)
        {
            phase = Phase.Wander;
            wanderElapsed = 0f;
            PickNewWaypoint();
        }
    }

    void WanderAroundRaft()
    {
        wanderElapsed += Time.deltaTime;
        waypointElapsed += Time.deltaTime;

        Vector3 toTarget = currentWaypoint - transform.position;
        toTarget.y = 0f; // keep steering horizontal; bobbing handles Y
        Vector3 desired = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;

        // Add side-to-side noise so motion isn't a straight line.
        float n = Mathf.PerlinNoise(noiseSeed, Time.time * wanderNoiseFrequency) - 0.5f;
        Vector3 sideways = Vector3.Cross(Vector3.up, desired);
        desired = (desired + sideways * (n * 2f * wanderNoiseStrength)).normalized;

        Vector3 baseDir = moveDirection.sqrMagnitude < 0.001f ? desired : moveDirection;
        moveDirection = Vector3.Slerp(baseDir, desired, Time.deltaTime * turnSpeed);

        transform.Translate(moveDirection * speed * currentSpeedMul * Time.deltaTime, Space.World);
        FaceDirection(moveDirection);

        bool reached = toTarget.sqrMagnitude <= waypointReachedDistance * waypointReachedDistance;
        bool timedOut = waypointElapsed >= maxTimePerWaypoint;

        if (reached || timedOut)
        {
            if (reached && Random.value < pauseChance)
            {
                phase = Phase.Pause;
                pauseTimer = Random.Range(pauseDurationRange.x, pauseDurationRange.y);
                return;
            }
            PickNewWaypoint();
        }

        if (wanderElapsed >= totalWanderTime)
        {
            phase = Phase.Leave;
            Vector3 awayFromRaft = transform.position - raft.position;
            awayFromRaft.y = 0f;
            moveDirection = awayFromRaft.sqrMagnitude > 0.01f ? awayFromRaft.normalized : transform.forward;
        }
    }

    void PauseAtWaypoint()
    {
        pauseTimer -= Time.deltaTime;
        wanderElapsed += Time.deltaTime;

        // Slow yaw sway so the fish still looks alive while idling.
        transform.Rotate(0f, Mathf.Sin(Time.time * 1.5f + randomOffset) * 8f * Time.deltaTime, 0f, Space.World);

        if (pauseTimer <= 0f) PickNewWaypoint();
        if (wanderElapsed >= totalWanderTime) phase = Phase.Leave;
    }

    void LeaveRaft()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
        FaceDirection(moveDirection);
    }

    void PickNewWaypoint()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(minWanderRadius, maxWanderRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        Vector3 raftPos = raft != null ? raft.position : transform.position;
        currentWaypoint = raftPos + offset;
        currentWaypoint.y = transform.position.y;

        waypointElapsed = 0f;
        currentSpeedMul = Random.Range(speedJitterRange.x, speedJitterRange.y);
        phase = Phase.Wander;
    }

    void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed * 2f);
    }

    void ApplyWaterBobbing()
    {
        float newY = waterLevel + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}