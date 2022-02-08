using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class LFAgentManager : MonoBehaviour
{
    public int agentCount = 10;
    public float agentSpawnRadius = 20;
    public GameObject agentPrefab;
    public GameObject leaderPrefab;
    public static Dictionary<GameObject, FollowerAgent> agentsObjs = new Dictionary<GameObject, FollowerAgent>();

    private LeaderAgent leader;
    private static List<FollowerAgent> agents = new List<FollowerAgent>();
    private GameObject agentParent;
    private Vector3 destination;

    public const float UPDATE_RATE = 0.0f;
    private const int PATHFINDING_FRAME_SKIP = 25;

    public bool spiralFlag = false;
    public float CrowdFollow_Param = 0f;

    #region Unity Functions

    void Awake()
    {
        Random.InitState(0);

        agentParent = GameObject.Find("Agents");
        for (int i = 0; i < agentCount; i++)
        {
            var randPos = new Vector3((Random.value - 0.5f) * agentSpawnRadius, 0, (Random.value - 0.5f) * agentSpawnRadius);
            NavMeshHit hit;
            NavMesh.SamplePosition(randPos, out hit, 10, NavMesh.AllAreas);
            randPos = hit.position + Vector3.up;

            GameObject agent = null;
            agent = Instantiate(agentPrefab, randPos, Quaternion.identity);
            agent.name = "Agent " + i;
            agent.transform.parent = agentParent.transform;
            var agentScript = agent.GetComponent<FollowerAgent>();
            agentScript.radius = 0.3f;// Random.Range(0.2f, 0.6f);
            agentScript.mass = 1;
            agentScript.perceptionRadius = 3;
            agent.SetActive(true);
            agents.Add(agentScript);
            agentsObjs.Add(agent, agentScript);
        }

        leader = leaderPrefab.GetComponent<LeaderAgent>();

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization
        
        if (Input.GetMouseButtonDown(0))
        {
            if (true)
            {
                var point = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                var dir = point - Camera.main.transform.position;
                RaycastHit rcHit;
                if (Physics.Raycast(point, dir, out rcHit))
                {
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(rcHit.point, out navHit, 1.0f, NavMesh.AllAreas))
                    {
                        SetLeaderDestination(navHit.position);
                    }
                }
            } else
            {
                var randPos = new Vector3((Random.value - 0.5f) * agentSpawnRadius, 0, (Random.value - 0.5f) * agentSpawnRadius);

                NavMeshHit hit;
                NavMesh.SamplePosition(randPos, out hit, 1.0f, NavMesh.AllAreas);
                print(hit.position);
                Debug.DrawLine(hit.position, hit.position + Vector3.up * 10, Color.red, 1000000);
                foreach (var agent in agents)
                {
                    //agent.ComputePath(hit.position);
                }
            }
        }

#if UNITY_EDITOR
        if (Application.isFocused)
        {
            //UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
        }
#endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        for (int iterations = 0; ; iterations++)
        {
            if (iterations % PATHFINDING_FRAME_SKIP == 0)
            {
                SetAgentDestinations(leaderPrefab.transform.position);
            }
            
            foreach (var agent in agents)
            {
                agent.ApplyForce();
            }

            leader.ApplyForce();

            if (UPDATE_RATE == 0)
            {
                yield return null;
            } else
            {
                yield return new WaitForSeconds(UPDATE_RATE);
            }
        }
    }

    #endregion

    #region Public Functions

    public static bool IsAgent(GameObject obj)
    {
        return agentsObjs.ContainsKey(obj);
    }

    public void SetLeaderDestination(Vector3 NewDestination){
        NavMeshHit hit;
        NavMesh.SamplePosition(NewDestination, out hit, 10, NavMesh.AllAreas);
        leader.ComputePath(hit.position);
    }

    public void SetAgentDestinations(Vector3 NewDestination)
    {
        destination = NewDestination;
        NavMeshHit hit;
        NavMesh.SamplePosition(NewDestination, out hit, 10, NavMesh.AllAreas);
        foreach (var agent in agents)
        {
            agent.ComputePath(hit.position);
        }
        
    }

    public static void RemoveAgent(GameObject obj)
    {
        var agent = obj.GetComponent<FollowerAgent>();

        agents.Remove(agent);
        agentsObjs.Remove(obj);
    }
    #endregion

    #region Private Functions

    #endregion

    #region Visualization Functions

    #endregion

    #region Utility Classes

    private class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
