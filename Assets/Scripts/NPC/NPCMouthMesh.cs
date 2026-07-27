using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class NPCMouthMesh : MonoBehaviour
{
    public enum MouthExpression
    {
        Neutral,
        Happy,
        Sad,
        Open,
        Surprised,
        SmirkLeft,
        SmirkRight
    }

    [Header("Current Expression")]
    [SerializeField]
    private MouthExpression expression = MouthExpression.Neutral;

    [Header("Expression Strength")]

    [Tooltip("Curvature of smiles and frowns, measured as a proportion of mouth width.")]
    [SerializeField, Range(0f, 0.3f)]
    private float curveStrength = 0.08f;

    [Tooltip("How far the mouth opens, measured as a proportion of mouth width.")]
    [SerializeField, Range(0f, 0.3f)]
    private float openStrength = 0.08f;

    [Tooltip("Width of the surprised mouth compared with its neutral width.")]
    [SerializeField, Range(0.1f, 1f)]
    private float surprisedWidth = 0.35f;

    [Header("Animation")]

    [Tooltip("How quickly the mouth changes between expressions.")]
    [SerializeField, Min(0f)]
    private float blendSpeed = 12f;

    private SkinnedMeshRenderer mouthRenderer;
    private Mesh runtimeMesh;

    private Vector3[] neutralVertices;
    private Vector3[] currentVertices;
    private Vector3[] targetVertices;

    private MouthExpression previousExpression;

    private float minimumX;
    private float maximumX;
    private float centreX;

    private float minimumY;
    private float maximumY;
    private float centreY;

    private float mouthWidth;
    private float mouthHeight;

    private bool initialized;

    private void Awake()
    {
        InitializeMouth();
    }

    private void Update()
    {
        if (!initialized)
            return;

        // Allows us to change the enum directly in the Inspector during Play Mode.
        if (expression != previousExpression)
        {
            previousExpression = expression;
            BuildTargetExpression(expression);
        }

        AnimateVertices();
    }

    private void InitializeMouth()
    {
        mouthRenderer = GetComponent<SkinnedMeshRenderer>();

        if (mouthRenderer.sharedMesh == null)
        {
            Debug.LogError(
                $"{name}: NPCMouthMesh could not find a mesh.",
                this
            );

            enabled = false;
            return;
        }

        // Clone the imported mesh so we never alter the original FBX asset.
        runtimeMesh = Instantiate(mouthRenderer.sharedMesh);
        runtimeMesh.name = $"{mouthRenderer.sharedMesh.name}_Runtime";

        mouthRenderer.sharedMesh = runtimeMesh;

        neutralVertices = runtimeMesh.vertices;
        currentVertices = (Vector3[])neutralVertices.Clone();
        targetVertices = (Vector3[])neutralVertices.Clone();

        CalculateMouthDimensions();

        previousExpression = expression;
        BuildTargetExpression(expression);

        initialized = true;

        Debug.Log(
            $"{name}: Mouth initialized with {neutralVertices.Length} vertices.",
            this
        );
    }

    private void CalculateMouthDimensions()
    {
        minimumX = float.PositiveInfinity;
        maximumX = float.NegativeInfinity;

        minimumY = float.PositiveInfinity;
        maximumY = float.NegativeInfinity;

        foreach (Vector3 vertex in neutralVertices)
        {
            minimumX = Mathf.Min(minimumX, vertex.x);
            maximumX = Mathf.Max(maximumX, vertex.x);

            minimumY = Mathf.Min(minimumY, vertex.y);
            maximumY = Mathf.Max(maximumY, vertex.y);
        }

        centreX = (minimumX + maximumX) * 0.5f;
        centreY = (minimumY + maximumY) * 0.5f;

        mouthWidth = maximumX - minimumX;
        mouthHeight = maximumY - minimumY;

        if (mouthWidth <= Mathf.Epsilon)
        {
            Debug.LogError(
                $"{name}: The mouth has no measurable width on its local X axis.",
                this
            );

            enabled = false;
        }
    }

    private void BuildTargetExpression(MouthExpression newExpression)
    {
        for (int i = 0; i < neutralVertices.Length; i++)
        {
            Vector3 neutral = neutralVertices[i];
            Vector3 result = neutral;

            float horizontalPosition = Mathf.InverseLerp(
                minimumX,
                maximumX,
                neutral.x
            );

            // Converts the horizontal position to -1 on the left,
            // 0 in the centre and 1 on the right.
            float horizontalSigned = (horizontalPosition * 2f) - 1f;

            float distanceFromCentre = Mathf.Abs(horizontalSigned);

            bool isTopVertex = neutral.y > centreY;

            switch (newExpression)
            {
                case MouthExpression.Happy:
                    result.y += CalculateSmileCurve(distanceFromCentre);
                    break;

                case MouthExpression.Sad:
                    result.y -= CalculateSmileCurve(distanceFromCentre);
                    break;

                case MouthExpression.Open:
                    result.y += isTopVertex
                        ? mouthWidth * openStrength
                        : -mouthWidth * openStrength;
                    break;

                case MouthExpression.Surprised:
                    result.x = centreX +
                               ((neutral.x - centreX) * surprisedWidth);

                    result.y += isTopVertex
                        ? mouthWidth * openStrength * 1.4f
                        : -mouthWidth * openStrength * 1.4f;
                    break;

                case MouthExpression.SmirkLeft:
                    result.y += CalculateSmirk(horizontalPosition, false);
                    break;

                case MouthExpression.SmirkRight:
                    result.y += CalculateSmirk(horizontalPosition, true);
                    break;

                case MouthExpression.Neutral:
                default:
                    break;
            }

            targetVertices[i] = result;
        }
    }

    private float CalculateSmileCurve(float distanceFromCentre)
    {
        /*
         * Centre vertices move downward.
         * Corner vertices move upward.
         *
         * Centre: -curve amount
         * Corners: +curve amount
         */

        float curve = Mathf.Lerp(-1f, 1f, distanceFromCentre);
        return curve * mouthWidth * curveStrength;
    }

    private float CalculateSmirk(
        float horizontalPosition,
        bool raiseRightSide)
    {
        float sideWeight = raiseRightSide
            ? horizontalPosition
            : 1f - horizontalPosition;

        return sideWeight * mouthWidth * curveStrength;
    }

    private void AnimateVertices()
    {
        bool verticesChanged = false;

        float movementAmount = blendSpeed * Time.deltaTime;

        for (int i = 0; i < currentVertices.Length; i++)
        {
            Vector3 previousPosition = currentVertices[i];

            currentVertices[i] = Vector3.Lerp(
                currentVertices[i],
                targetVertices[i],
                1f - Mathf.Exp(-movementAmount)
            );

            if ((currentVertices[i] - previousPosition).sqrMagnitude > 0.00000001f)
                verticesChanged = true;
        }

        if (!verticesChanged)
            return;

        runtimeMesh.vertices = currentVertices;
        runtimeMesh.RecalculateBounds();
    }

    public void SetExpression(MouthExpression newExpression)
    {
        expression = newExpression;

        if (!initialized)
            return;

        previousExpression = newExpression;
        BuildTargetExpression(newExpression);
    }

    public void ReturnToNeutral()
    {
        SetExpression(MouthExpression.Neutral);
    }

    public MouthExpression GetExpression()
    {
        return expression;
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
            Destroy(runtimeMesh);
    }
}