using UnityEngine;

[RequireComponent(typeof(Animator))]
public class NPCHeadTracking : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Detection")]
    [SerializeField] private float detectionDistance = 5f;

    [Header("Look Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float lookWeight = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float headWeight = 0.85f;

    [Range(0f, 1f)]
    [SerializeField] private float bodyWeight = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float clampWeight = 0.65f;

    [Header("Smoothing")]
    [SerializeField] private float lookSpeed = 4f;

    private Animator animator;
    private float currentWeight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    private void Update()
    {
        float desiredWeight = 0f;

        if (target != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distance <= detectionDistance)
                desiredWeight = lookWeight;
        }

        currentWeight = Mathf.MoveTowards(
            currentWeight,
            desiredWeight,
            lookSpeed * Time.deltaTime
        );
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (target == null)
            return;

        animator.SetLookAtWeight(
            currentWeight,
            bodyWeight,
            headWeight,
            0f,
            clampWeight
        );

        animator.SetLookAtPosition(target.position);
    }
}