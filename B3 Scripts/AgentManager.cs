using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.AI;
using UnityEngine.UI;

public class AgentManager : MonoBehaviour
{
    public List<GameObject> agents;
    public Slider speedSlider;
    public int selectedCount;
    public int arrivedAtDestination;

    // Start is called before the first frame update
    void Start()
    {
        arrivedAtDestination = 0;
        agents = new List<GameObject>();
        GameObject[] newAgents = GameObject.FindGameObjectsWithTag("Agent");
        foreach(GameObject newAgent in newAgents){
            agents.Add(newAgent);
        }
        selectedCount = agents.Count;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckMousePosition();
        }
        if(Input.GetMouseButtonDown(1)){
            CheckValidPosition();
        }
    }

    void CheckMousePosition()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            NavMeshHit navHit;
            if (hit.collider.gameObject.CompareTag("Agent"))
            {
                if (!agents.Contains(hit.collider.gameObject))
                {
                    Debug.Log("Added Agent");
                    agents.Add(hit.collider.gameObject);
                    selectedCount += 1;
                    hit.collider.gameObject.GetComponent<AgentController>().OnSelected();
                }
                else
                {
                    Debug.Log("Removed Agent");
                    selectedCount -= 1;
                    agents.Remove(hit.collider.gameObject);
                    hit.collider.gameObject.GetComponent<AgentController>().OnDeselect();
                }
            }

        }
    }

    void CheckValidPosition(){
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 1.0f, NavMesh.AllAreas))
            {
                foreach (GameObject agent in agents)
                {
                    Debug.Log(navHit.position);
                    NavMeshAgent nav = agent.GetComponent<NavMeshAgent>();
                    Vector3 dist = nav.destination - navHit.position;
                    agent.GetComponent<AgentController>().SetAgentDestination(navHit.position);
                }
            }
        }
    }

    public void OnSliderChange(){
        float val = speedSlider.value;
        foreach(GameObject agent in agents){
            agent.GetComponent<AgentController>().OnSpeedSliderChange(val);
        }
    }
}
