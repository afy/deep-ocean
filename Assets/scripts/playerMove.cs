using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// attatches to playerContainer
public class playerMove : MonoBehaviour
{
    public float rotSens;
    public float moveSpeed;

    public GameObject mainCamera;

    void Update() {
        handleInput();
    }

    void handleInput() {
        // rotate
        if (Input.GetKey(KeyCode.RightArrow))
        {
            mainCamera.transform.RotateAround(transform.position, transform.up, Time.deltaTime * rotSens);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            mainCamera.transform.RotateAround(transform.position, transform.up, Time.deltaTime * -rotSens);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            mainCamera.transform.RotateAround(transform.position, transform.right, Time.deltaTime * -rotSens);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            mainCamera.transform.RotateAround(transform.position, transform.right, Time.deltaTime * rotSens);
        }

        // move
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += Vector3.forward * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= Vector3.forward * Time.deltaTime * moveSpeed;
        }

        // strafe
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += Vector3.right * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= Vector3.right * Time.deltaTime * moveSpeed;
        }

        // up/down
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += Vector3.up * Time.deltaTime * moveSpeed;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.position -= Vector3.up * Time.deltaTime * moveSpeed;
        }
    }
}
