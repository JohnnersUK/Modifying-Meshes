using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Slicing : MonoBehaviour
{
    // Maximum amount of cuts when shattering glass
    public int MaxCuts;

    private GameObject CurrentTarget;
    public List<GameObject> TargetList;
    private List<GameObject> LockedTargetList;

    // Components of the cutting plane
    public GameObject CuttingPlane;
    private Vector3 PlaneDirection;
    private Vector3 PlanePosition;

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
                if(hit.transform.gameObject != CuttingPlane)
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
        // Re-Initialize core components
        InitializeComponents();

        // Set the position of the cut
        Vector3 origin = CuttingPlane.transform.position;
        CuttingPlane.transform.position = hitPos;

        PlaneDirection = (-CuttingPlane.transform.forward).normalized;
        PlanePosition = CuttingPlane.transform.position;

        // Initilize the target components
        Mesh targetMesh = CurrentTarget.GetComponent<MeshFilter>().mesh;

        int[] triangles = targetMesh.triangles;
        Vector2[] uvs = targetMesh.uv;
        Vector3[] vertices = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;

        TopPart = new GameObject("TopPart");
        BottomPart = new GameObject("BottomPart");

        // Loop through each triangle, checking for intersections
        for (int i = 0; i < triangles.Length; i += 3)
        {
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
        Vector3 center = Vector3.zero;
        for (int i = 0; i < CenterVertices.Count; i++)
            center += CenterVertices[i];
        center /= CenterVertices.Count;

        // Order the vertices clockwise
        IOrderedEnumerable<Vector3> orderedCenterVerts;
        if (PlaneDirection.y != 0)
        {
            float normalDir = Mathf.Sign(PlaneDirection.y);
            orderedCenterVerts = CenterVertices.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x));
        }
        else
        {
            float normalDir = Mathf.Sign(PlaneDirection.z);
            orderedCenterVerts = CenterVertices.OrderBy(x => normalDir * Mathf.Atan2((x - center).x, (x - center).y));
        }

        // Fill the gaps left after slicing
        CapGap(TopVertices, TopTriangles, TopUVs, TopNormals, orderedCenterVerts, center, true);
        CapGap(BottomVertices, BottomTriangles, BottomUVs, BottomNormals, orderedCenterVerts, center, false);

        // Create the two new GameObjects
        CreatePart(TopPart, TopVertices, TopTriangles, TopUVs, TopNormals);
        CreatePart(BottomPart, BottomVertices, BottomTriangles, BottomUVs, BottomNormals);

        // Remove the original game object
        TargetList.Remove(CurrentTarget);
        Destroy(CurrentTarget);
    }

    // Checks for intersections
    private void CheckIntersection()
    {
        float v1Side = Mathf.Sign(Vector3.Dot(PlaneDirection, TempVertices[0] - PlanePosition));
        float v2Side = Mathf.Sign(Vector3.Dot(PlaneDirection, TempVertices[1] - PlanePosition));
        float v3Side = Mathf.Sign(Vector3.Dot(PlaneDirection, TempVertices[2] - PlanePosition));

        bool intersect1 = v1Side != v2Side;
        bool intersect2 = v2Side != v3Side;
        bool intersect3 = v1Side != v3Side;

        bool[] intersections = { intersect1, intersect2, intersect3 };

        // If there is an intersection, handle the intersection point
        if (intersections[0] || intersections[1] || intersections[2])
        {
            List<Vector3> tmpUpVerts = new List<Vector3>();
            List<Vector3> tmpDownVerts = new List<Vector3>();

            if (intersections[0])
            {
                SplitTriangles(v1Side, 0, 1, TempVertices, TempUVs, TempNormals, tmpUpVerts, tmpDownVerts);
            }
            if (intersections[1])
            {
                SplitTriangles(v2Side, 1, 2, TempVertices, TempUVs, TempNormals, tmpUpVerts, tmpDownVerts);
            }
            if (intersections[2])
            {
                SplitTriangles(v3Side, 2, 0, TempVertices, TempUVs, TempNormals, tmpUpVerts, tmpDownVerts);
            }
            CreateNewTriangles(tmpUpVerts, tmpDownVerts);
        }
        // If there isn't check which half it belongs to and assign it
        else
        {
            // Top Half
            if (Vector3.Dot(PlaneDirection, (TempVertices[0] - PlanePosition)) >= 0)
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
            // Bottom Half
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
        Vector3 p1 = vertices[v1Index];
        Vector3 p2 = vertices[v2Index];
        Vector2 uv1 = uvs[v1Index];
        Vector2 uv2 = uvs[v2Index];
        Vector3 n1 = normals[v1Index];
        Vector3 n2 = normals[v2Index];

        Vector3 rayDir = (p2 - p1).normalized;
        float t = Vector3.Dot(PlanePosition - p1, PlaneDirection) / Vector3.Dot(rayDir, PlaneDirection);
        Vector3 newVert = p1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        GetNewUVs(newVert, ref newUv, ref newNormal, vertices, uvs, normals);


        Vector3 topNewVert = TopPart.transform.InverseTransformPoint(newVert);
        Vector3 botNewVert = BottomPart.transform.InverseTransformPoint(newVert);
        Vector3 topNewNormal = TopPart.transform.InverseTransformVector(newNormal).normalized;
        Vector3 botNewNormal = BottomPart.transform.InverseTransformVector(newNormal).normalized;

        if (vertexSide > 0)
        {
            p1 = TopPart.transform.InverseTransformPoint(p1);
            p2 = BottomPart.transform.InverseTransformPoint(p2);
            n1 = TopPart.transform.InverseTransformVector(n1).normalized;
            n2 = BottomPart.transform.InverseTransformVector(n2).normalized;

            if (!newTopVertices.Contains(p1))
            {
                newTopVertices.Add(p1);
                TopUVs.Add(uv1);
                TopNormals.Add(n1);
            }

            newTopVertices.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            newBottomVertices.Add(botNewVert);
            BottomUVs.Add(newUv);
            BottomNormals.Add(botNewNormal);

            if (!newBottomVertices.Contains(p2))
            {
                newBottomVertices.Add(p2);
                BottomUVs.Add(uv2);
                BottomNormals.Add(n2);
            }

            CenterVertices.Add(topNewVert);

        }
        else
        {
            p2 = TopPart.transform.InverseTransformPoint(p2);
            p1 = BottomPart.transform.InverseTransformPoint(p1);
            n2 = TopPart.transform.InverseTransformVector(n2).normalized;
            n1 = BottomPart.transform.InverseTransformVector(n1).normalized;

            newTopVertices.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            if (!newTopVertices.Contains(p2))
            {
                newTopVertices.Add(p2);
                TopUVs.Add(uv2);
                TopNormals.Add(n2);
            }
            if (!newBottomVertices.Contains(p1))
            {
                newBottomVertices.Add(p1);
                BottomUVs.Add(uv1);
                BottomNormals.Add(n1);
            }

            newBottomVertices.Add(botNewVert);
            BottomUVs.Add(newUv);
            BottomNormals.Add(botNewNormal);

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

        // find the uv corresponding to point f
        newNormal = normals[0] * a1 + normals[1] * a2 + normals[2] * a3;
        newUV = uvs[0] * a1 + uvs[1] * a2 + uvs[2] * a3;
    }

    // Creates new triangles from split parts
    private void CreateNewTriangles(List<Vector3> tempTopVertices, List<Vector3> tempBottomVertices)
    {
        int upLastInsert = TopVertices.Count;
        int downLastInsert = BottomVertices.Count;

        BottomVertices.AddRange(tempBottomVertices);
        TopVertices.AddRange(tempTopVertices);

        TopTriangles.Add(upLastInsert);
        TopTriangles.Add(upLastInsert + 1);
        TopTriangles.Add(upLastInsert + 2);

        if (tempTopVertices.Count > 3)
        {
            TopTriangles.Add(upLastInsert);
            TopTriangles.Add(upLastInsert + 2);
            TopTriangles.Add(upLastInsert + 3);
        }

        BottomTriangles.Add(downLastInsert);
        BottomTriangles.Add(downLastInsert + 1);
        BottomTriangles.Add(downLastInsert + 2);

        if (tempBottomVertices.Count > 3)
        {
            BottomTriangles.Add(downLastInsert);
            BottomTriangles.Add(downLastInsert + 2);
            BottomTriangles.Add(downLastInsert + 3);

        }

    }

    // Fills gap left by slice
    private void CapGap(List<Vector3> partVertices, List<int> partTriangles, List<Vector2> partUVs, List<Vector3> partNormals, IOrderedEnumerable<Vector3> orderedSliceVerts, Vector3 center, bool top)
    {
        List<int> centerTris = new List<int>();

        int sizeVertsBeforeCenter = partVertices.Count;
        partVertices.AddRange(orderedSliceVerts);
        partVertices.Add(center);

        if (top)
        {
            for (int i = sizeVertsBeforeCenter; i < partVertices.Count - 1; i++)
            {
                centerTris.Add(i);
                centerTris.Add(i + 1);
                centerTris.Add(partVertices.Count - 1);
            }

            centerTris.Add(partVertices.Count - 2);
            centerTris.Add(sizeVertsBeforeCenter);
            centerTris.Add(partVertices.Count - 1);
        }
        else
        {
            for (int i = sizeVertsBeforeCenter; i < partVertices.Count - 1; i++)
            {
                centerTris.Add(i);
                centerTris.Add(partVertices.Count - 1);
                centerTris.Add(i + 1);
            }

            centerTris.Add(partVertices.Count - 2);
            centerTris.Add(partVertices.Count - 1);
            centerTris.Add(sizeVertsBeforeCenter);
        }

        partTriangles.AddRange(centerTris);

        Vector3 normal;
        if (top)
            normal = TopPart.transform.InverseTransformVector(-PlanePosition);
        else
            normal = BottomPart.transform.InverseTransformVector(PlanePosition);
        for (int i = sizeVertsBeforeCenter; i < partVertices.Count; i++)
        {
            partUVs.Add(new Vector2(0, 0));
            partNormals.Add(normal.normalized * 3);
        }

    }

    // Creates part from part lists
    private void CreatePart(GameObject part, List<Vector3> partVertices, List<int> partTriangles, List<Vector2> partUVs, List<Vector3> partNormals)
    {
        part.AddComponent<MeshFilter>();
        part.AddComponent<MeshRenderer>();

        part.AddComponent<Rigidbody>().useGravity = false;

        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;

        partMesh.Clear();
        partMesh.vertices = partVertices.ToArray();
        partMesh.triangles = partTriangles.ToArray();
        partMesh.uv = partUVs.ToArray();
        partMesh.normals = partNormals.ToArray();
        partMesh.RecalculateBounds();
        part.GetComponent<Renderer>().material = CurrentTarget.GetComponent<Renderer>().material;

        part.AddComponent<MeshCollider>().convex = true;
    }
}