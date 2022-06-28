using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Spline))]
public class SplineEditor : Editor
{
	private Spline spline;
	private Transform handleTransform;
	private Quaternion handleRotation;
	private bool showInterpolations;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
		showInterpolations = EditorGUILayout.Toggle("Show Interpolations", showInterpolations);
    }

    // Called when a user makes a change in Unity's "Scene View"
    private void OnSceneGUI()
	{
		spline = target as Spline;
		if (spline == null || !spline.isActiveAndEnabled) return;

		handleTransform = spline.transform;
		handleRotation = Tools.pivotRotation == PivotRotation.Local ?
			handleTransform.rotation : Quaternion.identity;

		Vector3 p0 = UpdateControlPoint(0);
		Vector3 p1 = UpdateControlPoint(1);
		Vector3 p2 = UpdateControlPoint(2);
		Vector3 p3 = UpdateControlPoint(3);

		Handles.color = Color.yellow;
		Vector3 lineStart = spline.GetPointAt(0f);
		const int NUM_LINE_STEPS = 20;
		for (int i = 1; i <= NUM_LINE_STEPS; i++)
		{
			float t = (float)i / NUM_LINE_STEPS;
			Vector3 lineEnd = spline.GetPointAt(t);
			Handles.DrawLine(lineStart, lineEnd, 2);
			lineStart = lineEnd;
		}

		#region Visualizations
		if (showInterpolations)
		{
			Handles.color = Color.gray;
			Handles.DrawLine(p0, p1, 1);
			Handles.DrawLine(p1, p2, 1);
			Handles.DrawLine(p2, p3, 1);
			Vector3 pT = spline.GetPointAt(spline.T);
			Vector3 pDir = spline.GetDirectionAt(spline.T);
			Handles.color = Color.grey;
			Vector3 p01 = Vector3.Lerp(p0, p1, spline.T);
			Vector3 p12 = Vector3.Lerp(p1, p2, spline.T);
			Vector3 p23 = Vector3.Lerp(p2, p3, spline.T);
			Vector3 p012 = Vector3.Lerp(p01, p12, spline.T);
			Vector3 p123 = Vector3.Lerp(p12, p23, spline.T);
			Handles.DrawWireDisc(p01, (p1 - p0).normalized, 0.15f, 2f);
			Handles.DrawWireDisc(p12, (p2 - p1).normalized, 0.15f, 2f);
			Handles.DrawWireDisc(p23, (p3 - p2).normalized, 0.15f, 2f);
			Handles.DrawDottedLine(p01, p12, 2f);
			Handles.DrawDottedLine(p12, p23, 2f);
			Handles.color = Color.white;
			Handles.DrawWireDisc(p012, (p123 - p012).normalized, 0.1f, 2f);
			Handles.DrawWireDisc(p123, (p123 - p012).normalized, 0.1f, 2f);
			Handles.DrawDottedLine(p012, p123, 1f);
			Handles.DrawWireDisc(pT, pDir, 0.05f, 2f);
		}
        #endregion
    }

    private Vector3 UpdateControlPoint(int index)
	{
		if (index < 0 || index >= spline.Points.Count) return Vector3.zero;
		Vector3 point = handleTransform.TransformPoint(spline.Points[index]);
		EditorGUI.BeginChangeCheck();
		point = Handles.FreeMoveHandle(point, handleRotation, 0.1f, Vector3.one*0.1f, Handles.CircleHandleCap);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(spline, "Move spline point");
			EditorUtility.SetDirty(spline);
			Vector3 localPoint = handleTransform.InverseTransformPoint(point);
			spline.SetPoint(index, localPoint);
		}
		return point;
	}
}
