using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class LeaderAgent : MonoBehaviour
{
    public float radius;
    public float mass;
    public float perceptionRadius;
    public float goalWeight = 5;
    public float goalForceSpeed;
    
    float A = 2.0f;
    float B = 0.6f;
    float k = 1.2f;
    float kappa = 2.4f;

    private List<Vector3> path;
    private NavMeshAgent nma;
    private Rigidbody rb;
    private SphereCollider sc;
    private HashSet<GameObject> perceivedNeighbors = new HashSet<GameObject>();
    private HashSet<GameObject> perceivedWalls = new HashSet<GameObject>();

    void Start()
    {
        path = new List<Vector3>();
        nma = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        sc = GetComponent<SphereCollider>();

        gameObject.transform.localScale = new Vector3(2 * radius, 1, 2 * radius);
        nma.radius = radius;
        rb.mass = mass;
        GetComponent<SphereCollider>().radius = perceptionRadius / 2;
    }

    private void Update()
    {
        if (path.Count > 0 && Vector3.Distance(transform.position, path[0]) < 1.1f)
        {
            path.RemoveAt(0);
        }
    }

    #region Public Functions

    public void ComputePath(Vector3 destination)
    {
        nma.enabled = true;
        var nmPath = new NavMeshPath();
        nma.CalculatePath(destination, nmPath);
        path = nmPath.corners.Skip(1).ToList();
        goalForceSpeed = nma.speed;
        nma.enabled = false;
    }

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    #endregion

    #region Incomplete Functions

    private Vector3 ComputeForce()
    {
        Vector3 gForce = CalculateGoalForce();
        
        Vector3 wForce = CalculateWallForce();

        Debug.DrawLine(transform.position, transform.position + gForce, Color.red, 0.5f);
        Debug.DrawLine(transform.position, transform.position + wForce, Color.blue, 0.5f);

        return gForce + wForce;
    }

    private Vector3 CalculateGoalForce()
    {
        // calculate the path to the destination and take the first two corners.
        // return the unit vector in that direction and multiplied by a constant scalar.
        if(path.Count == 0)
            return Vector3.zero - GetVelocity();
        Vector3 ei_0 = (new Vector3(path[0].x, transform.position.y, path[0].z) - transform.position).normalized;
        //Return the (Vi_0 * ei_0) - Vi
        return ((goalForceSpeed * ei_0) - GetVelocity());
    }
    /*
    private Vector3 CalculateAgentForce()
    {
        Vector3 agentForce = Vector3.zero;
        foreach (GameObject neighbor in perceivedNeighbors)
        {
            Vector3 repForce = Vector3.zero;
            Vector3 slideForce = Vector3.zero;
            Vector3 opos = neighbor.transform.position;
            Agent oagent = neighbor.GetComponent<Agent>();
            
            float r_ij = oagent.radius + radius;
            float d_ij = Vector3.Distance(opos,transform.position);
            float g = (r_ij - d_ij > 0.000001) ? (r_ij- d_ij) : 0;
            Vector3 n_ij = (transform.position - opos)/d_ij;
            Vector3 t_ij = new Vector3(-n_ij.z, n_ij.x, 0);
            float deltaV_ji = Vector3.Dot((oagent.GetVelocity() - GetVelocity()), t_ij);

            repForce = (A * Mathf.Exp((r_ij - d_ij)/B) + kappa*g) * n_ij;
            slideForce = k * g * deltaV_ji * t_ij;

            agentForce += (repForce + slideForce);
        }

        return agentForce;
    }
    */
    private Vector3 CalculateWallForce()
    {
        
        // Forces on the agent will be perpendicular to the walls closest surface
        Vector3 WallForce = Vector3.zero;
        foreach (GameObject Wall in perceivedWalls)
        {
            Vector3 repForce = Vector3.zero;
            Vector3 slideForce = Vector3.zero;

            float r_i = radius;
            float d_iw = Vector3.Distance(Wall.transform.position, transform.position);
            float g = (r_i - d_iw > 0.000001) ? (r_i - d_iw) : 0;
            Vector3 n_iw = Vector3.zero;

            Vector3 direction = (Wall.transform.position - rb.transform.position).normalized;
            float dotEast = Vector3.Dot(Vector3.right, direction);
            float dotNorth = Vector3.Dot(Vector3.forward, direction);
            if (Mathf.Abs(dotEast) > Mathf.Abs(dotNorth))
            {
                // Wall is east/west of Agent
                if(dotEast < 0)
                    n_iw = Wall.transform.right; // Will be negative if west, positive if east //The inverse by dividing by a number means the force gets stronger as the agent is closer to the wall
                else
                    n_iw = -Wall.transform.right;
            }
            else
            {
                // Wall is north/south of Agent
                if(dotNorth < 0)
                    n_iw = Wall.transform.forward;
                else
                    n_iw = -Wall.transform.forward;
                // Will be negative if south, positive if North
            }
            Vector3 t_iw = new Vector3(-n_iw.z, n_iw.x, 0);
            
            repForce = (A * Mathf.Exp((r_i - d_iw)/B) + kappa*g) * n_iw;
            slideForce = k * g * Vector3.Dot(GetVelocity(), t_iw) * t_iw;
            WallForce += (repForce - slideForce);
        }
        return WallForce;
    }

    /*
    //OnTriggerExit is not called if the GameObject is deactivated or destroyed.
    //Function will clean up perceivedNeighbors that were deactivated while still in range of Agent.
    private void CleanNeighbors(){
        HashSet<GameObject> excludeNeighbors = new HashSet<GameObject>();
        foreach(GameObject neighbor in perceivedNeighbors){
            if(!neighbor.activeSelf)
                excludeNeighbors.Add(neighbor);
        }
        perceivedNeighbors.ExceptWith(excludeNeighbors);
    }
    */
    public void ApplyForce()
    {
        var force = ComputeForce();
        force.y = 0;
        rb.AddForce(force * 10, ForceMode.Force);
    }

    public void OnTriggerEnter(Collider other)
    {   /*
        if(AgentManager.IsAgent(other.gameObject)){
            perceivedNeighbors.Add(other.gameObject);
        } else*/ 
        if(WallManager.IsWall(other.gameObject)){
            perceivedWalls.Add(other.gameObject);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {   /*
        if(AgentManager.IsAgent(other.gameObject)){
            perceivedNeighbors.Remove(other.gameObject);
        } else */
        if(WallManager.IsWall(other.gameObject)){
            perceivedWalls.Remove(other.gameObject);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        /*
        if(WallManager.IsWall(collision.transform.gameObject)){
            perceivedWalls.Add(collision.transform.gameObject);
        }*/
    }

    public void OnCollisionExit(Collision collision)
    {
        /*
        if(WallManager.IsWall(collision.transform.gameObject)){
            perceivedWalls.Remove(collision.transform.gameObject);
        }
        */
    }

    #endregion
}
