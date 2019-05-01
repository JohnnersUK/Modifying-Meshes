using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public GameObject cross;
    public float camSpeed;
    public float moveSpeed;

    private Vector3 center;

    private float pitch;
    private float yaw;

    // Start is called before the first frame update
    void Start()
    {
        center = new Vector3(0, 0, 0);
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        cross.transform.position = Input.mousePosition;

        if (Input.GetMouseButton(1))
        {
            cross.transform.position = center;

            yaw += camSpeed * Input.GetAxis("Mouse X");
            pitch -= camSpeed * Input.GetAxis("Mouse Y");

            transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        }

        if (Input.GetKey(KeyCode.W))
        {
            Vector3 tempMove = transform.forward * moveSpeed;
            tempMove.y = 0;

            transform.position += tempMove;
        }

        if (Input.GetKey(KeyCode.S))
        {
            Vector3 tempMove = -transform.forward * moveSpeed;
            tempMove.y = 0;

            transform.position += tempMove;
        }

        if (Input.GetKey(KeyCode.A))
        {
            Vector3 tempMove = -transform.right * moveSpeed;
            tempMove.y = 0;

            transform.position += tempMove;
        }

        if (Input.GetKey(KeyCode.D))
        {
            Vector3 tempMove = transform.right * moveSpeed;
            tempMove.y = 0;

            transform.position += tempMove;
        }
    }
}
