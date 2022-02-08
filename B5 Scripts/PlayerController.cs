using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private bool foundExit = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnTriggerEnter(Collider other){
        Debug.Log(other.tag);
        if(other.tag == "Exit"){
            Debug.Log("Exit found!");
            foundExit = true;
        }
    }

    public bool IsExitFound(){
        return foundExit;
    }
}
