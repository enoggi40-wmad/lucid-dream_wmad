using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public class GuardPatrolEntity : MonoBehaviour
{
    private enum GuardState
    {
        Patrol,
        Chase,
        CarryPlayer,
        Cooldown
    }

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string playerObjectName = "Player";
    [SerializeField] private string aiDestinationObjectName = "AIdestination";
    [SerializeField, Min(0.1f)] private float chaseDistance = 8f;
    [SerializeField, Min(0.1f)] private float catchDistance = 0.85f;
    [SerializeField, Min(0f)] private float loseSightDuration = 1.2f;
    [SerializeField, Min(0f)] private float sightHeight = 1.1f;
    [SerializeField] private LayerMask sightBlockMask = ~0;
    [SerializeField, Min(0f)] private float guardWaitAfterDrop = 3f;

    [Header("Move")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.2f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 1.7f;
    [SerializeField, Min(0.1f)] private float carryingSpeed = 2f;
    [SerializeField, Min(1f)] private float carryingSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float carriedPlayerForwardOffset = 0.8f;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.6f;
    [SerializeField, Min(0.1f)] private float repathDelay = 0.25f;
    [SerializeField, Min(0.1f)] private float randomPatrolRadius = 5f;
    [SerializeField, Min(0.05f)] private float wallCheckRadius = 0.18f;
    [SerializeField, Min(0.1f)] private float wallCheckDistance = 0.55f;
    [SerializeField, Min(0.2f)] private float stuckRecoverDelay = 1.4f;
    [SerializeField] private string[] routeObjectNames =
    {
        "\uACFC\uD559\uC2E4 \uBC14\uB2E5 (1)",
        "\uD654\uC7A5\uC2E4 \uBC14\uB2E5",
        "\uBCF5\uB3C4",
        "\uACFC\uD559\uC2E4 \uBC14\uB2E5"
    };

    [Header("Footprints")]
    [SerializeField] private GameObject footprintPrefab;
    [SerializeField, Min(0.05f)] private float footprintInterval = 1.3f;
    [SerializeField, Min(0f)] private float footprintLifetime = 10f;
    [SerializeField, Min(0.01f)] private float footprintPurifyTime = 2f;
    [SerializeField, Min(0f)] private float footprintLateralOffset = 0.18f;
    [SerializeField] private Vector3 footprintEulerOffset = Vector3.zero;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private int purifyingLayer = 9;

    [Header("Bell Audio")]
    [SerializeField] private AudioSource bellAudioSource;
    [SerializeField, Min(0.1f)] private float bellAudibleDistance = 12f;
    [SerializeField, Range(0f, 1f)] private float bellVolume = 0.45f;

    private const int SampleRate = 44100;
    private const string PurifyingLayerName = "Purifying";
    private const string FootprintHighlightGroupName = "Group#9_001";
    private const string FootprintHighlightObjectName = "FootprintHighlightObject";
    private static readonly FieldInfo HighlightObjectField = typeof(HighlightObject).GetField("highlightObject", BindingFlags.Instance | BindingFlags.NonPublic);

    private NavMeshAgent agent;
    private Transform player;
    private Transform aiDestination;
    private CharacterController capturedCharacterController;
    private PlayerController capturedPlayerController;
    private readonly List<Transform> routePoints = new List<Transform>();
    private GuardState state;
    private float repathTimer;
    private float footprintTimer;
    private float cooldownTimer;
    private float lostSightTimer;
    private float stuckTimer;
    private bool leftFoot;
    private Vector3 carryDestinationPosition;

    public void Configure(
        GameObject configuredFootprintPrefab,
        string configuredPlayerTag,
        string configuredPlayerObjectName,
        string configuredAIDestinationObjectName,
        string[] configuredRouteObjectNames,
        float configuredMoveSpeed,
        float configuredChaseSpeed,
        float configuredCarryingSpeed,
        float configuredFootprintInterval,
        float configuredFootprintLifetime,
        float configuredFootprintPurifyTime,
        int configuredPurifyingLayer)
    {
        footprintPrefab = configuredFootprintPrefab;
        playerTag = configuredPlayerTag;
        playerObjectName = configuredPlayerObjectName;
        aiDestinationObjectName = configuredAIDestinationObjectName;
        routeObjectNames = configuredRouteObjectNames;
        moveSpeed = configuredMoveSpeed;
        chaseSpeed = configuredChaseSpeed;
        carryingSpeed = configuredCarryingSpeed;
        footprintInterval = configuredFootprintInterval;
        footprintLifetime = configuredFootprintLifetime;
        footprintPurifyTime = configuredFootprintPurifyTime;
        purifyingLayer = configuredPurifyingLayer;
    }

    private void Awake()
    {
        EnsureComponents();
        SetupBellAudio();
    }

    private void Start()
    {
        EnsureComponents();
        CacheSceneObjects();
        ConfigureAgent(moveSpeed);
        TrySnapToNavMesh();
        SetNextPatrolDestination();
    }

    private void Update()
    {
        UpdateState();
        UpdateFootprints();
        UpdateBellVolume();
    }

    private void OnEnable()
    {
        if (bellAudioSource != null && bellAudioSource.clip != null && !bellAudioSource.isPlaying)
            bellAudioSource.Play();
    }

    private void OnDisable()
    {
        if (bellAudioSource != null)
            bellAudioSource.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCapture(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCapture(collision.gameObject);
    }

    public void VanishGuard()
    {
        Destroy(gameObject);
    }

    private void EnsureComponents()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
            agent = gameObject.AddComponent<NavMeshAgent>();

        Rigidbody body = GetComponent<Rigidbody>();

        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();

        if (capsule == null)
            capsule = gameObject.AddComponent<CapsuleCollider>();

        capsule.center = new Vector3(0f, 0.82f, 0f);
        capsule.radius = 0.32f;
        capsule.height = 1.65f;
        capsule.isTrigger = false;
    }

    private void CacheSceneObjects()
    {
        player = FindPlayerTransform();

        GameObject destinationObject = FindFirstExistingObject(aiDestinationObjectName, "AIDestination", "AI_Destination", "AI Destination", "AIdestination");
        aiDestination = destinationObject != null ? destinationObject.transform : null;

        routePoints.Clear();

        for (int i = 0; i < routeObjectNames.Length; i++)
        {
            GameObject routeObject = GameObject.Find(routeObjectNames[i]);

            if (routeObject != null)
                routePoints.Add(routeObject.transform);
        }
    }

    private void UpdateState()
    {
        if (agent == null || !agent.enabled || !TrySnapToNavMesh())
            return;

        RecoverIfStuck();

        if (StopBeforeWall())
            return;

        switch (state)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                TryStartChase();
                TryCaptureNearbyPlayer();
                break;
            case GuardState.Chase:
                UpdateChase();
                TryCaptureNearbyPlayer();
                break;
            case GuardState.CarryPlayer:
                UpdateCarryPlayer();
                break;
            case GuardState.Cooldown:
                cooldownTimer -= Time.deltaTime;

                if (cooldownTimer <= 0f)
                {
                    state = GuardState.Patrol;
                    ConfigureAgent(moveSpeed);
                    SetNextPatrolDestination();
                }
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance))
            SetNextPatrolDestination();
    }

    private void TryStartChase()
    {
        if (player == null)
            player = FindPlayerTransform();

        if (player == null)
            return;

        if (Vector3.Distance(transform.position, player.position) > chaseDistance)
            return;

        if (!HasClearSightToPlayer())
            return;

        state = GuardState.Chase;
        ConfigureAgent(chaseSpeed);
        repathTimer = 0f;
        lostSightTimer = 0f;
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            state = GuardState.Patrol;
            ConfigureAgent(moveSpeed);
            SetNextPatrolDestination();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > chaseDistance * 1.25f || !HasClearSightToPlayer())
            lostSightTimer += Time.deltaTime;
        else
            lostSightTimer = 0f;

        if (lostSightTimer >= loseSightDuration)
        {
            state = GuardState.Patrol;
            ConfigureAgent(moveSpeed);
            SetNextPatrolDestination();
            return;
        }

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0f)
        {
            if (!SetReachableDestination(player.position, 3f))
            {
                state = GuardState.Patrol;
                ConfigureAgent(moveSpeed);
                SetNextPatrolDestination();
                return;
            }

            repathTimer = repathDelay;
        }

        if (distanceToPlayer > chaseDistance * 1.5f)
        {
            state = GuardState.Patrol;
            ConfigureAgent(moveSpeed);
            SetNextPatrolDestination();
        }
    }

    private void UpdateCarryPlayer()
    {
        if (player != null)
        {
            player.position = GetCarriedPlayerPosition();
            player.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            player.SendMessage("ResetMovementSettings", SendMessageOptions.DontRequireReceiver);
        }

        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete))
            SetReachableDestination(carryDestinationPosition, 8f);

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= arrivalDistance)
            ReleasePlayer();
    }

    private void TryCapture(GameObject candidate)
    {
        if (state == GuardState.CarryPlayer || state == GuardState.Cooldown || candidate == null)
            return;

        GameObject playerObject = ResolvePlayerObject(candidate);

        if (playerObject == null)
            return;

        if (aiDestination == null)
        {
            Debug.LogWarning($"{gameObject.name} could not find AIdestination.");
            return;
        }

        if (!NavMesh.SamplePosition(aiDestination.position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{gameObject.name} could not find AIdestination on the NavMesh.");
            return;
        }

        if (!HasCompletePath(hit.position))
        {
            Debug.LogWarning($"{gameObject.name} could not reach AIdestination without leaving the NavMesh.");
            return;
        }

        player = playerObject.transform;
        capturedCharacterController = playerObject.GetComponent<CharacterController>();
        capturedPlayerController = playerObject.GetComponent<PlayerController>();

        if (capturedCharacterController != null)
            capturedCharacterController.enabled = false;

        if (capturedPlayerController != null)
            capturedPlayerController.enabled = false;

        state = GuardState.CarryPlayer;
        carryDestinationPosition = hit.position;
        ConfigureAgent(GetCarryingSpeed());
        agent.ResetPath();
        agent.SetDestination(carryDestinationPosition);
    }

    private void TryCaptureNearbyPlayer()
    {
        if (player == null)
            player = FindPlayerTransform();

        if (player == null)
            return;

        if (Vector3.Distance(transform.position, player.position) <= catchDistance)
            TryCapture(player.gameObject);
    }

    private void ReleasePlayer()
    {
        if (player != null && aiDestination != null)
        {
            player.position = carryDestinationPosition;
            player.SendMessage("ResetMovementSettings", SendMessageOptions.DontRequireReceiver);
        }

        if (capturedCharacterController != null)
            capturedCharacterController.enabled = true;

        if (capturedPlayerController != null)
            capturedPlayerController.enabled = true;

        capturedCharacterController = null;
        capturedPlayerController = null;
        state = GuardState.Cooldown;
        cooldownTimer = guardWaitAfterDrop;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void SetNextPatrolDestination()
    {
        if (routePoints.Count == 0 || agent == null || !agent.isOnNavMesh)
            return;

        for (int i = 0; i < routePoints.Count * 4; i++)
        {
            Transform routePoint = routePoints[Random.Range(0, routePoints.Count)];

            if (routePoint == null)
                continue;

            Vector2 randomOffset = Random.insideUnitCircle * randomPatrolRadius;
            Vector3 samplePosition = routePoint.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            if (SetReachableDestination(samplePosition, randomPatrolRadius))
            {
                return;
            }
        }
    }

    private void UpdateFootprints()
    {
        if (footprintPrefab == null || agent == null || agent.velocity.sqrMagnitude < 0.04f)
            return;

        footprintTimer -= Time.deltaTime;

        if (footprintTimer > 0f)
            return;

        SpawnFootprint(agent.velocity.normalized);
        footprintTimer = footprintInterval;
    }

    private void SpawnFootprint(Vector3 forward)
    {
        Vector3 lateral = Vector3.Cross(Vector3.up, forward).normalized;
        float side = leftFoot ? -1f : 1f;
        Vector3 origin = transform.position + lateral * footprintLateralOffset * side + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, groundMask, QueryTriggerInteraction.Ignore))
        {
            Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, hit.normal).normalized, hit.normal);
            GameObject footprint = Instantiate(footprintPrefab, hit.point + hit.normal * 0.01f, rotation * Quaternion.Euler(footprintEulerOffset));
            ConfigureFootprint(footprint);
        }

        leftFoot = !leftFoot;
    }

    private void ConfigureFootprint(GameObject footprint)
    {
        int resolvedPurifyingLayer = ResolvePurifyingLayer();
        SetLayerRecursively(footprint.transform, resolvedPurifyingLayer);
        EnsureFootprintCollider(footprint);

        FootprintPurifiable purifiable = footprint.GetComponent<FootprintPurifiable>();

        if (purifiable == null)
            purifiable = footprint.AddComponent<FootprintPurifiable>();

        HighlightObject highlight = footprint.GetComponent<HighlightObject>();

        if (highlight == null)
            highlight = footprint.AddComponent<HighlightObject>();

        GameObject highlightTarget = EnsureFootprintHighlightTarget(footprint.transform, resolvedPurifyingLayer);
        SetHighlightObject(highlight, highlightTarget);

        purifiable.Initialize(footprintLifetime, footprintPurifyTime, footprint.transform);
    }

    private int ResolvePurifyingLayer()
    {
        int layer = LayerMask.NameToLayer(PurifyingLayerName);
        return layer >= 0 ? layer : purifyingLayer;
    }

    private static GameObject EnsureFootprintHighlightTarget(Transform footprintRoot, int layer)
    {
        Transform group = FindChildRecursive(footprintRoot, FootprintHighlightGroupName);

        if (group == null)
        {
            GameObject groupObject = new GameObject(FootprintHighlightGroupName);
            groupObject.transform.SetParent(footprintRoot, false);
            groupObject.transform.localPosition = Vector3.zero;
            groupObject.transform.localRotation = Quaternion.identity;
            groupObject.transform.localScale = Vector3.one;
            groupObject.layer = layer;
            group = groupObject.transform;
        }

        Transform existingTarget = group.Find(FootprintHighlightObjectName);

        if (existingTarget != null)
        {
            existingTarget.gameObject.layer = layer;
            return existingTarget.gameObject;
        }

        GameObject target = new GameObject(FootprintHighlightObjectName);
        target.transform.SetParent(group, false);
        target.transform.localPosition = Vector3.zero;
        target.transform.localRotation = Quaternion.identity;
        target.transform.localScale = Vector3.one;
        target.layer = layer;
        return target;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);

            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void SetHighlightObject(HighlightObject highlight, GameObject highlightTarget)
    {
        if (HighlightObjectField != null && highlight != null)
            HighlightObjectField.SetValue(highlight, highlightTarget);
    }

    private static void EnsureFootprintCollider(GameObject footprint)
    {
        if (footprint.GetComponentInChildren<Collider>() != null)
            return;

        BoxCollider collider = footprint.AddComponent<BoxCollider>();
        collider.size = new Vector3(0.45f, 0.12f, 0.7f);
        collider.center = new Vector3(0f, 0.06f, 0f);
    }

    private static void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursively(target.GetChild(i), layer);
        }
    }

    private void ConfigureAgent(float speed)
    {
        agent.speed = speed;
        agent.radius = 0.32f;
        agent.height = 1.65f;
        agent.angularSpeed = 240f;
        agent.acceleration = 8f;
        agent.stoppingDistance = arrivalDistance;
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private float GetCarryingSpeed()
    {
        if (carryingSpeedMultiplier <= 0f)
            return Mathf.Max(0.1f, carryingSpeed);

        return Mathf.Max(0.1f, moveSpeed * carryingSpeedMultiplier);
    }

    private Vector3 GetCarriedPlayerPosition()
    {
        Vector3 offset = transform.forward * carriedPlayerForwardOffset;
        Vector3 position = transform.position + offset;

        if (player != null)
            position.y = player.position.y;

        return position;
    }

    private bool TrySnapToNavMesh()
    {
        if (agent.isOnNavMesh)
            return true;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 20f, NavMesh.AllAreas))
            return false;

        agent.Warp(hit.position);
        return agent.isOnNavMesh;
    }

    private bool SetReachableDestination(Vector3 destination, float sampleDistance)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, sampleDistance, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete || !IsPhysicsClearPath(path))
            return false;

        agent.SetPath(path);
        return true;
    }

    private bool HasCompletePath(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        return agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete && IsPhysicsClearPath(path);
    }

    private bool IsPhysicsClearPath(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return true;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Vector3 start = path.corners[i] + Vector3.up * 0.8f;
            Vector3 end = path.corners[i + 1] + Vector3.up * 0.8f;
            Vector3 segment = end - start;
            float distance = segment.magnitude;

            if (distance <= 0.01f)
                continue;

            if (!Physics.SphereCast(start, wallCheckRadius, segment.normalized, out RaycastHit hit, distance, sightBlockMask, QueryTriggerInteraction.Ignore))
                continue;

            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || ResolvePlayerObject(hit.collider.gameObject) != null)
                continue;

            return false;
        }

        return true;
    }

    private bool StopBeforeWall()
    {
        if (agent == null || !agent.hasPath || agent.desiredVelocity.sqrMagnitude < 0.05f)
            return false;

        Vector3 direction = Vector3.ProjectOnPlane(agent.desiredVelocity, Vector3.up).normalized;
        Vector3 origin = transform.position + Vector3.up * 0.8f;

        if (!Physics.SphereCast(origin, wallCheckRadius, direction, out RaycastHit hit, wallCheckDistance, sightBlockMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || ResolvePlayerObject(hit.collider.gameObject) != null)
            return false;

        agent.ResetPath();
        agent.velocity = Vector3.zero;

        if (state == GuardState.Patrol || state == GuardState.Chase)
        {
            state = GuardState.Patrol;
            ConfigureAgent(moveSpeed);
            SetNextPatrolDestination();
        }

        return true;
    }

    private void RecoverIfStuck()
    {
        if (agent == null || state == GuardState.CarryPlayer || state == GuardState.Cooldown)
        {
            stuckTimer = 0f;
            return;
        }

        if (!agent.hasPath || agent.pathPending || agent.remainingDistance <= arrivalDistance)
        {
            stuckTimer = 0f;
            return;
        }

        bool barelyMoving = agent.velocity.sqrMagnitude < 0.01f && agent.desiredVelocity.sqrMagnitude > 0.05f;

        if (!barelyMoving)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.deltaTime;

        if (stuckTimer < stuckRecoverDelay)
            return;

        stuckTimer = 0f;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
        state = GuardState.Patrol;
        ConfigureAgent(moveSpeed);
        SetNextPatrolDestination();
    }

    private bool HasClearSightToPlayer()
    {
        if (player == null)
            return false;

        Vector3 start = transform.position + Vector3.up * sightHeight;
        Vector3 end = player.position + Vector3.up * sightHeight;
        Vector3 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(start, direction.normalized, distance, sightBlockMask, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        RaycastHit nearestHit = default;
        bool foundHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearestHit = hits[i];
                foundHit = true;
            }
        }

        if (!foundHit)
            return true;

        return ResolvePlayerObject(nearestHit.collider.gameObject) != null;
    }

    private Transform FindPlayerTransform()
    {
        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();

        if (playerController != null)
            return playerController.transform;

        if (!string.IsNullOrEmpty(playerObjectName))
        {
            GameObject namedPlayer = GameObject.Find(playerObjectName);

            if (namedPlayer != null)
                return namedPlayer.transform;
        }

        if (!string.IsNullOrEmpty(playerTag))
        {
            try
            {
                GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);

                if (taggedPlayer != null)
                    return taggedPlayer.transform;
            }
            catch (UnityException)
            {
            }
        }

        return null;
    }

    private bool IsPlayer(GameObject candidate)
    {
        return ResolvePlayerObject(candidate) != null;
    }

    private GameObject ResolvePlayerObject(GameObject candidate)
    {
        if (candidate == null)
            return null;

        PlayerController playerController = candidate.GetComponentInParent<PlayerController>();

        if (playerController != null)
            return playerController.gameObject;

        Transform current = candidate.transform;

        while (current != null)
        {
            if (!string.IsNullOrEmpty(playerObjectName) && current.name == playerObjectName)
                return current.gameObject;

            if (!string.IsNullOrEmpty(playerTag) && current.CompareTag(playerTag))
                return current.gameObject;

            current = current.parent;
        }

        return null;
    }

    private void SetupBellAudio()
    {
        if (bellAudioSource == null)
        {
            GameObject audioObject = new GameObject("BellAudioSource");
            audioObject.transform.SetParent(transform, false);
            bellAudioSource = audioObject.AddComponent<AudioSource>();
        }

        bellAudioSource.clip = CreateBellClip();
        bellAudioSource.loop = true;
        bellAudioSource.playOnAwake = false;
        bellAudioSource.spatialBlend = 1f;
        bellAudioSource.rolloffMode = AudioRolloffMode.Linear;
        bellAudioSource.minDistance = 1f;
        bellAudioSource.maxDistance = bellAudibleDistance;
        bellAudioSource.volume = bellVolume;

        if (isActiveAndEnabled && !bellAudioSource.isPlaying)
            bellAudioSource.Play();
    }

    private void UpdateBellVolume()
    {
        if (bellAudioSource == null)
            return;

        if (player == null)
            player = FindPlayerTransform();

        if (player == null)
        {
            bellAudioSource.volume = bellVolume;
            return;
        }

        float distance = Vector3.Distance(player.position, transform.position);
        float distance01 = Mathf.Clamp01(1f - distance / bellAudibleDistance);
        bellAudioSource.volume = bellVolume * distance01;
    }

    private static AudioClip CreateBellClip()
    {
        float duration = 1.2f;
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)SampleRate;
            float strike = Mathf.Exp(-Mathf.Repeat(time, 0.6f) * 7f);
            float toneA = Mathf.Sin(2f * Mathf.PI * 1460f * time);
            float toneB = Mathf.Sin(2f * Mathf.PI * 2190f * time) * 0.35f;
            samples[i] = (toneA + toneB) * strike * 0.18f;
        }

        AudioClip clip = AudioClip.Create("GeneratedGuardBell", sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static GameObject FindFirstExistingObject(params string[] objectNames)
    {
        for (int i = 0; i < objectNames.Length; i++)
        {
            if (string.IsNullOrEmpty(objectNames[i]))
                continue;

            GameObject found = GameObject.Find(objectNames[i]);

            if (found != null)
                return found;
        }

        return null;
    }
}
