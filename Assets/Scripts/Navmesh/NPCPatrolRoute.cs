using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public sealed class NPCPatrolRoute : MonoBehaviour
{
    [System.Serializable]
    private struct PatrolPoint
    {
        [Tooltip("The world-space destination for this point.")]
        public Transform destination;

        [Tooltip("Stop and play the Look Around animation here.")]
        public bool lookAroundHere;

        [Tooltip("How long the NPC remains stopped at this point.")]
        [Min(0.1f)]
        public float lookDuration;
    }

    [Header("Patrol Route")]
    [Tooltip("Points are visited from top to bottom, then the route loops.")]
    [SerializeField] private PatrolPoint[] patrolPoints =
        new PatrolPoint[6];

    [Tooltip("How close the agent must get before the point counts as reached.")]
    [SerializeField, Min(0.05f)]
    private float waypointReachDistance = 0.35f;

    [Tooltip("Small pause allowing the walk animation to settle into idle.")]
    [SerializeField, Min(0f)]
    private float settleBeforeLooking = 0.15f;

    [Header("Animation Matching")]
    [Tooltip("How fast the character should travel when the walk animation plays at normal speed.")]
    [SerializeField, Min(0.01f)]
    private float animationReferenceSpeed = 3f;

    [SerializeField, Min(0f)]
    private float animationDampTime = 0.1f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int WalkSpeedMultiplierHash =
        Animator.StringToHash("WalkSpeedMultiplier");

    private static readonly int LookAroundHash =
        Animator.StringToHash("LookAround");

    private NavMeshAgent agent;
    private Animator animator;

    private int currentPointIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        StartCoroutine(PatrolLoop());
    }

    private void Update()
    {
        UpdateMovementAnimation();
    }

    private IEnumerator PatrolLoop()
    {
        while (true)
        {
            PatrolPoint targetPoint = patrolPoints[currentPointIndex];

            // Continue smoothly through ordinary points, but brake when
            // approaching a point where the guard must stop and look.
            agent.autoBraking = targetPoint.lookAroundHere;
            agent.isStopped = false;

            if (!agent.SetDestination(targetPoint.destination.position))
            {
                Debug.LogError(
                    $"{name} could not set a path to patrol point " +
                    $"{currentPointIndex + 1}.",
                    this);

                enabled = false;
                yield break;
            }

            // Give the NavMeshAgent one frame to begin calculating its path.
            yield return null;

            yield return WaitUntilPointReached(
                targetPoint.lookAroundHere);

            if (targetPoint.lookAroundHere)
            {
                yield return PlayLookAround(
                    targetPoint.lookDuration);
            }

            currentPointIndex++;

            if (currentPointIndex >= patrolPoints.Length)
            {
                currentPointIndex = 0;
            }
        }
    }

    private IEnumerator WaitUntilPointReached(bool mustComeToStop)
    {
        float requiredDistance = Mathf.Max(
            waypointReachDistance,
            agent.stoppingDistance);

        while (true)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= requiredDistance)
            {
                // Ordinary route points can be passed through without
                // forcing the guard to stop.

                if (!mustComeToStop ||
                    agent.velocity.sqrMagnitude <= 0.01f)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    private IEnumerator PlayLookAround(float duration)
    {
        agent.isStopped = true;
        agent.ResetPath();

        if (settleBeforeLooking > 0f)
        {
            yield return new WaitForSeconds(
                settleBeforeLooking);
        }

        animator.ResetTrigger(LookAroundHash);
        animator.SetTrigger(LookAroundHash);

        yield return new WaitForSeconds(
            Mathf.Max(0.1f, duration));
    }

    private void UpdateMovementAnimation()
    {
        float actualSpeed = agent.velocity.magnitude;

        // Controls Idle <-> Walk.
        animator.SetFloat(
            SpeedHash,
            actualSpeed,
            animationDampTime,
            Time.deltaTime);

        // Matches the walk-cycle playback speed to the agent's speed.
        float playbackMultiplier =
            animationReferenceSpeed > 0.01f
                ? actualSpeed / animationReferenceSpeed
                : 1f;

        animator.SetFloat(
            WalkSpeedMultiplierHash,
            playbackMultiplier,
            animationDampTime,
            Time.deltaTime);
    }

    private bool ValidateSetup()
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name} is not standing on a NavMesh.",
                this);

            return false;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError(
                $"{name} has no patrol points.",
                this);

            return false;
        }

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i].destination == null)
            {
                Debug.LogError(
                    $"{name} has no destination assigned to " +
                    $"patrol point {i + 1}.",
                    this);

                return false;
            }
        }

        return true;
    }
}