using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SplineMaker))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class SplineMesh : MonoBehaviour
{
    [Min(0.00001f)]
    public float width = 1f;

    private SplineMaker splineMaker;
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> uvCoords = new List<Vector2>();
    private readonly List<Vector3> normals = new List<Vector3>();
    private readonly List<int> triangles = new List<int>(); // list of indices into other lists (vertices, uvCoords, normals), each three indices form a new triangle
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Awake()
    {
        splineMaker = GetComponent<SplineMaker>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = new Mesh();
            meshFilter.hideFlags = HideFlags.HideInInspector;
        }
        splineMaker.OnChanged += SplineMaker_OnChanged;
    }

    private void OnDestroy()
    {
        splineMaker.OnChanged -= SplineMaker_OnChanged;
    }

    private void SplineMaker_OnChanged()
    {
        Rebuild();
    }

    private void Start()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        mesh.Clear();

        vertices.Clear();
        uvCoords.Clear();
        normals.Clear();
        triangles.Clear();

        if (!splineMaker || splineMaker.spline == null) return;

        for (int segmentIndex = 0; segmentIndex < splineMaker.spline.SegmentCount; segmentIndex++)
        {
            BuildSegment(segmentIndex);
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uvCoords.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();

        meshCollider.sharedMesh = mesh;
    }

    private void AddVertex(Vector3 position, Vector2 uv, Vector3 normal)
    {
        vertices.Add(position);
        uvCoords.Add(uv);
        normals.Add(normal);
    }

    private void AddQuadrilateral(int vertexIndex_0, int vertexIndex_1, int vertexIndex_2, int vertexIndex_3)
    {
        //  v2 -- v3
        //  | \   |
        //  |  \  |
        //  |   \ |
        //  |    \|
        //  v0 -- v1

        triangles.Add(vertexIndex_0);
        triangles.Add(vertexIndex_1);
        triangles.Add(vertexIndex_2);

        triangles.Add(vertexIndex_3);
        triangles.Add(vertexIndex_2);
        triangles.Add(vertexIndex_1);
    }

    private void AddCrossSectionVertices(int segmentIndex, float t)
    {
        Vector3 pos = splineMaker.spline.GetPointAt(segmentIndex, t);
        Vector3 forward = splineMaker.spline.GetDirectionAt(segmentIndex, t);
        Vector3 up = transform.up;
        Vector3 right = Vector3.Cross(forward, up);
        Vector3 offset = right * (width / 2f);
        Vector3 posL = pos - offset;
        Vector3 posR = pos + offset;
        Vector2 uvL = new Vector2(t, 0f);
        Vector2 uvR = new Vector2(t, 1f);
        AddVertex(posL, uvL, up);
        AddVertex(posR, uvR, up);
    }

    private void BuildSegment(int segmentIndex)
    {
        // Add sub-segments along the curve
        for (int i = 0; i <= SplineMaker.NUM_LINE_STEPS; i++) // note the <=
        {
            // Add the vertices for this sub-segment
            float t = (float)i / SplineMaker.NUM_LINE_STEPS;
            AddCrossSectionVertices(segmentIndex, t);

            // Add the quadrilateral for this sub-segment
            if (i > 0)
            {
                int vi3 = vertices.Count - 1;
                int vi2 = vi3 - 1;
                int vi1 = vi2 - 1;
                int vi0 = vi1 - 1;
                AddQuadrilateral(vi0, vi1, vi2, vi3);
            }
        }
    }    
}
