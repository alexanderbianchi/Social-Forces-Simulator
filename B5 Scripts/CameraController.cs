using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float sensitivity = 2f;
    public float camSpeed = -0.5f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        transform.position = transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up) * Input.GetAxis("Vertical") *moveSpeed*Time.deltaTime;
        transform.position = transform.position + Vector3.ProjectOnPlane(transform.right, Vector3.up) * Input.GetAxis("Horizontal") *moveSpeed*Time.deltaTime;
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
        Vector3 rotateValue = new Vector3(y, x * -1, 0);
        transform.eulerAngles = transform.eulerAngles - rotateValue;
        transform.eulerAngles += rotateValue * camSpeed;
        int vertical = Input.GetKey("left shift") ? 1 : (Input.GetKey("left ctrl") ? -1 : 0);
        transform.position = transform.position + transform.up * vertical * moveSpeed * Time.deltaTime;
    }
}
