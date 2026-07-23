using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public sealed class NavMeshDestinationTest : MonoBehaviour
{
    [SerializeField] private Transform destination;

    [Header("Animation Matching")]
    [Tooltip("How fast the character should travel when the walk animation plays at normal speed.")]
    [SerializeField] private float animationReferenceSpeed = 1.8f;

    [SerializeField] private float animationDampTime = 0.1f;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int WalkSpeedMultiplierHash =
        Animator.StringToHash("WalkSpeedMultiplier");

    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (destination == null)
        {
            Debug.LogError($"{name} has no destination assigned.", this);
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"{name} is not standing on a NavMesh.", this);
            enabled = false;
            return;
        }

        agent.SetDestination(destination.position);
    }

    private void Update()
    {
        float actualSpeed = agent.velocity.magnitude;

        // Controls Idle <-> Walk.
        animator.SetFloat(
            SpeedHash,
            actualSpeed,
            animationDampTime,
            Time.deltaTime);

        // Slows or speeds the walk cycle to match the agent.
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
}