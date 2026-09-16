using UnityEngine;
using System.Collections.Generic;

public class Shark : MonoBehaviour
{
    enum SharkState { Roaming, Hunting, AttackingPlayer, AttackingRaft }
    SharkState currentState;

    Transform player;

    private Player playerScript;
    public int sharkDamage = 20;

    public float waterLevel = 0f;
    public float speed = 5f;
    public float rotationSpeed = 2f;
    public float swimRadius = 50f;
    public float surfaceLevel = -1f;
    public float deepLevel = -10f;
    [Range(0, 1)] 
    public float surfaceSwimChance = 0.5f;
    public float raftClearanceBuffer = 1f;
    public float raftDetectionPadding = 3f;
    [Range(0, 1)]
    public float raftHitChance = 0.15f;
    public float raftAttackCooldown = 60f;
    public float raftAttackMaxY = -0.875f;

    public float playerDetectionRadius = 35f;
    public float playerAttackDistance = 2f;
    public float attackCooldown = 3f;

    public float maxHuntDuration = 12f; // Safety net so hunting can never last forever

    float lastAttackTime;
    float lastRaftAttackTime;
    float huntStartTime;
    Collider raftTarget;
    Barricade barricadeTarget; // Current barricade being attacked
    Vector3 targetPosition, centerPoint;

    Collider sharkCollider;
    bool playerCollisionIgnored;

