using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Slicing : MonoBehaviour
{

    private GameObject target;

    // Components of the cutting plane
    public GameObject cuttingPlane;
    private Vector3 planeDirection;
    private Vector3 planePosition;

    // Top components 
    private GameObject topPart;

    private List<Vector3> upVerts;
    private List<Vector3> upNormals;
    private List<Vector2> upUVs;
    private List<int> upTris;

    // Bottom components
    private GameObject bottomPart;

    private List<Vector3> downVerts;
    private List<Vector3> downNormals;
    private List<Vector2> downUVs;
    private List<int> downTris;

    // Newly generated vertices around cut
    private List<Vector3> centerVerts;

    // Temporary Components
    private Vector3[] tempVerts;
    private Vector2[] tempUvs;
    private Vector3[] tempNormals;

    private void Start()
    {
        // Initilize components
        tempVerts = new Vector3[3];
        tempUvs = new Vector2[3];
        tempNormals = new Vector3[3];

        upVerts = new List<Vector3>();
        upTris = new List<int>();
        upUVs = new List<Vector2>();
        upNormals = new List<Vector3>();

        downVerts = new List<Vector3>();
        downTris = new List<int>();
        downUVs = new List<Vector2>();
        downNormals = new List<Vector3>();

        centerVerts = new List<Vector3>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                target = hit.transform.gameObject;
                // Slice the mesh
                SliceMesh();

                // Destroy the original game object
                Destroy(target);
            }
        }
    }

    private void SliceMesh()
    {
        // Set the position of the cut
        planeDirection = (-cuttingPlane.transform.forward).normalized;
        planePosition = cuttingPlane.transform.position;

        // Initilize the target components
        Mesh targetMesh = target.GetComponent<MeshFilter>().mesh;

        int[] tris = targetMesh.triangles;
        Vector2[] uvs = targetMesh.uv;
        Vector3[] verts = targetMesh.vertices;
        Vector3[] normals = targetMesh.normals;

        topPart = new GameObject("TopPart");
        bottomPart = new GameObject("BottomPart");

        // Loop through each triangle, checking for intersections
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 worldp1 = target.transform.TransformPoint(verts[tris[i]]);
            Vector3 worldp2 = target.transform.TransformPoint(verts[tris[i + 1]]);
            Vector3 worldp3 = target.transform.TransformPoint(verts[tris[i + 2]]);

            Vector2 uv1 = uvs[tris[i]];
            Vector2 uv2 = uvs[tris[i + 1]];
            Vector2 uv3 = uvs[tris[i + 2]];

            Vector3 normal1 = target.transform.TransformVector(normals[tris[i]]);
            Vector3 normal2 = target.transform.TransformVector(normals[tris[i + 1]]);
            Vector3 normal3 = target.transform.TransformVector(normals[tris[i + 2]]);

            // Check for intersections
            bool[] intersected = CheckIntersection(worldp1, worldp2, worldp3);

            // If there is an intersection, handle the intersection point
            if (intersected[0] || intersected[1] || intersected[2])
            {
                tempVerts[0] = worldp1;
                tempVerts[1] = worldp2;
                tempVerts[2] = worldp3;

                tempUvs[0] = uv1;
                tempUvs[1] = uv2;
                tempUvs[2] = uv3;

                tempNormals[0] = normal1;
                tempNormals[1] = normal2;
                tempNormals[2] = normal3;

                ResolveIntersections(intersected, tempVerts, tempUvs, tempNormals);
            }
            // If there isn't check which half it belongs to and assign it
            else
            {
                // Top Half
                if (Vector3.Dot(planeDirection, (worldp1 - planePosition)) >= 0)
                {
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp1));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp2));
                    upVerts.Add(topPart.transform.InverseTransformPoint(worldp3));

                    upTris.Add(upVerts.Count - 3);
                    upTris.Add(upVerts.Count - 2);
                    upTris.Add(upVerts.Count - 1);

                    upUVs.Add(uv1);
                    upUVs.Add(uv2);
                    upUVs.Add(uv3);

                    upNormals.Add(topPart.transform.InverseTransformVector(normal1));
                    upNormals.Add(topPart.transform.InverseTransformVector(normal2));
                    upNormals.Add(topPart.transform.InverseTransformVector(normal3));
                }
                // Bottom Half
                else
                {
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp1));
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp2));
                    downVerts.Add(bottomPart.transform.InverseTransformPoint(worldp3));

                    downTris.Add(downVerts.Count - 3);
                    downTris.Add(downVerts.Count - 2);
                    downTris.Add(downVerts.Count - 1);

                    downUVs.Add(uv1);
                    downUVs.Add(uv2);
                    downUVs.Add(uv3);

                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal1));
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal2));
                    downNormals.Add(bottomPart.transform.InverseTransformVector(normal3));
                }
            }

        }

        // Add verts along the cut line
        Vector3 center = Vector3.zero;
        for (int i = 0; i < centerVerts.Count; i++)
            center += centerVerts[i];
        center /= centerVerts.Count;

        IOrderedEnumerable<Vector3> orderedInnerVerts;

        if (planeDirection.y != 0)
        {
            float normalDir = Mathf.Sign(planeDirection.y);
            orderedInnerVerts = centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).z, (x - center).x));
        }
        else
        {
            float normalDir = Mathf.Sign(planeDirection.z);
            orderedInnerVerts = centerVerts.OrderBy(x => normalDir * Mathf.Atan2((x - center).x, (x - center).y));
        }

        GapFill(upVerts, upTris, upUVs, upNormals, orderedInnerVerts, center, true);
        GapFill(downVerts, downTris, downUVs, downNormals, orderedInnerVerts, center, false);

        // Create the two new GameObjects
        createPart(topPart, upVerts, upTris, upUVs, upNormals);
        createPart(bottomPart, downVerts, downTris, downUVs, downNormals);
    }

    private void GapFill(List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNormals, IOrderedEnumerable<Vector3> orderedInnerVerts, Vector3 center, bool top)
    {
        List<int> centerTris = new List<int>();

        int sizeVertsBeforeCenter = partVerts.Count;
        partVerts.AddRange(orderedInnerVerts);
        partVerts.Add(center);

        if (top)
        {
            for (int i = sizeVertsBeforeCenter; i < partVerts.Count - 1; i++)
            {
                centerTris.Add(i);
                centerTris.Add(i + 1);
                centerTris.Add(partVerts.Count - 1);
            }

            centerTris.Add(partVerts.Count - 2);
            centerTris.Add(sizeVertsBeforeCenter);
            centerTris.Add(partVerts.Count - 1);
        }
        else
        {
            for (int i = sizeVertsBeforeCenter; i < partVerts.Count - 1; i++)
            {
                centerTris.Add(i);
                centerTris.Add(partVerts.Count - 1);
                centerTris.Add(i + 1);
            }

            centerTris.Add(partVerts.Count - 2);
            centerTris.Add(partVerts.Count - 1);
            centerTris.Add(sizeVertsBeforeCenter);
        }

        partTris.AddRange(centerTris);

        Vector3 normal;
        if (top)
            normal = topPart.transform.InverseTransformVector(-planePosition);
        else
            normal = bottomPart.transform.InverseTransformVector(planePosition);
        for (int i = sizeVertsBeforeCenter; i < partVerts.Count; i++)
        {
            partUvs.Add(new Vector2(0, 0));
            partNormals.Add(normal.normalized * 3);
        }

    }

    private void createPart(GameObject part, List<Vector3> partVerts, List<int> partTris, List<Vector2> partUvs, List<Vector3> partNorms)
    {
        part.AddComponent<MeshFilter>();
        part.AddComponent<MeshRenderer>();

        part.AddComponent<Rigidbody>().useGravity = false;

        Mesh partMesh = part.GetComponent<MeshFilter>().mesh;

        partMesh.Clear();
        partMesh.vertices = partVerts.ToArray();
        partMesh.triangles = partTris.ToArray();
        partMesh.uv = partUvs.ToArray();
        partMesh.normals = partNorms.ToArray();
        partMesh.RecalculateBounds();
        part.GetComponent<Renderer>().material = target.GetComponent<Renderer>().material;

        part.AddComponent<MeshCollider>().convex = true;
    }

    private bool[] CheckIntersection(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float upOrDown = Mathf.Sign(Vector3.Dot(planeDirection, p1 - planePosition));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeDirection, p2 - planePosition));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeDirection, p3 - planePosition));

        bool intersect1 = upOrDown != upOrDown2;
        bool intersect2 = upOrDown2 != upOrDown3;
        bool intersect3 = upOrDown != upOrDown3;

        bool[] intersections = { intersect1, intersect2, intersect3 };

        return intersections;
    }

    private void ResolveIntersections(bool[] intersections, Vector3[] verts, Vector2[] uvs, Vector3[] normals)
    {
        List<Vector3> tmpUpVerts = new List<Vector3>();
        List<Vector3> tmpDownVerts = new List<Vector3>();

        float upOrDown = Mathf.Sign(Vector3.Dot(planeDirection, verts[0] - planePosition));
        float upOrDown2 = Mathf.Sign(Vector3.Dot(planeDirection, verts[1] - planePosition));
        float upOrDown3 = Mathf.Sign(Vector3.Dot(planeDirection, verts[2] - planePosition));

        if (intersections[0])
        {
            AddToCorrectSideList(upOrDown, 0, 1, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[1])
        {
            AddToCorrectSideList(upOrDown2, 1, 2, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        if (intersections[2])
        {
            AddToCorrectSideList(upOrDown3, 2, 0, verts, uvs, normals, tmpUpVerts, tmpDownVerts);
        }
        HandleTriOrder(tmpUpVerts, tmpDownVerts);
    }

    private void CalculateCentroid(Vector3 newPoint, ref Vector2 newUV, ref Vector3 newNormal, Vector3[] points, Vector2[] uvs, Vector3[] normals)
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

    private void HandleTriOrder(List<Vector3> tmpUpVerts, List<Vector3> tmpDownVerts)
    {
        int upLastInsert = upVerts.Count;
        int downLastInsert = downVerts.Count;

        downVerts.AddRange(tmpDownVerts);
        upVerts.AddRange(tmpUpVerts);

        upTris.Add(upLastInsert);
        upTris.Add(upLastInsert + 1);
        upTris.Add(upLastInsert + 2);

        if (tmpUpVerts.Count > 3)
        {
            upTris.Add(upLastInsert);
            upTris.Add(upLastInsert + 2);
            upTris.Add(upLastInsert + 3);
        }

        downTris.Add(downLastInsert);
        downTris.Add(downLastInsert + 1);
        downTris.Add(downLastInsert + 2);

        if (tmpDownVerts.Count > 3)
        {
            downTris.Add(downLastInsert);
            downTris.Add(downLastInsert + 2);
            downTris.Add(downLastInsert + 3);

        }

    }

    private void AddToCorrectSideList(float upOrDown, int pIndex1, int pIndex2, Vector3[] verts, Vector2[] uvs, Vector3[] normals, List<Vector3> top, List<Vector3> bottom)
    {
        Vector3 p1 = verts[pIndex1];
        Vector3 p2 = verts[pIndex2];
        Vector2 uv1 = uvs[pIndex1];
        Vector2 uv2 = uvs[pIndex2];
        Vector3 n1 = normals[pIndex1];
        Vector3 n2 = normals[pIndex2];

        Vector3 rayDir = (p2 - p1).normalized;
        float t = Vector3.Dot(planePosition - p1, planeDirection) / Vector3.Dot(rayDir, planeDirection);
        Vector3 newVert = p1 + rayDir * t;
        Vector2 newUv = new Vector2(0, 0);
        Vector3 newNormal = new Vector3(0, 0, 0);
        CalculateCentroid(newVert, ref newUv, ref newNormal, verts, uvs, normals);


        Vector3 topNewVert = topPart.transform.InverseTransformPoint(newVert);
        Vector3 botNewVert = bottomPart.transform.InverseTransformPoint(newVert);
        Vector3 topNewNormal = topPart.transform.InverseTransformVector(newNormal).normalized;
        Vector3 botNewNormal = bottomPart.transform.InverseTransformVector(newNormal).normalized;

        if (upOrDown > 0)
        {
            p1 = topPart.transform.InverseTransformPoint(p1);
            p2 = bottomPart.transform.InverseTransformPoint(p2);
            n1 = topPart.transform.InverseTransformVector(n1).normalized;
            n2 = bottomPart.transform.InverseTransformVector(n2).normalized;

            if (!top.Contains(p1))
            {
                top.Add(p1);
                upUVs.Add(uv1);
                upNormals.Add(n1);
            }

            top.Add(topNewVert);
            upUVs.Add(newUv);
            upNormals.Add(topNewNormal);

            bottom.Add(botNewVert);
            downUVs.Add(newUv);
            downNormals.Add(botNewNormal);

            if (!bottom.Contains(p2))
            {
                bottom.Add(p2);
                downUVs.Add(uv2);
                downNormals.Add(n2);
            }

            centerVerts.Add(topNewVert);

        }
        else
        {
            p2 = topPart.transform.InverseTransformPoint(p2);
            p1 = bottomPart.transform.InverseTransformPoint(p1);
            n2 = topPart.transform.InverseTransformVector(n2).normalized;
            n1 = bottomPart.transform.InverseTransformVector(n1).normalized;

            top.Add(topNewVert);
            upUVs.Add(newUv);
            upNormals.Add(topNewNormal);

            if (!top.Contains(p2))
            {
                top.Add(p2);
                upUVs.Add(uv2);
                upNormals.Add(n2);
            }
            if (!bottom.Contains(p1))
            {
                bottom.Add(p1);
                downUVs.Add(uv1);
                downNormals.Add(n1);
            }

            bottom.Add(botNewVert);
            downUVs.Add(newUv);
            downNormals.Add(botNewNormal);

            centerVerts.Add(botNewVert);
        }

    }
}
