using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    public float radius;
    public float mass;
    public float perceptionRadius;
    public float goalWeight = 5;
    public float goalForceSpeed;
    public float agentCount = 0;
    public bool spiralFlag = false;
    public bool flocking = true;
    public Vector3 velocmon;
    public float crowdFollowParam = 0f;

    private Vector3 ei_0 = Vector3.zero;
    
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

        if (spiralFlag)
        {
            computeSpiral();
        }
    }
    
    private void computeSpiral()
    {
        Vector3 apos = this.transform.position;
        Vector3 apnext = apos*1.00008f;
        transform.RotateAround(apnext, Vector3.up, 60.0f);
        velocmon = apnext;

        this.ComputePath(apnext);

    }

    private void Update()
    {
        if (spiralFlag)
        {
            if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
            {
                computeSpiral();
                
            }
        }
        else { 
            if (path.Count > 1 && Vector3.Distance(transform.position, path[0]) < 1.1f)
            {
                path.RemoveAt(0);
            } else if (path.Count == 1 && Vector3.Distance(transform.position, path[0]) < 2f)
            {
                path.RemoveAt(0);

                if (path.Count == 0)
                {
                    gameObject.SetActive(false);
                    AgentManager.RemoveAgent(gameObject);
                }
            }
        }
        #region Visualization

        if (false)
        {
            if (path.Count > 0)
            {
                Debug.DrawLine(transform.position, path[0], Color.green);
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], Color.yellow);
            }
        }

        if (false)
        {
            foreach (var neighbor in perceivedNeighbors)
            {
                Debug.DrawLine(transform.position, neighbor.transform.position, Color.yellow);
            }
        }

        #endregion
    }

    #region Public Functions

    public void ComputePath(Vector3 destination)
    {
        nma.enabled = true;
        var nmPath = new NavMeshPath();
        nma.CalculatePath(destination, nmPath);
        if(spiralFlag){
            path = new List<Vector3>() { destination };
        }else{
            path = nmPath.corners.Skip(1).ToList();
        }
        //nma.SetDestination(destination);
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
        Vector3 flockForce = Vector3.zero;
        if (flocking){
            flockForce = CalculateFlockForce();
        }
        Vector3 gForce = CalculateGoalForce();
        
        CleanNeighbors();
        Vector3 aForce = CalculateAgentForce();
        
        Vector3 wForce = CalculateWallForce();

        Debug.DrawLine(transform.position, transform.position + gForce, Color.red, 0.5f);
        Debug.DrawLine(transform.position, transform.position + aForce, Color.green, 0.5f);
        Debug.DrawLine(transform.position, transform.position + wForce, Color.blue, 0.5f);

        return gForce + aForce + wForce + flockForce;
        /*
        var force = Vector3.zero;
        
        if (force != Vector3.zero)
        {
            return force.normalized * Mathf.Min(force.magnitude, Parameters.maxSpeed);
        } else
        {
            return Vector3.zero;
        }
        */
    }

    private Vector3 CalculateFlockForce()
    {
        Vector3 Alighnment = Vector3.zero;
        Vector3 Seperation = Vector3.zero;
        Vector3 Cohesion = Vector3.zero;
        foreach (GameObject neighbor in perceivedNeighbors)
        {
            Alighnment += neighbor.GetComponent<Agent>().GetLastDirection();
            Cohesion += neighbor.transform.position;
            Seperation += neighbor.transform.position - rb.position;
        }
        Cohesion /= perceivedNeighbors.Count();
        Alighnment /= perceivedNeighbors.Count();
        Seperation /= perceivedNeighbors.Count();
        Seperation *= -1;
        return Alighnment.normalized + Seperation.normalized + (Cohesion - rb.position).normalized;
    }
    private Vector3 NeighborGoalForce(){
        Vector3 avg_e = Vector3.zero;
        if(perceivedNeighbors.Count == 0)
            return avg_e;
        foreach(GameObject neighbor in perceivedNeighbors)
        {
            avg_e = neighbor.GetComponent<Agent>().GetLastDirection();
        }
        avg_e = avg_e * (1 / perceivedNeighbors.Count);
        return avg_e;
    }

    private Vector3 CalculateGoalForce()
    {
        // calculate the path to the destination and take the first two corners.
        // return the unit vector in that direction and multiplied by a constant scalar.
        ei_0 = (new Vector3(path[0].x, transform.position.y, path[0].z) - transform.position).normalized;
        if(crowdFollowParam > 0){
            Vector3 neighbor_e = NeighborGoalForce();
            ei_0 = ((1 - crowdFollowParam) * ei_0 + crowdFollowParam * neighbor_e).normalized;
        }
        //Return the (Vi_0 * ei_0) - Vi
        return ((goalForceSpeed * ei_0) - GetVelocity());
    }

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
                    n_iw = Wall.transform.right;
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

    public void ApplyForce()
    {
        var force = ComputeForce();
        force.y = 0;
        rb.AddForce(force * 10, ForceMode.Force);
    }

    public Vector3 GetLastDirection(){
        return ei_0;
    }

    public void OnTriggerEnter(Collider other)
    {
        if(AgentManager.IsAgent(other.gameObject)){
            perceivedNeighbors.Add(other.gameObject);
        } else if(WallManager.IsWall(other.gameObject)){
            perceivedWalls.Add(other.gameObject);
        }
    }
    
    public void OnTriggerExit(Collider other)
    {
        if(AgentManager.IsAgent(other.gameObject)){
            perceivedNeighbors.Remove(other.gameObject);
        } else if(WallManager.IsWall(other.gameObject)){
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
