using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NavMeshAgent))]
public class UserMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    public float maxtimer = 5;
    private float timer = 0;

    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        Patroling();
    }

    private void Patroling()
    {
        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if(timer > maxtimer && walkPointSet)
        {
            timer = 0;
            walkPointSet = false;
        }

        if (distanceToWalkPoint.magnitude < 0.3f)
            walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
        {

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, walkPoint, NavMesh.AllAreas, path))
            {
                walkPointSet = true;
            }
            else
            {
                walkPointSet = false;
            }
        }
    }
    

    // Update is called once per frame
    void Update()
    {
        Patroling();

        timer += Time.deltaTime;
    }
}
