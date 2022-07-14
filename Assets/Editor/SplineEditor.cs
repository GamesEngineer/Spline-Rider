using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineMaker))]
public class SplineEditor : Editor
{
	private SplineMaker splineMaker;
	private Transform handleTransform;
	private Quaternion handleRotation;
	private int highlightedSegmentIndex = -1;
	private int selectedSegmentIndex = -1;

    #region IGNORE
    private bool showInterpolations;
	private float tParam;
    #endregion

    // Called when a user makes a change in Unity's "Inspector" window
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
		
		EditorGUI.BeginChangeCheck();

		showInterpolations = EditorGUILayout.Toggle("Show Interpolations", showInterpolations);
		if (showInterpolations)
		{
			tParam = EditorGUILayout.Slider(tParam, 0f, 1f);
		}

		if (EditorGUI.EndChangeCheck())
        {
			SceneView.RepaintAll();
        }
    }

	// Called when a user makes a change in Unity's "Scene View"
	private void OnSceneGUI()
	{
		splineMaker = target as SplineMaker;
		if (splineMaker == null || !splineMaker.isActiveAndEnabled || splineMaker.spline == null) return;

		handleTransform = splineMaker.transform;
		handleRotation = Tools.pivotRotation == PivotRotation.Local ?
			handleTransform.rotation : Quaternion.identity;

		Event guiEvent = Event.current;
		Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
		float distance = Vector3.Distance(ray.origin, handleTransform.position);
		Vector3 nearestPoint = ray.GetPoint(distance);

        if (guiEvent.type == EventType.MouseMove)
		{
			UpdateSelectedSegment(nearestPoint); // START HERE NEXT TIME 07/19/2022
		}

        #region Operations
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
		{
			if (selectedSegmentIndex >= 0 && guiEvent.control)
			{
				Undo.RecordObject(splineMaker, "Split Segment");
				splineMaker.SplitSegment(nearestPoint, selectedSegmentIndex);
				Event.current.Use();
			}
			else if (!splineMaker.spline.IsClosed && guiEvent.shift)
			{
				Undo.RecordObject(splineMaker, "Add Segment");
				splineMaker.AddSegment(nearestPoint);
				Event.current.Use();
			}
		}

		if (guiEvent.type == EventType.MouseDown && guiEvent.button == 1)
		{
			if (selectedSegmentIndex >= 0 && guiEvent.control)
			{
				Undo.RecordObject(splineMaker, "Delete segment");
				splineMaker.spline.DeleteSegment(selectedSegmentIndex);
				Event.current.Use();
			}
		}
		#endregion

		#region Control Points & Handles
		if (selectedSegmentIndex >= 0)
		{
			Vector3 p0 = UpdateControlPoint(selectedSegmentIndex, 0);
			Vector3 p1 = UpdateControlPoint(selectedSegmentIndex, 1);
			Vector3 p2 = UpdateControlPoint(selectedSegmentIndex, 2);
			Vector3 p3 = UpdateControlPoint(selectedSegmentIndex, 3);
			Handles.DrawBezier(p0, p3, p1, p2, Color.yellow, null, 8f);

			// Draw the curve handles
			Handles.color = showInterpolations ? Color.gray : Color.white;
			Handles.DrawLine(p0, p1, 1);
			Handles.DrawLine(p2, p3, 1);
			if (!showInterpolations)
			{
				Vector3 prevP2 = UpdateControlPoint(selectedSegmentIndex - 1, 2);
				Vector3 nextP1 = UpdateControlPoint(selectedSegmentIndex + 1, 1);
				Handles.color = Color.gray;
				if (selectedSegmentIndex > 0) Handles.DrawLine(prevP2, p0, 1);
				if (selectedSegmentIndex < splineMaker.spline.SegmentCount - 1) Handles.DrawLine(p3, nextP1, 1);
			}
		}
        #endregion

        #region Visualizations
        if (showInterpolations && selectedSegmentIndex > 0)
		{
			Vector3 p0 = GetControlPoint(selectedSegmentIndex, 0);
			Vector3 p1 = GetControlPoint(selectedSegmentIndex, 1);
			Vector3 p2 = GetControlPoint(selectedSegmentIndex, 2);
			Vector3 p3 = GetControlPoint(selectedSegmentIndex, 3);
			Handles.color = Color.gray;
			Handles.DrawLine(p1, p2, 1);
			Vector3 pT = splineMaker.GetPointAt(selectedSegmentIndex, tParam);
			Vector3 pDir = splineMaker.GetDirectionAt(selectedSegmentIndex, tParam);
			Handles.color = Color.grey;
			Vector3 p01 = Vector3.Lerp(p0, p1, tParam);
			Vector3 p12 = Vector3.Lerp(p1, p2, tParam);
			Vector3 p23 = Vector3.Lerp(p2, p3, tParam);
			Vector3 p012 = Vector3.Lerp(p01, p12, tParam);
			Vector3 p123 = Vector3.Lerp(p12, p23, tParam);
			Handles.DrawWireDisc(p01, (p1 - p0).normalized, 0.15f, 2f);
			Handles.DrawWireDisc(p12, (p2 - p1).normalized, 0.15f, 2f);
			Handles.DrawWireDisc(p23, (p3 - p2).normalized, 0.15f, 2f);
			Handles.DrawDottedLine(p01, p12, 2f);
			Handles.DrawDottedLine(p12, p23, 2f);
			Handles.color = Color.white;
			Handles.DrawWireDisc(p012, (p123 - p012).normalized, 0.1f, 2f);
			Handles.DrawWireDisc(p123, (p123 - p012).normalized, 0.1f, 2f);
			Handles.DrawDottedLine(p012, p123, 1f);
			Handles.color = Color.yellow;
			Handles.DrawWireDisc(pT, pDir, 0.05f, 2f);
		}
        #endregion
    }

	private Vector3 GetControlPoint(int segmentIndex, int pointIndex)
    {
		return splineMaker.spline[segmentIndex * 3 + pointIndex];
    }

	private Vector3 UpdateControlPoint(int segmentIndex, int pointIndex)
	{
		pointIndex += segmentIndex * 3;
		if (pointIndex < 0 || pointIndex >= splineMaker.spline.PointCount) return Vector3.zero;
        #region IGNORE
        if (showInterpolations)
        {
			return splineMaker[pointIndex];
        }
		#endregion
		Vector3 point = handleTransform.TransformPoint(splineMaker[pointIndex]);
		EditorGUI.BeginChangeCheck();
		float handleSize = SplineMaker.ANCHOR_SIZE + 0.1f; // HandleUtility.GetHandleSize(point) * 0.1f;
		if ((pointIndex % 3) != 0) handleSize *= 0.5f; // make the handles smaller than the anchor points
		Handles.color = Color.white;
		point = Handles.FreeMoveHandle(point, handleRotation, handleSize, snap: Vector3.one * 0.1f, Handles.CircleHandleCap);
		if (EditorGUI.EndChangeCheck())
		{
			Undo.RecordObject(splineMaker, "Move spline point");
			EditorUtility.SetDirty(splineMaker);
			splineMaker.MovePoint(pointIndex, point, updateHandles: !Event.current.control);
		}
		return point;
	}

	private void UpdateSelectedSegment(Vector3 selectionPointWS)
    {
		Vector3 selectionPoint = handleTransform.InverseTransformPoint(selectionPointWS);
		float minDistance = 10f;
		highlightedSegmentIndex = -1;
		for (int segmentIndex = 0; segmentIndex < splineMaker.spline.SegmentCount; segmentIndex++)
        {
			int startPointIndex = segmentIndex * 3;
			int startHandleIndex = startPointIndex + 1;
			int endHandleIndex = startPointIndex + 2;
			int endPointIndex = startPointIndex + 3;
			Vector3 startPosition = splineMaker.spline[startPointIndex];
			Vector3 endPosition = splineMaker.spline[endPointIndex];
			Vector3 startHandle = splineMaker.spline[startHandleIndex];
			Vector3 endHandle = splineMaker.spline[endHandleIndex];
			Vector3 startTangent = startHandle;
			Vector3 endTangent = endHandle;
			float distance = HandleUtility.DistancePointBezier(selectionPoint, startPosition, endPosition, startTangent, endTangent);
			if (distance < minDistance)
            {
				minDistance = distance;
				highlightedSegmentIndex = segmentIndex;
            }
        }

		if (highlightedSegmentIndex >= 0)
		{
			selectedSegmentIndex = highlightedSegmentIndex;
		}
    }
}
