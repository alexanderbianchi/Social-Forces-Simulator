using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentController : MonoBehaviour
{
    Animator anim;
    NavMeshAgent agent;
    NavMeshObstacle agentObstacle;
    public AgentManager manager;
    bool IsRotating = false;
    bool IsJumping = false;
    bool ActivateAgent = false;
    Vector3 newTargetPosition = Vector3.zero;
    Quaternion lookRotation;
    bool sprint;
    float rot, speed;
    float baseSpeed = 1.550f;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agentObstacle = GetComponent<NavMeshObstacle>();
        agentObstacle.enabled = false; 
        // Don’t update position automatically
        agent.updatePosition = false;
        agent.autoTraverseOffMeshLink = false;
        speed = baseSpeed;
    }

    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;


    void Update()
    {
        if(ActivateAgent){
            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(transform.position, transform.position, NavMesh.AllAreas, path);
            if(path.corners.Length > 0){
                ActivateAgent = false;
                agent.enabled = true;
                agent.avoidancePriority = 50;
                agent.SetDestination(newTargetPosition);

                agent.isStopped = true;
                IsRotating = true;
                agent.updatePosition = false;
                agent.autoTraverseOffMeshLink = false;
            }
            
        }else if(agent.isOnOffMeshLink && !IsJumping){
            IsJumping = true;
            anim.SetBool("leap",true);
        }else if(IsJumping){
            OffMeshLinkData data = agent.currentOffMeshLinkData;

            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
            
            transform.position = Vector3.MoveTowards(transform.position, endPos, 2.0f * Time.deltaTime);
            
            if(transform.position == endPos)
            {
                agent.CompleteOffMeshLink();
                IsJumping = false;
                anim.SetBool("leap", false);
            }
        }else if(!IsRotating && agent.enabled){
            MoveAnimation();
        }else if(agent.enabled){
            Vector3 direction = (agent.steeringTarget - transform.position).normalized;
            lookRotation = Quaternion.LookRotation(direction);
            
            Quaternion oldRotation = transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * 100.0f);
            float angle = Quaternion.Angle( oldRotation, transform.rotation);
            
            anim.SetFloat("sides", angle);
            if (Quaternion.Angle(transform.rotation, lookRotation) < 2.5f)
            {
                IsRotating = false;
                agent.isStopped = false;
            }
        }
    }

    void MoveAnimation(){
        Vector3 worldDeltaPosition = agent.destination - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        if(velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius -2){
            rot = Mathf.Deg2Rad * Vector2.Angle(transform.position,deltaPosition);
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Forwards")) rot = 0.0f;
                // Update animation parameters
            anim.SetFloat("sides", rot);
            anim.SetFloat("speed", speed);

            agent.speed = speed;
        }else{
            anim.SetFloat("sides", 0f);
            anim.SetFloat("speed", 0f);
        }

        if (agent.enabled)
        {
            if (PathFindingComplete())
            {
                anim.CrossFade("idle", 1f);
                anim.SetFloat("sides", 0f);
                anim.SetFloat("speed", 0f);
                agent.avoidancePriority = 30;
                agent.enabled = false;
                agentObstacle.enabled = true;
            }
        }
    }

    bool PathFindingComplete(){
        double r = 2;
        float distance = Vector3.Distance(agent.destination, agent.transform.position);
        
        if (manager)
        {
            r += manager.arrivedAtDestination * (manager.arrivedAtDestination) * 30;
        }
        if (distance <= (agent.stoppingDistance + r))
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.05f)
            {
                if (manager)
                {
                    manager.arrivedAtDestination += 1;
                    if (manager.arrivedAtDestination == manager.selectedCount)
                    {
                        manager.arrivedAtDestination = 0;
                        agent.stoppingDistance = 0.6f;
                    }
                }
                return true;
            }
        }
        return false;
    }

    public void OnSelected(){
        gameObject.transform.GetChild(7).gameObject.SetActive(true);
    }
    public void OnDeselect(){
        gameObject.transform.GetChild(7).gameObject.SetActive(false);
    }

    public void SetAgentDestination(Vector3 targetPosition){
        ActivateAgent = true;
        newTargetPosition = targetPosition;
        agentObstacle.enabled = false;
    }

    private void OnAnimatorMove()
    {
        if(!IsJumping && !IsRotating)
            transform.position = agent.nextPosition;
    }

    public void OnSpeedSliderChange(float val)
    {
        speed = (1 + val * 2) * baseSpeed;
    }
}
