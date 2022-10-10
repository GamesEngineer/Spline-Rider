using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Spline
{
    [SerializeField, HideInInspector]
    private List<Vector3> points = new List<Vector3>();

    public bool IsClosed; // CHALLENGE! Implement the ability to automatically maintain closed-loop splines.

    public Spline()
    {
        AddDefaultSegment();
    }

    public void AddDefaultSegment()
    {
        points.Add(new Vector3(-2f, 0f,  0f));
        points.Add(new Vector3(-1f, 0f,  2f));
        points.Add(new Vector3( 1f, 0f, -2f));
        points.Add(new Vector3( 2f, 0f,  0f));
    }

    public void AddSegment(Vector3 newAnchorPoint)
    {
        // Add three new points to the spline:
        //  1) newControlPoint1 = a point that mirrors the prior control point relative to its anchor point
        //  2) newControlPoint2 = a point that is halfway between the newControlPoint1 and the newAnchorPoint
        //  3) newAnchorPoint
        Vector3 priorControlPoint = points[points.Count - 2];
        Vector3 lastAnchorPoint = points[points.Count - 1];
        Vector3 newControlPoint1 = lastAnchorPoint * 2 - priorControlPoint;
        Vector3 newControlPoint2 = (newControlPoint1 + newAnchorPoint) / 2f;
        points.Add(newControlPoint1);
        points.Add(newControlPoint2);
        points.Add(newAnchorPoint);
    }

    public void SplitSegment(Vector3 newAnchorPoint, int segmentIndex)
    {
        int anchorIndex = segmentIndex * 3;
        var newPoints = new Vector3[] { Vector3.one, newAnchorPoint, -Vector3.one };
        points.InsertRange(anchorIndex + 2, newPoints);
        anchorIndex = (segmentIndex + 1) * 3;
        UpdateHandles(anchorIndex);
    }

    public void DeleteSegment(int segmentIndex)
    {
        if (SegmentCount <= 1) return;

        if (IsClosed)
        {
            // TODO - what if the segment is the one that closes the loop?
        }
        else
        {
            int anchorIndex = segmentIndex * 3;
            points.RemoveRange(anchorIndex, 3);
        }
    }

    public Vector3 this[int pointIndex]
    {
        get => points[pointIndex];
        set => points[pointIndex] = value;
    }

    public int PointCount => points.Count;

    public int SegmentCount => points.Count / 3;

    public int PointIndex(int i) => (i + points.Count) % points.Count;

    public Vector3 GetPointAt(float t)
    {
        if (points.Count < 4) return Vector3.zero;
        float s = Mathf.Clamp(t * SegmentCount, 0, (float)SegmentCount - 1.0e-5f);
        int segmentIndex = Mathf.FloorToInt(s);
        t = Mathf.Repeat(s, 1f);
        return GetPointAt(segmentIndex, t);
    }

    public Vector3 GetVelocityAt(float t)
    {
        if (points.Count < 4) return Vector3.right;
        int segmentIndex = Mathf.FloorToInt(t * (SegmentCount - 1));
        t = Mathf.Repeat(t * SegmentCount, 1f);
        return GetVelocityAt(segmentIndex, t);
    }

    public Vector3 GetDirectionAt(float t) => GetVelocityAt(t).normalized;

    public Vector3 GetPointAt(int segmentIndex, float t)
    {
        if (points.Count < 4) return Vector3.zero;
        int i = segmentIndex * 3;
        Vector3 p = GetPointOnBezierCurve(points[i], points[i + 1], points[i + 2], points[i + 3], t);
        return p;
    }

    public Vector3 GetVelocityAt(int segmentIndex, float t)
    {
        if (points.Count < 4) return Vector3.right;
        int i = segmentIndex * 3;
        Vector3 v = GetFirstDerivativeOnBezierCurve(points[i], points[i+1], points[i+2], points[i+3], t);
        return v;
    }

    public Vector3 GetDirectionAt(int segmentIndex, float t) => GetVelocityAt(segmentIndex, t).normalized;

    public void MovePoint(int pointIndex, Vector3 pos, bool updateHandles)
    {
        Vector3 deltaMove = pos - points[pointIndex];
        points[pointIndex] = pos;

        if (!updateHandles) return;

        int localIndex = (pointIndex % 3);
        if (localIndex == 0) // Moving an anchor point
        {
            UpdateHandles(pointIndex);
        }
        else if (pointIndex > 1 && pointIndex < PointCount - 2)
        {
            // Mirror the movement on the other handle

            // First, get the index of the other handle
            if (localIndex == 1)
            {
                pointIndex -= 2;
            }
            else if (localIndex == 2)
            {
                pointIndex += 2;
            }

            // Then move it in the opposite (mirrored) direction
            // CHALLENGE: FIXME! Notice how the mirrored movement
            // doesn't keep the handles in line with their anchor.
            // Why? Can you fix it?
            points[pointIndex] -= deltaMove;
        }
    }

    private void UpdateHandles(int anchorIndex)
    {
        Vector3 anchorPos = points[anchorIndex];
        Vector3 tangentDirection = Vector3.zero;

        int prevControlIndex = anchorIndex - 2;
        float prevNeighborDistance = 0f;
        if (prevControlIndex >= 0 || IsClosed)
        {
            Vector3 offset = points[PointIndex(prevControlIndex)] - anchorPos;
            tangentDirection += offset.normalized;
            prevNeighborDistance = offset.magnitude;
        }

        int nextControlIndex = anchorIndex + 2;
        float nextNeighborDistance = 0f;
        if (nextControlIndex < PointCount || IsClosed)
        {
            Vector3 offset = points[PointIndex(nextControlIndex)] - anchorPos;
            tangentDirection -= offset.normalized;
            nextNeighborDistance = -offset.magnitude;
        }

        tangentDirection.Normalize();

        prevControlIndex = anchorIndex - 1;
        if (prevControlIndex >= 0 || IsClosed)
        {
            points[PointIndex(prevControlIndex)] = anchorPos + tangentDirection * (prevNeighborDistance * 0.5f);
        }

        nextControlIndex = anchorIndex + 1;
        if (nextControlIndex < points.Count || IsClosed)
        {
            points[PointIndex(nextControlIndex)] = anchorPos + tangentDirection * (nextNeighborDistance * 0.5f);
        }
    }

    private static Vector3 GetPointOnQuadraticCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t); // don't go beyond the ends of the curve

        // The simple (and slow) way to do it would be to return
        //      Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t)
        // But since Lerp is implemented as this:
        //      Lerp(p0, p1, t) = (1 - t) * p0 + t * p1
        // We can optimize the simple method by expanding it to this:
        //      (1 - t)((1 - t) * p0 + t * p1) + t((1 - t) * p1 + t * p2)
        // and then simpify to this:
        //      (1 - t)^2 * p0 + 2 * (1 - t) * t * p1 + t^2 * p2
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * p0  +  2f * oneMinusT * t * p1  +  t * t * p2;
    }

    private static Vector3 GetFirstDerivativeOnQuadraticCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return 2f * oneMinusT * (p1 - p0) + 2f * t * (p2 - p1);
    }
    
    private static Vector3 GetPointOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0 +
               3f * oneMinusT * oneMinusT * t * p1 +
               3f * oneMinusT * t * t * p2 +
               t * t * t * p3;
    }

    private static Vector3 GetFirstDerivativeOnBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return 3f * oneMinusT * oneMinusT * (p1 - p0) +
               6f * oneMinusT * t * (p2 - p1) +
               3f * t * t * (p3 - p2);
    }
}
