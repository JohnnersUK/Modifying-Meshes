using UnityEngine;

public class MeshModifier : MonoBehaviour
{
    // Start is called before the first frame update
    Mesh mesh;

    Vector3[] Vertices;
    Vector2[] UVs;
    int[] Triangles;

    Vector3[] newVertices;
    Vector2[] newUVs;
    int[] newTriangles;

    int gapStart;

    Vector3[] cutVertices;
    Vector2[] cutUvs;
    int[] cutTriangles;

    public Material SegmentMaterial;

    float width = 1;
    float height = 1;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 1000.0f))
            {
                Cut(hit.transform.gameObject, hit.triangleIndex);
            }
        }
    }

    void Cut(GameObject obj, int index)
    {
        int i = 0;
        int j = 0;

        // Get the objects material
        SegmentMaterial = obj.GetComponent<Renderer>().material;

        // Destroy the old mesh collider
        Destroy(obj.GetComponent<MeshCollider>());

        // Get original mesh components
        mesh = obj.GetComponent<MeshFilter>().mesh;
        Vertices = mesh.vertices;
        UVs = mesh.uv;
        Triangles = mesh.triangles;

        // Create new vertices
        newVertices = new Vector3[Vertices.Length + 1];

        for (int k = 0; k < Vertices.Length; k++)
        {
            newVertices[k] = Vertices[k];
        }

        // Add a centroid vertex
        Vector3 centroid = new Vector3(0, 0, 0);
        for (int k = 0; k < Vertices.Length; k++)
        {
            centroid.x += Vertices[k].x;
            centroid.y += Vertices[k].y;
            centroid.z = Vertices[k].z;
        }
        centroid /= Vertices.Length;

        newVertices[newVertices.Length - 1] = centroid;

        // create new triangles
        newTriangles = new int[Triangles.Length + 9];

        // Create segment components
        cutTriangles = new int[3];
        cutVertices = new Vector3[4];
        cutUvs = new Vector2[cutVertices.Length];

        // Whilst there are unprocessed triangles
        while (j < Triangles.Length)
        {
            // If the triangle isn't the one clicked on
            if (j != index * 3)
            {
                // Add it to the new triangles
                for (int k = 0; k < 3; k++)
                {
                    newTriangles[i++] = Triangles[j++];
                }
            }
            // If the triangle is the one clicked on
            else
            {
                newTriangles[i++] = Triangles[j + 1];
                newTriangles[i++] = Triangles[j + 2];
                newTriangles[i++] = newVertices.Length-1;

                newTriangles[i++] = Triangles[j + 1];
                newTriangles[i++] = Triangles[j + 3];
                newTriangles[i++] = newVertices.Length - 1;

                newTriangles[i++] = Triangles[j + 2];
                newTriangles[i++] = Triangles[j + 3];
                newTriangles[i++] = newVertices.Length - 1;

                for (int k = 0; k < 3; k++)
                {
                    cutVertices[k] = Vertices[Triangles[j++]];
                    Debug.Log(cutVertices[k]);
                }
            }
        }


        // Reconstruct the object
        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;
        obj.AddComponent<MeshCollider>();

        //Construct the cut segment
        GameObject Seg = new GameObject(obj.name + " segment");
        Seg.transform.position = obj.transform.position;

        Mesh segMesh = new Mesh();

        // Assign the new verts
        segMesh.vertices = cutVertices;

        // Assign the new uvs
        for (int k = 0; k < cutUvs.Length; k++)
        {
            cutUvs[k] = new Vector2(cutVertices[k].x, cutVertices[k].z);
        }
        segMesh.uv = cutUvs;

        // Assign the new triangles
        segMesh.triangles = new int[] { 0, 1, 2,
                                        0, 1, 3,
                                        0, 2, 3,
                                        1, 2, 3 };

        Seg.AddComponent<MeshFilter>().mesh = segMesh;
        Seg.AddComponent<MeshRenderer>().material = SegmentMaterial;
        Seg.AddComponent<MeshCollider>().convex = true;

        Seg.AddComponent<Rigidbody>();
    }
}
