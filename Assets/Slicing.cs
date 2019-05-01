using System.Collections.Generic;
using UnityEngine;

public class Slicing : MonoBehaviour
{
    public int MaxCuts;

    private GameObject Target;
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

    private List<Vector3> DownVertices;
    private List<Vector3> DownNormals;
    private List<Vector2> DownUVs;
    private List<int> DownTriangles;

    // Newly generated vertices around cut
    private List<Vector3> CenterVertices;

    // Temporary Components
    private Vector3[] TempVertices;
    private Vector2[] TempUVs;
    private Vector3[] TempNormals;

    private void Start()
    {
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
                Target = TargetList[TargetList.Count - 1];
                SliceMesh(transform.position);

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
                    Target = TargetList[0];
                    SliceMesh(hit.transform.position);

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

        DownVertices = new List<Vector3>();
        DownTriangles = new List<int>();
        DownUVs = new List<Vector2>();
        DownNormals = new List<Vector3>();

        CenterVertices = new List<Vector3>();
    }

    /// <summary>
    /// Slices the target game object
    /// </summary>
    /// <param name="HitPos">Position of the cut, by default send in the players location</param>
    private void SliceMesh(Vector3 HitPos)
    {
        // Re-initilize components for general clean-up
        InitializeComponents();

        // Set the position of the cut
        Vector3 origin = CuttingPlane.transform.position;
        //cuttingPlane.transform.position = HitPos;

        CutDirection = (-CuttingPlane.transform.forward).normalized;
        CutPosition = CuttingPlane.transform.position;

        // Initilize the target components
        Mesh targetMesh = Target.GetComponent<MeshFilter>().mesh;

        int[] tris = targetMesh.triangles;
        Vector2[] uvs = targetMesh.uv;
        Vector3[] verts = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;

        TopPart = new GameObject("TopPart");
        BottomPart = new GameObject("BottomPart");

        // Loop through each triangle, checking for intersections
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 worldp1 = Target.transform.TransformPoint(verts[tris[i]]);
            Vector3 worldp2 = Target.transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 worldp3 = Target.transform.TransformPoint(verts[tris[i + 2]]);

            Vector2 uv1 = uvs[tris[i]];
            Vector2 uv2 = uvs[tris[i + 1]];
            Vector2 uv3 = uvs[tris[i + 2]];

            Vector3 normal1 = Target.transform.TransformVector(normals[tris[i]]);
            Vector3 normal2 = Target.transform.TransformVector(normals[tris[i + 1]]);
            Vector3 normal3 = Target.transform.TransformVector(normals[tris[i + 2]]);

            // Check for intersections
            bool[] intersected = CheckIntersection(worldp1, worldp2, worldp3);

            // If there is an intersection, handle the intersection point
            if (intersected[0] || intersected[1] || intersected[2])
            {
                TempVertices[0] = worldp1;
                TempVertices[1] = worldp2;
                TempVertices[2] = worldp3;

                TempUVs[0] = uv1;
                TempUVs[1] = uv2;
                TempUVs[2] = uv3;

                TempNormals[0] = normal1;
                TempNormals[1] = normal2;
                TempNormals[2] = normal3;

                ResolveIntersections(intersected, TempVertices, TempUVs, TempNormals);
            }
            // If there isn't check which half it belongs to and assign it
            else
            {
                // Top Half
                if (Vector3.Dot(CutDirection, (worldp1 - CutPosition)) >= 0)
                {
                    TopVertices.Add(TopPart.transform.InverseTransformPoint(worldp1));
                    TopVertices.Add(TopPart.transform.InverseTransformPoint(worldp2));
                    TopVertices.Add(TopPart.transform.InverseTransformPoint(worldp3));

                    TopTriangles.Add(TopVertices.Count - 3);
                    TopTriangles.Add(TopVertices.Count - 2);
                    TopTriangles.Add(TopVertices.Count - 1);

                    TopUVs.Add(uv1);
                    TopUVs.Add(uv2);
                    TopUVs.Add(uv3);

                    TopNormals.Add(TopPart.transform.InverseTransformVector(normal1));
                    TopNormals.Add(TopPart.transform.InverseTransformVector(normal2));
                    TopNormals.Add(TopPart.transform.InverseTransformVector(normal3));
                }
                // Bottom Half
                else
                {
                    DownVertices.Add(BottomPart.transform.InverseTransformPoint(worldp1));
                    DownVertices.Add(BottomPart.transform.InverseTransformPoint(worldp2));
                    DownVertices.Add(BottomPart.transform.InverseTransformPoint(worldp3));

                    DownTriangles.Add(DownVertices.Count - 3);
                    DownTriangles.Add(DownVertices.Count - 2);
                    DownTriangles.Add(DownVertices.Count - 1);

                    DownUVs.Add(uv1);
                    DownUVs.Add(uv2);
                    DownUVs.Add(uv3);

                    DownNormals.Add(BottomPart.transform.InverseTransformVector(normal1));
                    DownNormals.Add(BottomPart.transform.InverseTransformVector(normal2));
                    DownNormals.Add(BottomPart.transform.InverseTransformVector(normal3));
                }
            }

        }

