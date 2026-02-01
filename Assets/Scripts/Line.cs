using UnityEngine;

public class Line : MonoBehaviour
{
    [SerializeField] private Transform[] anchors;

    [SerializeField, Min(2)]
    private int numVertices = 10; // per segment
    public float lineWidth=1f;

    private LineRenderer lineRenderer;
    private Vector3[] cachedPoints;
    float baseLength;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetAnchors(anchors);
    }

    private void Update()
    {
        UpdateCurve();
    }

    public void SetAnchors(Transform[] anchors)
    {
        this.anchors=anchors;
        baseLength=CalculateLineLength();
    }

    // =========================
    // Main Flow
    // =========================

    private void UpdateCurve()
    {
        if (!IsValid())
            return;

        SampleCurve();
        UpdateLineRenderer();
    }

    // =========================
    // Validation
    // =========================

    private bool IsValid()
    {
        return anchors != null &&
               anchors.Length >= 2 &&
               lineRenderer != null;
    }

    // =========================
    // Curve Sampling
    // =========================

    private void SampleCurve()
    {
        int segmentCount = anchors.Length - 1;
        int totalPoints = segmentCount * numVertices;

        EnsureBufferSize(totalPoints);

        int index = 0;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 p0 = anchors[i].position;
            Vector3 p2 = anchors[i + 1].position;
            Vector3 control = ComputeControlPoint(i);

            for (int j = 0; j < numVertices; j++)
            {
                float t = j / (float)(numVertices - 1);
                cachedPoints[index++] = GetQuadraticBezierPoint(p0, control, p2, t);
            }
        }
    }

    // =========================
    // Control Point Logic
    // =========================

    private Vector3 ComputeControlPoint(int index)
    {
        Vector3 current = anchors[index].position;

        // Endpoints: straight line
        if (index == 0 || index == anchors.Length - 2)
        {
            Vector3 next = anchors[index + 1].position;
            return (current + next) * 0.5f;
        }

        Vector3 prev = anchors[index - 1].position;
        Vector3 nextPoint = anchors[index + 1].position;

        Vector3 tangent = (nextPoint - prev) * 0.5f;
        return current + tangent;
    }

    // =========================
    // Bezier Math
    // =========================

    private Vector3 GetQuadraticBezierPoint(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        float t)
    {
        float u = 1f - t;
        return u * u * p0 +
               2f * u * t * p1 +
               t * t * p2;
    }

    // =========================
    // Rendering
    // =========================

    private void EnsureBufferSize(int count)
    {
        if (cachedPoints != null && cachedPoints.Length == count)
            return;

        cachedPoints = new Vector3[count];
        lineRenderer.positionCount = count;
    }

    float CalculateLineLength()
    {
        float res=0;
        for(int i = 1; i < anchors.Length; ++i)
        {
            res+=Vector2.Distance(anchors[i-1].position, anchors[i].position);
        }
        return res;
    }

    float WidthCurve(float t)
    {
        t-=1f;
        t/=4f;
        return 1/(t+1);
    }
    private void UpdateLineRenderer()
    {
        lineRenderer.SetPositions(cachedPoints);
        float width=lineWidth*WidthCurve(CalculateLineLength()/baseLength);
        lineRenderer.startWidth=width;
        lineRenderer.endWidth=width;
    }
}