using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float sensitivity = 2f;
    public float camSpeed = -0.5f;
    public RTSAgentManager agentManager;
    public Texture2D selectionHighLight = null;

    static Rect selection = new Rect(0, 0, 0, 0);
    Vector3 startSelected = Vector3.zero;
    // Update is called once per frame
    void LateUpdate()
    {
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
        transform.position = transform.position + Vector3.ProjectOnPlane(transform.forward, Vector3.up) * Input.GetAxis("Vertical") *moveSpeed*Time.deltaTime;
        transform.position = transform.position + Vector3.ProjectOnPlane(transform.right, Vector3.up) * Input.GetAxis("Horizontal") *moveSpeed*Time.deltaTime;
        //float x = Input.GetAxis("Mouse X");
        //float y = Input.GetAxis("Mouse Y");
        //Vector3 rotateValue = new Vector3(y, x * -1, 0);
        //transform.eulerAngles = transform.eulerAngles - rotateValue;
        //transform.eulerAngles += rotateValue * camSpeed;
        transform.position = transform.position + transform.up * Input.mouseScrollDelta.y * moveSpeed * Time.deltaTime;
        StartSelection();
    }

    void StartSelection(){
        if(Input.GetMouseButtonDown(0)){
            startSelected = Input.mousePosition;
        }else if(Input.GetMouseButtonUp(0)){
            if(startSelected != Input.mousePosition){
                agentManager.SelectedAgents(Input.mousePosition);
            }
            startSelected = Vector3.zero;
        }
        HandleSelection();
    }

    void HandleSelection(){
        if(Input.GetMouseButton(0)){
            selection = new Rect(startSelected.x, Screen.height - startSelected.y,
                Input.mousePosition.x - startSelected.x, (Screen.height - Input.mousePosition.y) - (Screen.height - startSelected.y));
            if(selection.width < 0){
                selection.x += selection.width;
                selection.width = -selection.width;
            }
            if(selection.height < 0){
                selection.y += selection.height;
                selection.height = -selection.height;
            }
        }
    }

    void OnGUI(){
        if(startSelected != Vector3.zero){
            GUI.DrawTexture(selection, selectionHighLight);
        }
    }
}
