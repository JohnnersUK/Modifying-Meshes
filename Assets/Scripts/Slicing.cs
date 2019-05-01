using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Slicing : MonoBehaviour
{
    // Maximum amount of cuts when shattering glass
    public int MaxCuts;

    private GameObject CurrentTarget;
    public List<GameObject> TargetList;
    private List<GameObject> LockedTargetList;

    // Components of the cutting plane
    public GameObject CuttingPlane;
    private Vector3 CutDirection;
    private Vector3 CutPosition;

    // Top components 
    private GameObject TopPart;

    private List<Vector3> TopVertices;
    private List<Vector3> TopNormals;
    private List<Vector2> TopUVs;
    private List<int> TopTriangles;

    // Bottom components
    private GameObject BottomPart;

    private List<Vector3> BottomVertices;
    private List<Vector3> BottomNormals;
    private List<Vector2> BottomUVs;
    private List<int> BottomTriangles;

    // Newly generated vertices around cut
    private List<Vector3> CenterVertices;

    // Temporary Components
    private Vector3[] TempVertices;
    private Vector2[] TempUVs;
    private Vector3[] TempNormals;

    private void Start()
    {
        // Initilize components
        InitializeComponents();
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, transform.forward, Color.green);

        // Complete a single cut
        if (Input.GetMouseButtonUp(2))
        {
            bool empty = (TargetList.Count == 0);

            // Whilst the list of targets isn't empty
            while (!empty)
            {
                // Slice the top object
                CurrentTarget = TargetList[TargetList.Count - 1];
                SliceMesh(CurrentTarget.transform.position);

                empty = (TargetList.Count == 0);
            }

        }

        // Complete a series of cuts at random rotations
        if (Input.GetMouseButton(0))
        {
            // Raycast from screen to mouse point
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                if (hit.transform.gameObject != CuttingPlane)
                {
                    // Clear the target list and add the hit object
                    TargetList.Clear();
                    TargetList.Add(hit.transform.gameObject);

                    int cuts = Random.Range(MaxCuts / 2, MaxCuts);
                    bool empty = (TargetList.Count == 0);

                    // Store the original rotation then reset it
                    Quaternion origin = transform.rotation;
                    transform.rotation = new Quaternion(0, 0, 0, 0);

                    // Whilst the list of targets isn't empty
                    while (!empty)
                    {
                        // Random a rotation
                        transform.Rotate(0.0f, 0.0f, Random.Range(0.0f, 360.0f));

                        // Slice the bottom object
                        CurrentTarget = TargetList[0];
                        SliceMesh(hit.point);

                        if (cuts < 0)
                        {
                            TargetList.Add(TopPart);
                            TargetList.Add(BottomPart);
                            cuts--;
                        }

                        empty = (TargetList.Count == 0);
                    }

                    transform.rotation = origin;
                }
            }

        }
    }

    // Re-initializes the core components
    private void InitializeComponents()
    {
        // Initilize components
        TempVertices = new Vector3[3];
        TempUVs = new Vector2[3];
        TempNormals = new Vector3[3];

        TopVertices = new List<Vector3>();
        TopTriangles = new List<int>();
        TopUVs = new List<Vector2>();
        TopNormals = new List<Vector3>();

        BottomVertices = new List<Vector3>();
        BottomTriangles = new List<int>();
        BottomUVs = new List<Vector2>();
        BottomNormals = new List<Vector3>();

        CenterVertices = new List<Vector3>();
    }

    // Slices the target mesh
    private void SliceMesh(Vector3 hitPos)
    {
        // Origin of the cuttin plane
        Vector3 origin;

        // target components
        Mesh targetMesh;
        int[] triangles;
        Vector2[] uvs;
        Vector3[] vertices;
        Vector3[] normals;

        // Cap components
        Vector3 center;
        float normalDir;

        // Re-Initialize core components
        InitializeComponents();

        // Set the position of the cut
        origin = CuttingPlane.transform.position;
        CuttingPlane.transform.position = hitPos;

        CutDirection = (-CuttingPlane.transform.forward).normalized;
        CutPosition = CuttingPlane.transform.position;

        // Initilize the target components
        targetMesh = CurrentTarget.GetComponent<MeshFilter>().mesh;

        triangles = targetMesh.triangles;
        uvs = targetMesh.uv;
        vertices = targetMesh.vertices;
        normals = targetMesh.normals;

        TopPart = new GameObject("TopPart");
        BottomPart = new GameObject("BottomPart");

        // Loop through each triangle, checking for intersections
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Fill the temporary lists with the current triangle data
            TempVertices[0] = CurrentTarget.transform.TransformPoint(vertices[triangles[i]]);
            TempVertices[1] = CurrentTarget.transform.TransformPoint(vertices[triangles[i + 1]]);
            TempVertices[2] = CurrentTarget.transform.TransformPoint(vertices[triangles[i + 2]]);

            TempUVs[0] = uvs[triangles[i]];
            TempUVs[1] = uvs[triangles[i + 1]];
            TempUVs[2] = uvs[triangles[i + 2]];

            TempNormals[0] = CurrentTarget.transform.TransformVector(normals[triangles[i]]);
            TempNormals[1] = CurrentTarget.transform.TransformVector(normals[triangles[i + 1]]);
            TempNormals[2] = CurrentTarget.transform.TransformVector(normals[triangles[i + 2]]);

            // Check for intersections
            CheckIntersection();
        }

        // Calculate the center of the cut
        center = Vector3.zero;
        for (int i = 0; i < CenterVertices.Count; i++)
        {
            center += CenterVertices[i];
        }

        center /= CenterVertices.Count;

        // Order the vertices clockwise relative to the center
        if (CutDirection.y != 0)
        {
            normalDir = Mathf.Sign(CutDirection.y);
            CenterVertices = CenterVertices.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x)).ToList();
        }
        else
        {
            normalDir = Mathf.Sign(CutDirection.z);
            CenterVertices = CenterVertices.OrderBy(x => normalDir * Mathf.Atan2((x - center).x, (x - center).y)).ToList();
        }

        // Fill the gaps left after slicing
        CapGap(TopVertices, TopTriangles, TopUVs, TopNormals, CenterVertices, center, true);
        CapGap(BottomVertices, BottomTriangles, BottomUVs, BottomNormals, CenterVertices, center, false);

        // Create the two new GameObjects
        CreatePart(TopPart, TopVertices, TopTriangles, TopUVs, TopNormals);
        CreatePart(BottomPart, BottomVertices, BottomTriangles, BottomUVs, BottomNormals);

        // Remove the original game object
        TargetList.Remove(CurrentTarget);
        Destroy(CurrentTarget);
    }

    // Checks for and resolves any intersections
    private void CheckIntersection()
    {
        // Get the sign of the dot product between the vertex and the cutting plane
        // Positive is above the plane, negative is bellow
        float v1Side = Mathf.Sign(Vector3.Dot(CutDirection, TempVertices[0] - CutPosition));
        float v2Side = Mathf.Sign(Vector3.Dot(CutDirection, TempVertices[1] - CutPosition));
        float v3Side = Mathf.Sign(Vector3.Dot(CutDirection, TempVertices[2] - CutPosition));

        // If any of the vertices are on opposing sides
        // there has been an intersection across the triangle
        bool intersect1 = v1Side != v2Side;
        bool intersect2 = v2Side != v3Side;
        bool intersect3 = v1Side != v3Side;

        // If there is an intersection, split the triangle
        if (intersect1 || intersect2 || intersect3)
        {
            List<Vector3> tempTopVertices = new List<Vector3>();
            List<Vector3> tempBottomVertices = new List<Vector3>();

            if (intersect1)
            {
                SplitTriangles(v1Side, 0, 1, TempVertices, TempUVs, TempNormals, tempTopVertices, tempBottomVertices);
            }
            if (intersect2)
            {
                SplitTriangles(v2Side, 1, 2, TempVertices, TempUVs, TempNormals, tempTopVertices, tempBottomVertices);
            }
            if (intersect3)
            {
                SplitTriangles(v3Side, 2, 0, TempVertices, TempUVs, TempNormals, tempTopVertices, tempBottomVertices);
            }
            CreateNewTriangles(tempTopVertices, tempBottomVertices);
        }
        // If there isn't check which half it belongs to and assign it
        else
        {
            // Add the triangle to the top half
            if (Vector3.Dot(CutDirection, (TempVertices[0] - CutPosition)) >= 0)
            {
                TopVertices.Add(TopPart.transform.InverseTransformPoint(TempVertices[0]));
                TopVertices.Add(TopPart.transform.InverseTransformPoint(TempVertices[1]));
                TopVertices.Add(TopPart.transform.InverseTransformPoint(TempVertices[2]));

                TopTriangles.Add(TopVertices.Count - 3);
                TopTriangles.Add(TopVertices.Count - 2);
                TopTriangles.Add(TopVertices.Count - 1);

                TopUVs.Add(TempUVs[0]);
                TopUVs.Add(TempUVs[1]);
                TopUVs.Add(TempUVs[2]);

                TopNormals.Add(TopPart.transform.InverseTransformVector(TempNormals[0]));
                TopNormals.Add(TopPart.transform.InverseTransformVector(TempNormals[1]));
                TopNormals.Add(TopPart.transform.InverseTransformVector(TempNormals[2]));
            }
            // Add triangle to the bottom half
            else
            {
                BottomVertices.Add(BottomPart.transform.InverseTransformPoint(TempVertices[0]));
                BottomVertices.Add(BottomPart.transform.InverseTransformPoint(TempVertices[1]));
                BottomVertices.Add(BottomPart.transform.InverseTransformPoint(TempVertices[2]));

                BottomTriangles.Add(BottomVertices.Count - 3);
                BottomTriangles.Add(BottomVertices.Count - 2);
                BottomTriangles.Add(BottomVertices.Count - 1);

                BottomUVs.Add(TempUVs[0]);
                BottomUVs.Add(TempUVs[1]);
                BottomUVs.Add(TempUVs[2]);

                BottomNormals.Add(BottomPart.transform.InverseTransformVector(TempNormals[0]));
                BottomNormals.Add(BottomPart.transform.InverseTransformVector(TempNormals[1]));
                BottomNormals.Add(BottomPart.transform.InverseTransformVector(TempNormals[2]));
            }
        }
    }

    // Splits triangles and adds a new vertex at point of intersection
    private void SplitTriangles(float vertexSide, int v1Index, int v2Index, Vector3[] vertices, Vector2[] uvs, Vector3[] normals, List<Vector3> newTopVertices, List<Vector3> newBottomVertices)
    {
        // Temporary storage
        Vector3 v1 = vertices[v1Index];
        Vector3 v2 = vertices[v2Index];
        Vector2 uv1 = uvs[v1Index];
        Vector2 uv2 = uvs[v2Index];
        Vector3 n1 = normals[v1Index];
        Vector3 n2 = normals[v2Index];

        // Calculate the intersection point using the
        // perp dot product
        Vector3 rayDir = (v2 - v1).normalized;
        float t = Vector3.Dot(CutPosition - v1, CutDirection) / Vector3.Dot(rayDir, CutDirection);
        Vector3 newVert = v1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        GetNewUVs(newVert, ref newUv, ref newNormal, vertices, uvs, normals);

        // Create the new vertex
        Vector3 topNewVert = TopPart.transform.InverseTransformPoint(newVert);
        Vector3 botNewVert = BottomPart.transform.InverseTransformPoint(newVert);

        // Create the new normal
        Vector3 topNewNormal = TopPart.transform.InverseTransformVector(newNormal).normalized;
        Vector3 botNewNormal = BottomPart.transform.InverseTransformVector(newNormal).normalized;

        // If the first vertex is above the cut
        if (vertexSide > 0)
        {
            // Get the local positions
            v1 = TopPart.transform.InverseTransformPoint(v1);
            v2 = BottomPart.transform.InverseTransformPoint(v2);

            // Get the local directions
            n1 = TopPart.transform.InverseTransformVector(n1).normalized;
            n2 = BottomPart.transform.InverseTransformVector(n2).normalized;

            // Add the first vertex to the top half
            if (!newTopVertices.Contains(v1))
            {
                newTopVertices.Add(v1);
                TopUVs.Add(uv1);
                TopNormals.Add(n1);
            }

            // Add the new components to the top half
            newTopVertices.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            // Add the new components to the bottom half
            newBottomVertices.Add(botNewVert);
            BottomUVs.Add(newUv);
            BottomNormals.Add(botNewNormal);

            // Add the second vertex to the other half
            if (!newBottomVertices.Contains(v2))
            {
                newBottomVertices.Add(v2);
                BottomUVs.Add(uv2);
                BottomNormals.Add(n2);
            }

            // Add the new vertex to the list of center vertices
            CenterVertices.Add(topNewVert);
        }
        else
        {
            // Get the local positions
            v2 = TopPart.transform.InverseTransformPoint(v2);
            v1 = BottomPart.transform.InverseTransformPoint(v1);

            // Get the local directions
            n2 = TopPart.transform.InverseTransformVector(n2).normalized;
            n1 = BottomPart.transform.InverseTransformVector(n1).normalized;

            // Add the new components
            newTopVertices.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            // Add the second vertex to the top half
            if (!newTopVertices.Contains(v2))
            {
                newTopVertices.Add(v2);
                TopUVs.Add(uv2);
                TopNormals.Add(n2);
            }

            // Add the first vertex to the bottom half
            if (!newBottomVertices.Contains(v1))
            {
                newBottomVertices.Add(v1);
                BottomUVs.Add(uv1);
                BottomNormals.Add(n1);
            }

            // Add the new components
            newBottomVertices.Add(botNewVert);
            BottomUVs.Add(newUv);
            BottomNormals.Add(botNewNormal);

            // Add the new vertex to the list of center vertices
            CenterVertices.Add(botNewVert);
        }

    }

    // Calculates new UVs
    private void GetNewUVs(Vector3 newVertex, ref Vector2 newUV, ref Vector3 newNormal, Vector3[] vertices, Vector2[] uvs, Vector3[] normals)
    {
        Vector3 f1 = vertices[0] - newVertex;
        Vector3 f2 = vertices[1] - newVertex;
        Vector3 f3 = vertices[2] - newVertex;

        // calculate the triangle areas
        float areaMainTri = Vector3.Cross(vertices[0] - vertices[1], vertices[0] - vertices[2]).magnitude; // main triangle area a
        float a1 = Vector3.Cross(f2, f3).magnitude / areaMainTri; // p1's triangle area / a
        float a2 = Vector3.Cross(f3, f1).magnitude / areaMainTri; // p2's triangle area / a 
        float a3 = Vector3.Cross(f1, f2).magnitude / areaMainTri; // p3's triangle area / a

        // Multiply all the directions together to find the normal
        newNormal = normals[0] * a1 + normals[1] * a2 + normals[2] * a3;

        // find the uv at f
        newUV = uvs[0] * a1 + uvs[1] * a2 + uvs[2] * a3;
    }

    // Creates new triangles from split parts
    private void CreateNewTriangles(List<Vector3> tempTopVertices, List<Vector3> tempBottomVertices)
    {
        // Get the last inserted vertices for each part
        int topLastInsert = TopVertices.Count;
        int bottomLastInsert = BottomVertices.Count;

        // Add the new vertices
        BottomVertices.AddRange(tempBottomVertices);
        TopVertices.AddRange(tempTopVertices);

        // Create a new triangle from the new vertices
        TopTriangles.Add(topLastInsert);
        TopTriangles.Add(topLastInsert + 1);
        TopTriangles.Add(topLastInsert + 2);

        // Recreate the old triangles
        if (tempTopVertices.Count > 3)
        {
            TopTriangles.Add(topLastInsert);
            TopTriangles.Add(topLastInsert + 2);
            TopTriangles.Add(topLastInsert + 3);
        }

        // Create a new triangle from the new vertices
        BottomTriangles.Add(bottomLastInsert);
        BottomTriangles.Add(bottomLastInsert + 1);
        BottomTriangles.Add(bottomLastInsert + 2);

        // Recreate the old triangles
        if (tempBottomVertices.Count > 3)
        {
            BottomTriangles.Add(bottomLastInsert);
            BottomTriangles.Add(bottomLastInsert + 2);
            BottomTriangles.Add(bottomLastInsert + 3);

        }
    }

    // Fills gap left by slice
    private void CapGap(List<Vector3> partVertices, List<int> partTriangles, List<Vector2> partUVs, List<Vector3> partNormals, List<Vector3> orderedSliceVerts, Vector3 center, bool top)
    {
        List<int> centerTriangles = new List<int>();
        Vector3 normal;

        // Get the size of the list before the center node is added
        int sizeVertsBeforeCenter = partVertices.Count;

        // Add the center vertices
        partVertices.AddRange(orderedSliceVerts);
        partVertices.Add(center);

        // If we're capping the top part
        if (top)
        {
            // Build triangles from the top of the list
            for (int i = sizeVertsBeforeCenter; i < partVertices.Count - 1; i++)
            {
                centerTriangles.Add(i);
                centerTriangles.Add(i + 1);
                centerTriangles.Add(partVertices.Count - 1);
            }

            // Add the last triangle between the last 2 vertices and the first one
            centerTriangles.Add(partVertices.Count - 2);
            centerTriangles.Add(sizeVertsBeforeCenter);
            centerTriangles.Add(partVertices.Count - 1);
        }
        else
        {
            // Build triangles from the bottom of the list
            for (int i = sizeVertsBeforeCenter; i < partVertices.Count - 1; i++)
            {
                centerTriangles.Add(i);
                centerTriangles.Add(partVertices.Count - 1);
                centerTriangles.Add(i + 1);
            }

            // Add the last triangle between the first 2 vertices and the last one
            centerTriangles.Add(partVertices.Count - 2);
            centerTriangles.Add(partVertices.Count - 1);
            centerTriangles.Add(sizeVertsBeforeCenter);
        }

        // Add the new triangles to the part
        partTriangles.AddRange(centerTriangles);

        // Calculate the new normals based on the part
        if (top)
        {
            normal = TopPart.transform.InverseTransformVector(-CutPosition);
        }
        else
        {
            normal = BottomPart.transform.InverseTransformVector(CutPosition);
        }

        for (int i = sizeVertsBeforeCenter; i < partVertices.Count; i++)
        {
            // Add the new normals
            partUVs.Add(new Vector2(0, 0));
            partNormals.Add(normal.normalized * 3);
        }

    }

    // Creates part from part lists
    private void CreatePart(GameObject part, List<Vector3> partVertices, List<int> partTriangles, List<Vector2> partUVs, List<Vector3> partNormals)
    {
        // Add a blank mesh and mesh renderer
        part.AddComponent<MeshFilter>();
        part.AddComponent<MeshRenderer>();

        // Dissable gravity for a cool effect
        part.AddComponent<Rigidbody>().useGravity = false;

        // Build the new mesh using part data
        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;
        partMesh.Clear();
        partMesh.vertices = partVertices.ToArray();
        partMesh.triangles = partTriangles.ToArray();
        partMesh.uv = partUVs.ToArray();
        partMesh.normals = partNormals.ToArray();

        // Recalculate the objects hitbox
        partMesh.RecalculateBounds();

        // Set the material to match the original objects
        part.GetComponent<Renderer>().material = CurrentTarget.GetComponent<Renderer>().material;

        // Set the mesh to convex to allow rigid body collisions
        part.AddComponent<MeshCollider>().convex = true;
    }
}