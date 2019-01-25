using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempMeshCreation : MonoBehaviour
{

    public Material mat;

    float width = 1;
    float height = 1;

    // Use this for initialization
    void Start()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];

        vertices[0] = new Vector3(-width, -height);
        vertices[1] = new Vector3(-width, height);
        vertices[2] = new Vector3(width, height);

        float cx = 0.0f;
        float cy = 0.0f;
        float cz = 0.0f;

        for (int l = 0; l < 3; l++)
        {
            cx += vertices[l].x;
            cy += vertices[l].y;
            cz += vertices[l].z;
        }

        cx /= 3;
        cy /= 3;
        cz /= 3;

        vertices[3] = new Vector3(cx, cy, cz + 1);

        mesh.vertices = vertices;

        mesh.triangles = new int[] { 0, 1, 2,
                                     0, 1, 3,
                                     0, 2, 3,
                                     1, 2, 3 };

        GetComponent<MeshRenderer>().material = mat;

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
