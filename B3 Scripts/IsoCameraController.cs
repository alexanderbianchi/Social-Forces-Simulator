using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsoCameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float sensitivity = 2f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        
        transform.position = transform.position + Vector3.right * Input.GetAxis("Horizontal")*moveSpeed*Time.deltaTime;
        transform.position = transform.position + Vector3.forward * Input.GetAxis("Vertical")*moveSpeed*Time.deltaTime;
        transform.position = transform.position + transform.forward * Input.mouseScrollDelta.y*moveSpeed*Time.deltaTime;
        //transform.position = transform.position - transform.up*Input.GetAxis("Shift")*moveSpeed*Time.deltaTime;
        /*
        float rotX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivity;
        float rotY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * sensitivity;
        transform.localEulerAngles = new Vector3(rotY, rotX, 0f);
        /*Cursor.lockState = UnityEngine.CursorLockMode.Locked;

        if (Input.GetKey(KeyCode.Escape))
            Cursor.lockState = UnityEngine.CursorLockMode.None;
        else
            Cursor.lockState = UnityEngine.CursorLockMode.Locked;
        */
        /*
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            RaycastHit hit;
            Ray ray = this.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                //whatever
            }
        }
        */
    }
}