        // Calculate the center of the cut
        Vector3 center = Vector3.zero;
        for (int i = 0; i < CenterVertices.Count; i++)
        {
            center += CenterVertices[i];
        }
        center /= CenterVertices.Count;

        //TODO: Order the center verts clockwise

        // Fill the gap in both game objects left by the cut
        GapFill(TopVertices, TopTriangles, TopUVs, TopNormals, CenterVertices, center, true);
        GapFill(DownVertices, DownTriangles, DownUVs, DownNormals, CenterVertices, center, false);

        // Create the two new GameObjects
        createPart(TopPart, TopVertices, TopTriangles, TopUVs, TopNormals);
        createPart(BottomPart, DownVertices, DownTriangles, DownUVs, DownNormals);

        // Remove the old target from the target list and destroy it
        TargetList.Remove(Target);
        Destroy(Target);

        // Reset the position of the cutting plane
        CuttingPlane.transform.position = origin;
    }

    // Fills the gap left in each side after a cut
    private void GapFill(List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals, List<Vector3> centerVerts, Vector3 center, bool top)
    {
        List<int> centerTris = new List<int>();

        // Add the list of center vertices to the segments vertices
        int sizeVertsBeforeCenter = partVerts.Count;
        partVerts.AddRange(centerVerts);
        partVerts.Add(center);

        // For each vert in center verts, add a triangle betweiin this vert, the next vert and the center
        for (int i = sizeVertsBeforeCenter; i < partVerts.Count - 1; i++)
        {
            centerTris.Add(i);
            centerTris.Add(i + 1);
            centerTris.Add(partVerts.Count - 1);
        }

        centerTris.Add(partVerts.Count - 2);
        centerTris.Add(sizeVertsBeforeCenter);
        centerTris.Add(partVerts.Count - 1);

        partTris.AddRange(centerTris);

        // Flip the normal depending on which side of the object we are reconstructing
        Vector3 normal;
        if (top)
        {
            normal = TopPart.transform.InverseTransformVector(-CutPosition);
        }
        else
        {
            normal = BottomPart.transform.InverseTransformVector(CutPosition);
        }

        for (int i = sizeVertsBeforeCenter; i < partVerts.Count; i++)
        {
            partUvs.Add(new Vector2(0, 0));
            partNormals.Add(normal.normalized * 3);
        }
    }

    // Constructs a side of the cut
    private void createPart(GameObject part, List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNorms)
    {
        part.AddComponent<MeshFilter>();
        part.AddComponent<MeshRenderer>();

        Rigidbody rb = part.AddComponent<Rigidbody>();
        rb.mass = 10;


        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;

        partMesh.Clear();
        partMesh.vertices = partVerts.ToArray();
        partMesh.triangles = partTris.ToArray();
        partMesh.uv = partUvs.ToArray();
        partMesh.normals = partNorms.ToArray();
        partMesh.RecalculateBounds();
        part.GetComponent<Renderer>().material = Target.GetComponent<Renderer>().material;

        part.AddComponent<MeshCollider>().convex = true;

        part.name = Target.name + " " + part.name;
    }

    // Checks for an intersection across the cutting line
    private bool[] CheckIntersection(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(CutDirection, p1 - CutPosition));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(CutDirection, p2 - CutPosition));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(CutDirection, p3 - CutPosition));

        bool intersect1 = upOrDown != upOrDown2;
        bool intersect2 = upOrDown2 != upOrDown3;
        bool intersect3 = upOrDown != upOrDown3;

        bool[] intersections = { intersect1, intersect2, intersect3 };

        return intersections;
    }

    // If theres an intersection, add its verts to the correct side and construct new triangles
    private void ResolveIntersections(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals)
    {
        List<Vector3> tmpUpVerts = new List<Vector3>();
        List<Vector3> tmpDownVerts = new List<Vector3>();
        float upOrDown;

        if (intersections[0])
        {
            upOrDown = Mathf.Sign(Vector3.Dot(CutDirection, verts[0] - CutPosition));
            AddToCorrectSide(upOrDown, 0, 1, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[1])
        {
            upOrDown = Mathf.Sign(Vector3.Dot(CutDirection, verts[1] - CutPosition));
            AddToCorrectSide(upOrDown, 1, 2, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[2])
        {
            upOrDown = Mathf.Sign(Vector3.Dot(CutDirection, verts[2] - CutPosition));
            AddToCorrectSide(upOrDown, 2, 0, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        AddNewTriangles(tmpUpVerts, tmpDownVerts);
    }

    // Calculates the new UV and normal for given side
    private void CalculateNewUVs(Vector3 newPoint, ref Vector2 newUV, ref Vector3 newNormal, Vector3[] points, Vector2[] uvs, Vector3[] normals)
    {
        Vector3 f1 = points[0] - newPoint;
        Vector3 f2 = points[1] - newPoint;
        Vector3 f3 = points[2] - newPoint;

        // calculate the triangle areas
        float areaMainTri = Vector3.Cross(points[0] - points[1], points[0] - points[2]).magnitude; // main triangle area a
        float a1 = Vector3.Cross(f2, f3).magnitude / areaMainTri; // p1's triangle area / a
        float a2 = Vector3.Cross(f3, f1).magnitude / areaMainTri; // p2's triangle area / a 
        float a3 = Vector3.Cross(f1, f2).magnitude / areaMainTri; // p3's triangle area / a

        // find the uv corresponding to point f
        newNormal = normals[0] * a1 + normals[1] * a2 + normals[2] * a3;
        newUV = uvs[0] * a1 + uvs[1] * a2 + uvs[2] * a3;
    }

    // Adds new triangles to the cut line
    private void AddNewTriangles(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts)
    {
        int upLastInsert = TopVertices.Count;
        int downLastInsert = DownVertices.Count;

        DownVertices.AddRange(tmpDownVerts);
        TopVertices.AddRange(tmpUpVerts);

        TopTriangles.Add(upLastInsert);
        TopTriangles.Add(upLastInsert + 1);
        TopTriangles.Add(upLastInsert + 2);

        if (tmpUpVerts.Count > 3)
        {
            TopTriangles.Add(upLastInsert);
            TopTriangles.Add(upLastInsert + 2);
            TopTriangles.Add(upLastInsert + 3);
        }

        DownTriangles.Add(downLastInsert);
        DownTriangles.Add(downLastInsert + 1);
        DownTriangles.Add(downLastInsert + 2);

        if (tmpDownVerts.Count > 3)
        {
            DownTriangles.Add(downLastInsert);
            DownTriangles.Add(downLastInsert + 2);
            DownTriangles.Add(downLastInsert + 3);

        }

    }

    // Adds intersected vertices to the correct side of the cut
    private void AddToCorrectSide(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, List<Vector3> top, List<Vector3> bottom)
    {
        Vector3 p1 = verts[pIndex1];
        Vector3 p2 = verts[pIndex2];
        Vector2 uv1 = uvs[pIndex1];
        Vector2 uv2 = uvs[pIndex2];
        Vector3 n1 = normals[pIndex1];
        Vector3 n2 = normals[pIndex2];

        Vector3 rayDir = (p2 - p1).normalized;
        float t = Vector3.Dot(CutPosition - p1, CutDirection) / Vector3.Dot(rayDir, CutDirection);
        Vector3 newVert = p1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        CalculateNewUVs(newVert, ref newUv, ref newNormal, verts, uvs, normals);


        Vector3 topNewVert = TopPart.transform.InverseTransformPoint(newVert);
        Vector3 botNewVert = BottomPart.transform.InverseTransformPoint(newVert);
        Vector3 topNewNormal = TopPart.transform.InverseTransformVector(newNormal).normalized;
        Vector3 botNewNormal = BottomPart.transform.InverseTransformVector(newNormal).normalized;

        if (upOrDown > 0)
        {
            p1 = TopPart.transform.InverseTransformPoint(p1);
            p2 = BottomPart.transform.InverseTransformPoint(p2);
            n1 = TopPart.transform.InverseTransformVector(n1).normalized;
            n2 = BottomPart.transform.InverseTransformVector(n2).normalized;

            if (!top.Contains(p1))
            {
                top.Add(p1);
                TopUVs.Add(uv1);
                TopNormals.Add(n1);
            }

            top.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            bottom.Add(botNewVert);
            DownUVs.Add(newUv);
            DownNormals.Add(botNewNormal);

            if (!bottom.Contains(p2))
            {
                bottom.Add(p2);
                DownUVs.Add(uv2);
                DownNormals.Add(n2);
            }

            CenterVertices.Add(topNewVert);

        }
        else
        {
            p2 = TopPart.transform.InverseTransformPoint(p2);
            p1 = BottomPart.transform.InverseTransformPoint(p1);
            n2 = TopPart.transform.InverseTransformVector(n2).normalized;
            n1 = BottomPart.transform.InverseTransformVector(n1).normalized;

            top.Add(topNewVert);
            TopUVs.Add(newUv);
            TopNormals.Add(topNewNormal);

            if (!top.Contains(p2))
            {
                top.Add(p2);
                TopUVs.Add(uv2);
                TopNormals.Add(n2);
            }
            if (!bottom.Contains(p1))
            {
                bottom.Add(p1);
                DownUVs.Add(uv1);
                DownNormals.Add(n1);
            }

            bottom.Add(botNewVert);
            DownUVs.Add(newUv);
            DownNormals.Add(botNewNormal);

            CenterVertices.Add(botNewVert);
        }

    }
}
