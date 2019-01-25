using UnityEngine;

public class MeshModifier : MonoBehaviour
{
    // Start is called before the first frame update
    Mesh mesh;

    Vector3[] Vertices;
    Vector2[] UVs;
    int[] Triangles;

    Vector3[] newVerticies;
    Vector3[] newUVs;
    int[] newTriangles;

    Vector3[] cutVerticies;
    int[] cutTriangles;

    public Material SegmentMaterial;

    float width = 1;
    float height = 1;


    void Start()
    {

    }

    void Cut(GameObject obj, int index)
    {
        int i = 0;
        int j = 0;

        // Destroy the old mesh collider
        Destroy(obj.GetComponent<MeshCollider>());

        // Get original mesh components
        mesh = obj.GetComponent<MeshFilter>().mesh;
        Vertices = mesh.vertices;
        UVs = mesh.uv;
        Triangles = mesh.triangles;

        // Create new mesh components
        newTriangles = new int[Triangles.Length - 3];

        // Create segment components
        cutTriangles = new int[3];
        cutVerticies = new Vector3[4];

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
                for (int k = 0; k < 3; k++)
                {
                    cutVerticies[k] = Vertices[Triangles[j++]];
                    Debug.Log(cutVerticies[k]);
                }
            }
        }

        // Reconstruct the object
        mesh.triangles = newTriangles;
        obj.AddComponent<MeshCollider>();

        //Construct the cut segment
        GameObject Seg = new GameObject(obj.name + " segment");
        Seg.transform.position = obj.transform.position;

        Mesh segMesh = new Mesh();

        // Add a centroid vertex
        float cx = 0.0f;
        float cy = 0.0f;
        float cz = 0.0f;

        for (int l = 0; l < 3; l++)
        {
            cx += cutVerticies[l].x;
            cy += cutVerticies[l].y;
            cz += cutVerticies[l].z;
        }

        cx /= 3;
        cy /= 3;
        cz /= 3;

        cutVerticies[3] = new Vector3(cx, cy, cz);

        segMesh.vertices = cutVerticies;

        segMesh.triangles = new int[] { 0, 1, 2,
                                        0, 1, 3,
                                        0, 2, 3,
                                        1, 2, 3 };

        Seg.AddComponent<MeshFilter>().mesh = segMesh;
        Seg.AddComponent<MeshRenderer>().material = SegmentMaterial;
        Seg.AddComponent<MeshCollider>().convex = true;

        Seg.AddComponent<Rigidbody>();
    }

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
}
