using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class RTSAgentManager : MonoBehaviour
{
    public float resourceModifier = 1;
    public float agentSpawnRadius = 20;
    public GameObject agentPrefab;
    public static Dictionary<GameObject, Agent> agentsObjs = new Dictionary<GameObject, Agent>();
    public float buildResources;

    private static List<Agent> agents = new List<Agent>();
    private static List<Agent> selected = new List<Agent>();
    private GameObject agentParent;
    private Vector3 destination;
    public float time = 0;

    public const float UPDATE_RATE = 0.0f;
    private const int PATHFINDING_FRAME_SKIP = 25;

    public bool spiralFlag = false;
    public bool flocking = true;

    public float CrowdFollow_Param = 0f;

    public Material selectedMaterial;
    public Material unselectedMaterial;
    public Text buildCount;

    private Vector3 mouseDownPosition = Vector3.zero;

    #region Unity Functions

    void Awake()
    {
        Random.InitState(0);
        buildResources = 10;

        agentParent = GameObject.Find("Agents");

        StartCoroutine(Run());
    }

    void Update()
    {
        #region Visualization
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (Input.mousePosition == mouseDownPosition)
            {
                var point = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                var dir = point - Camera.main.transform.position;
                Debug.DrawRay(point,dir,Color.red,2f);
                RaycastHit rcHit;
                if (Physics.Raycast(point, dir, out rcHit))
                {
                    if (rcHit.transform.tag == "Agent")
                    {
                        ChangeSelectAgent(rcHit.transform.gameObject);
                    }
                    else
                    {
                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(rcHit.point, out navHit, 5.0f, NavMesh.AllAreas))
                        {
                            destination = navHit.position;
                            SetAgentDestinations(destination);
                        }
                    }
                }
            }
        }
        if (Input.GetButtonUp("Fire1"))
        {
            if (buildResources > 0)
            {
                buildResources--;
                spawnAgent();
                //Debug.Log("Fire1 pressed");
            }
            else
            {
                //do something to alert the player
            }
        }
        time += Time.deltaTime;
        if (time >= 1)
        {
            time = 0;
            buildResources += resourceModifier;
            buildCount.text = "Resources: " + buildResources.ToString();
        }

        
#if UNITY_EDITOR
        if (Application.isFocused)
        {
            //UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }
    void spawnAgent()
    {
        
        var spawnPos = agentParent.transform.position + Random.onUnitSphere;
        spawnPos.y = 0;
        NavMeshHit hit;
        NavMesh.SamplePosition(spawnPos, out hit, 10, NavMesh.AllAreas);
        spawnPos = hit.position + Vector3.up;

        GameObject agent = null;
        agent = Instantiate(agentPrefab, spawnPos, Quaternion.identity);
        agent.name = "Agent " + agents.Count;
        agent.transform.parent = agentParent.transform;
        agent.tag = "Agent";
        var agentScript = agent.GetComponent<Agent>();
        agentScript.radius = 0.3f;// Random.Range(0.2f, 0.6f);
        agentScript.mass = 1;
        agentScript.perceptionRadius = 3;
        if (spiralFlag)
        {
            agentScript.spiralFlag = true;
        }
        if (!flocking)
        {
            agentScript.flocking = false;
        }
        agentScript.crowdFollowParam = CrowdFollow_Param;
        agents.Add(agentScript);
        agentsObjs.Add(agent, agentScript);
        selected.Add(agentScript);
    }
    IEnumerator Run()
    {
        yield return null;

        for (int iterations = 0; ; iterations++)
        {
            if (iterations % PATHFINDING_FRAME_SKIP == 0)
            {
                if (!spiralFlag)
                {
                    ComputeAgentPath();
                }
            }

            foreach (var agent in agents)
            {
                if (agent != null){
                    agent.ApplyForce();
                }
            }

            if (UPDATE_RATE == 0)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(UPDATE_RATE);
            }
        }
    }

    #endregion

    #region Public Functions

    public bool IsAgent(GameObject obj)
    {
        return agentsObjs.ContainsKey(obj);
    }

    public void SetAgentDestinations(Vector3 NewDestination)
    {
        destination = NewDestination;
        NavMeshHit hit;
        NavMesh.SamplePosition(NewDestination, out hit, 10, NavMesh.AllAreas);
        foreach (var agent in selected)
        {
            agent.SetDestination(hit.position);
        }
    }

    public void ComputeAgentPath()
    {
        foreach (var agent in agents)
        {
            agent.ComputePath();
        }
    }

    public void RemoveAgent(GameObject obj)
    {
        var agent = obj.GetComponent<Agent>();

        agents.Remove(agent);
        agentsObjs.Remove(obj);
    }

    public void SelectedAgents(Vector3 selection)
    {
        //Debug.Log(selection);
        Vector3 worldDownPosition = Camera.main.ScreenToWorldPoint(mouseDownPosition + Vector3.forward * 10);
        Vector3 worldUpPosition = Camera.main.ScreenToWorldPoint(selection + Vector3.forward * 10);
        Vector3 dir = worldDownPosition - Camera.main.transform.position;
        RaycastHit rcDownHit;
        RaycastHit rcUpHit;
        if (Physics.Raycast(worldDownPosition, dir, out rcDownHit))
        {
            dir = worldUpPosition - Camera.main.transform.position;
            if (Physics.Raycast(worldUpPosition, dir, out rcUpHit))
            {
                worldDownPosition = rcDownHit.point;
                worldUpPosition = rcUpHit.point;
                //Debug.Log("World Down Position:" + worldDownPosition);
                //Debug.Log("World Up Position:" + worldUpPosition);
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    //Debug.Log("Here");
                    foreach (Agent selectedAgent in agents)
                    {
                        if (selected.Contains(selectedAgent))
                        {
                            selected.Remove(selectedAgent);
                            selectedAgent.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = unselectedMaterial;
                        }
                    }
                }

                float xRight = worldUpPosition.x > worldDownPosition.x ? worldDownPosition.x : worldUpPosition.x;
                float xLeft = worldUpPosition.x > worldDownPosition.x ? worldUpPosition.x : worldDownPosition.x;

                float yUp = worldUpPosition.z > worldDownPosition.z ? worldDownPosition.z : worldUpPosition.z;
                float yDown = worldUpPosition.z > worldDownPosition.z ? worldUpPosition.z : worldDownPosition.z;
                //Debug.Log("xRight:" + xRight + " xLeft:" + xLeft + " yUp:" + yUp + " yDown:" + yDown);
                foreach (Agent agent in agents)
                {
                    Vector3 agentPosition = agent.transform.position;
                    //Debug.Log("Agent Position:" + agentPosition);
                    if (!selected.Contains(agent) && agentPosition.x >= xRight && agentPosition.x <= xLeft && agentPosition.z >= yUp && agentPosition.z <= yDown)
                    {
                        selected.Add(agent);
                        agent.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = selectedMaterial;
                        //agent.GetComponent<MeshRenderer>().material.shader = Shader.Find("UltimateOutline");
                    }
                }
            }

        }
    }
    #endregion

    #region Private Functions

    void ChangeSelectAgent(GameObject agent)
    {
        Agent agScript = agent.GetComponent<Agent>();
        if (selected.Contains(agScript))
        {
            selected.Remove(agScript);
            agent.GetComponent<MeshRenderer>().material = unselectedMaterial;
            
        }
        else
        {
            selected.Add(agScript);
            
            agent.GetComponent<MeshRenderer>().material = selectedMaterial;
        }
    }

    #endregion

    #region Visualization Functions

    #endregion

    #region Utility Classes

    private class Tuple<K, V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v)
        {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