    void Start()
    {
        // Ensure shark has a collider. It must be SOLID (non-trigger) so the shark
        // physically rams the raft's Rigidbody and shakes it. The player's melee uses
        // Physics.OverlapSphere, which still detects a solid collider, so hit detection works too.
        sharkCollider = GetComponent<Collider>();
        if (sharkCollider == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.radius = 2f;
            col.isTrigger = false;
            sharkCollider = col;
            Debug.Log("[Shark] Added solid SphereCollider to shark for raft impact + hit detection");
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            player = playerObj.transform;
            playerScript = playerObj.GetComponent<Player>();
            IgnorePlayerCollision();
        }
        else
            Debug.LogError("[Shark] Player not found. Tag your player 'Player'.", this);

        centerPoint = transform.position;
        currentState = SharkState.Roaming;
        SetNewRandomTarget();
        lastAttackTime = -attackCooldown;
        //lastRaftAttackTime = -raftAttackCooldown;
        lastRaftAttackTime = 0;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) 
            {
                player = playerObject.transform;
                playerScript = playerObject.GetComponent<Player>();
                IgnorePlayerCollision();
            }
        }

        if (currentState != SharkState.AttackingRaft)
            KeepTargetBelowRaftIfNeeded();

        // Random change to attack raft if the shark is roaming
        if (currentState == SharkState.Roaming && Time.time > lastRaftAttackTime + raftAttackCooldown)
            TryStartRaftAttack();

        switch (currentState)
        {
            case SharkState.Roaming:
                HandleRoaming();
                break;
            case SharkState.Hunting:
                HandleHunting();
                break;
            case SharkState.AttackingPlayer:
                HandleAttackingPlayer();
                break;
            case SharkState.AttackingRaft:
                HandleAttackingRaft();
                break;
        }
    }

    void HandleRoaming()
    {
        if (Vector3.Distance(transform.position, targetPosition) < 2f)
            SetNewRandomTarget();

        MoveTowardsTarget();

        if (player == null || Time.time < lastAttackTime + attackCooldown)
            return;

        if (player.position.y < waterLevel && Vector3.Distance(transform.position, player.position) < playerDetectionRadius)
        {
            Debug.Log("[Shark] Hunting player");
            currentState = SharkState.Hunting;
            huntStartTime = Time.time;
        }
    }

    void HandleHunting()
    {
        if (player == null || player.position.y >= waterLevel)
        {
            Debug.Log("[Shark] Roaming around");
            currentState = SharkState.Roaming;
            return;
        }

        // Safety net: if the shark has been chasing for too long without landing a
        // hit (e.g. player kiting it against geometry), give up and roam instead of
        // hunting/dragging the player forever.
        if (Time.time > huntStartTime + maxHuntDuration)
        {
            Debug.Log("[Shark] Hunt timed out, roaming around");
            lastAttackTime = Time.time;
            currentState = SharkState.Roaming;
            SetNewRandomTarget();
            return;
        }

        targetPosition = player.position;
        MoveTowardsTarget();

        if (Vector3.Distance(transform.position, player.position) < playerAttackDistance)
        {
            Debug.Log("[Shark] Attacking player");
            currentState = SharkState.AttackingPlayer;
        }
    }

    void HandleAttackingPlayer()
    {
        if (playerScript != null)
        {
            playerScript.TakeDamage(sharkDamage);
            Debug.Log("[Shark] Shark attacked the player.");
        }

        lastAttackTime = Time.time;
        currentState = SharkState.Roaming;
        SetNewRandomTarget();
    }

    void TryStartRaftAttack()
    {
        if (Random.value > raftHitChance)
            return;

        // Always ram a raft piece from below (this is what produces the shake).
        Collider raftCol = GetRandomRaftCollider();
        if (raftCol == null)
            return;

        raftTarget = raftCol;
        targetPosition = raftTarget.bounds.center;

        // If there are barricades, also wear one down during this same attack.
        barricadeTarget = GetRandomBarricade();

        currentState = SharkState.AttackingRaft;

        Debug.Log("[Shark] Attacking raft: " + raftCol.name);
    }

    void HandleAttackingRaft()
    {
        if (raftTarget == null)
        {
            currentState = SharkState.Roaming;
            SetNewRandomTarget();
            return;
        }

        // Ram the raft from below (same as before) so it bumps/shakes the raft and never phases through it.
        targetPosition = raftTarget.bounds.center;
        targetPosition.y = Mathf.Min(targetPosition.y, raftAttackMaxY);
        MoveTowardsTarget();

        float distance = Vector3.Distance(transform.position, targetPosition);
        if (distance < 2f)
        {
            // If there are barricades, wear one down on this hit.
            if (barricadeTarget != null && !barricadeTarget.IsDestroyed())
            {
                barricadeTarget.TakeDamage(1);
                Debug.Log("[Shark] Attacked barricade: " + barricadeTarget.name);
            }

            lastRaftAttackTime = Time.time;
            raftTarget = null;
            barricadeTarget = null;
            currentState = SharkState.Roaming;
            SetNewRandomTarget();
        }
    }

    void SetNewRandomTarget()
    {
        targetPosition = centerPoint + Random.insideUnitSphere * swimRadius;
        targetPosition.y = Random.value < surfaceSwimChance ? surfaceLevel : deepLevel;

        if (TryGetSafeDepthUnderRaft(targetPosition, out float safeDepth) && safeDepth < targetPosition.y)
        {
            targetPosition.y = safeDepth;
        }
    }

    void KeepTargetBelowRaftIfNeeded()
    {
        if (currentState == SharkState.AttackingRaft)
            return;

        if (TryGetSafeDepthUnderRaft(transform.position, out float safeDepth))
        {
            targetPosition.y = Mathf.Min(targetPosition.y, safeDepth);
        }
    }

    bool TryGetSafeDepthUnderRaft(Vector3 point, out float safeDepth)
    {
        safeDepth = float.PositiveInfinity;
        bool found = false;

        foreach (Collider c in GetRaftColliders())
        {
            Bounds b = c.bounds;
            if (point.x < b.min.x - raftDetectionPadding || point.x > b.max.x + raftDetectionPadding ||
                point.z < b.min.z - raftDetectionPadding || point.z > b.max.z + raftDetectionPadding)
                continue;

            found = true;
            safeDepth = Mathf.Min(safeDepth, b.min.y - raftClearanceBuffer - raftDetectionPadding);
        }

        return found;
    }

    Barricade GetRandomBarricade()
    {
        List<Barricade> barricades = CollectBarricades();

        if (barricades.Count == 0)
            return null;

        return barricades[Random.Range(0, barricades.Count)];
    }

    List<Barricade> CollectBarricades()
    {
        List<Barricade> barricades = new List<Barricade>();

        foreach (GameObject barricadeObj in GameObject.FindGameObjectsWithTag("Barricade"))
        {
            Barricade barricade = barricadeObj.GetComponent<Barricade>();
            if (barricade != null && !barricade.IsDestroyed())
            {
                barricades.Add(barricade);
            }
        }

        return barricades;
    }

    Collider GetRandomRaftCollider()
    {
        List<Collider> colliders = CollectRaftColliders();

        if (colliders.Count == 0)
            return null;

        return colliders[Random.Range(0, colliders.Count)];
    }

    Collider[] GetRaftColliders()
    {
        return CollectRaftColliders().ToArray();
    }

    List<Collider> CollectRaftColliders()
    {
        List<Collider> colliders = new List<Collider>();

        foreach (GameObject raft in GameObject.FindGameObjectsWithTag("Raft"))
        {
            foreach (Collider c in raft.GetComponentsInChildren<Collider>())
            {
                if (!c.CompareTag("Player"))
                    colliders.Add(c);
            }
        }

        return colliders;
    }

    // The shark keeps a SOLID collider so it can ram the raft, but that same solid
    // collider would otherwise physically shove the player around (and prevent the
    // shark center from ever reaching playerAttackDistance, so it would "hunt"
    // forever). Ignoring shark<->player collision lets the shark close the distance
    // and trigger its attack while staying solid against the raft.
    void IgnorePlayerCollision()
    {
        if (playerCollisionIgnored || sharkCollider == null || player == null)
            return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
        foreach (Collider pc in playerColliders)
        {
            if (pc != null)
                Physics.IgnoreCollision(sharkCollider, pc, true);
        }

        playerCollisionIgnored = true;
        Debug.Log("[Shark] Ignoring shark<->player physical collision so it can reach attack range");
    }

    void MoveTowardsTarget()
    {
        Vector3 dir = (targetPosition - transform.position).normalized;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    public void OnAttacked(Vector3 attackerPosition)
    {
        Debug.Log("[Shark] Attacked by player at " + attackerPosition + " (state=" + currentState + ")", this);
        
        Inventory inventory = FindFirstObjectByType<Inventory>();
        if (inventory != null)
        {
            inventory.ShowScreenNotification("Attacked shark");
        }

        lastAttackTime = Time.time;
        
        // Clear targets when escaping
        barricadeTarget = null;
        raftTarget = null;
        
        if (currentState == SharkState.Roaming)
        {
            SetRandomTargetAwayFrom(attackerPosition);
            return;
        }

        currentState = SharkState.Roaming;
        SetRandomTargetAwayFrom(attackerPosition);
    }

    void SetRandomTargetAwayFrom(Vector3 fromPosition)
    {
        Vector3 awayDir = (transform.position - fromPosition).normalized;
        if (awayDir.sqrMagnitude < 0.01f)
        {
            awayDir = Random.onUnitSphere;
            awayDir.y = 0;
            awayDir.Normalize();
        }

        float distance = Random.Range(swimRadius * 0.5f, swimRadius);
        targetPosition = transform.position + awayDir * distance;

        targetPosition.y = deepLevel;

        if (TryGetSafeDepthUnderRaft(targetPosition, out float safeDepth))
        {
            targetPosition.y = Mathf.Min(targetPosition.y, safeDepth);
        }
    }
}
