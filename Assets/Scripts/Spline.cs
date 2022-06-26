using System;
using System.Collections.Generic;
using UnityEngine;

public class Spline : MonoBehaviour
{
    private readonly List<Vector3> points = new List<Vector3>();
    public IReadOnlyList<Vector3> Points => points;

    private void Reset()
    {
        points.Add(new Vector3(0f, 0f, 0f));
        points.Add(new Vector3(1f, 0f, 0f));
        points.Add(new Vector3(2f, 0f, 0f));
        points.Add(new Vector3(3f, 0f, 0f));
    }

    public void SetPoint(int index, Vector3 localPoint)
    {
        if (index < 0 || index >= points.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        points[index] = localPoint;
    }

    public Vector3 GetPointAt(float t)
    {
        if (points.Count < 4) return Vector3.zero;
        Vector3 p = GetPointOnBezierCurve(points[0], points[1], points[2], points[3], t);
        return transform.TransformPoint(p);
    }

    public Vector3 GetVelocityAt(float t)
    {
        if (points.Count < 4) return Vector3.right;
        Vector3 dir = GetFirstDerivativeOnBezierCurve(points[0], points[1], points[2], points[3], t);
        return transform.TransformVector(dir);
    }

    public Vector3 GetDirectionAt(float t) => GetVelocityAt(t).normalized;

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

    #region IGNORE ME
    
    [Range(0f, 1f)]
    public float T;

    private void OnGUI()
    {
        T = GUI.HorizontalSlider(new Rect(25f, 25f, 100f, 30f), T, 0f, 1f);
    }

    #endregion
}
