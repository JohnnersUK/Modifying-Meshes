using System.Collections.Generic;
using UnityEngine;

public class VoronoiFracture : MonoBehaviour
{
    public Transform center;
    public int numPoints;

    private int count;
    private Mesh _mesh;
    public List<Point> points;

    // Start is called before the first frame update
    void Start()
    {
        points = new List<Point>();
        _mesh = GetComponent<MeshFilter>().mesh;
    }

    // Update is called once per frame
    void Update()
    {
        while (count < numPoints)
        {
            Vector3 point = GetPI();
            if (GetComponent<MeshCollider>().bounds.Contains(transform.TransformPoint(point)))
            {
                points.Add(new Point(point));
            }

            count++;
        }
    }

    // Gets a random point inside an object
    Vector3 GetPI()
    {
        return Vector3.Lerp(GetPOS(), GetPOS(), Random.Range(0.0f, 1.0f));
    }

    // Gets a random point on the surface of an object
    Vector3 GetPOS()
    {
        // Pick a random triangle
        int triangleOrigin = Mathf.FloorToInt(Random.Range(0f, _mesh.triangles.Length) / 3f) * 3;

        // Get the triangles vertices
        Vector3[] vertices = new Vector3[3]
        {
            _mesh.vertices[_mesh.triangles[triangleOrigin]],
            _mesh.vertices[_mesh.triangles[triangleOrigin + 1]],
            _mesh.vertices[_mesh.triangles[triangleOrigin + 2]]
        };

        // Get the center point of each line
        Vector3 diffAB = vertices[1] - vertices[0];
        Vector3 diffBC = vertices[2] - vertices[1];

        // Calculate a random point by adding 2 distances to the first vertex
        return vertices[0] + (diffAB * Random.Range(0f, 1f)) + (diffBC * Random.Range(0f, 1f));
    }

    private void OnDrawGizmos()
    {
        if (points.Count == 0 || points == null)
        {
            return;
        }
        else
        {
            foreach (Point p in points)
            {
                Gizmos.color = Color.red; // The_Helper.InterpolateColor(Color.red, Color.green, p.pos.magnitude); 

                Gizmos.DrawSphere(transform.TransformPoint(p.pos), transform.lossyScale.magnitude / 100);
            }
        }
    }

    public struct Point
    {
        public Point(Vector3 pos)
        {
            this.pos = pos;
        }
        public Vector3 pos;
    }
}
