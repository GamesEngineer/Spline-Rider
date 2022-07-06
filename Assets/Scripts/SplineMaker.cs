using UnityEngine;

public class SplineMaker : MonoBehaviour
{
    [SerializeField, HideInInspector]
    public Spline spline;

    public const int NUM_LINE_STEPS = 20;
    public const float ANCHOR_SIZE = 0.5f;

    private void Awake()
    {
        if (spline == null) Reset();
    }

    private void Reset()
    {
        spline = new Spline();
    }

    private void OnDrawGizmos()
    {
        if (spline == null) return;

        //DrawGizmosBounds(Color.gray);
        Gizmos.color = Color.white;
        for (int segmentIndex = 0; segmentIndex < spline.SegmentCount; segmentIndex++)
        {
            Vector3 lineStart = GetPointAt(segmentIndex, 0f);
            Gizmos.DrawSphere(lineStart, ANCHOR_SIZE);
            for (int i = 1; i <= NUM_LINE_STEPS; i++)
            {
                float t = (float)i / NUM_LINE_STEPS;
                Vector3 lineEnd = GetPointAt(segmentIndex, t);
                Gizmos.DrawLine(lineStart, lineEnd);
                lineStart = lineEnd;
            }
        }
        Gizmos.DrawSphere(GetPointAt(spline.SegmentCount - 1, 1f), ANCHOR_SIZE);
    }

    private void DrawGizmosBounds(Color color)
    {
        Gizmos.color = color;
        Bounds bounds = new Bounds(transform.position, Vector3.one * 0.05f);
        for (int i = 0; i < spline.PointCount; i++)
        {
            bounds.Encapsulate(transform.TransformPoint(spline[i]));
        }
        bounds.Expand(Vector3.one * 0.05f);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    public Vector3 this[int pointIndex]
    {
        get => transform.TransformPoint(spline[pointIndex]);
        set => spline[pointIndex] = transform.InverseTransformPoint(value);
    }

    public Vector3 GetPointAt(float t) => transform.TransformPoint(spline.GetPointAt(t));
    public Vector3 GetPointAt(int segmentIndex, float t) => transform.TransformPoint(spline.GetPointAt(segmentIndex, t));
    public Vector3 GetVelocityAt(float t) => transform.TransformDirection(spline.GetVelocityAt(t));
    public Vector3 GetVelocityAt(int segmentIndex, float t) => transform.TransformDirection(spline.GetVelocityAt(segmentIndex, t));
    public Vector3 GetDirectionAt(float t) => transform.TransformDirection(spline.GetDirectionAt(t));
    public Vector3 GetDirectionAt(int segmentIndex, float t) => transform.TransformDirection(spline.GetDirectionAt(segmentIndex, t));
    public void MovePoint(int pointIndex, Vector3 newPositionWS, bool updateHandles) => spline.MovePoint(pointIndex, transform.InverseTransformPoint(newPositionWS), updateHandles);
    public void SplitSegment(Vector3 pointWS, int segmentIndex) => spline.SplitSegment(transform.InverseTransformPoint(pointWS), segmentIndex);
    public void AddSegment(Vector3 pointWS) => spline.AddSegment(transform.InverseTransformPoint(pointWS));
}
