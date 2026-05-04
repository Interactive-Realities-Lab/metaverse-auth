using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UserMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;
    public float maxtimer = 5f;
    private float timer = 0f;

    public Vector3 walkPoint;
    private bool walkPointSet;
    public float walkPointRange = 10f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        Patroling();
    }

    private void Update()
    {
        Patroling();
        timer += Time.deltaTime;
    }

    private void Patroling()
    {
        if (!walkPointSet)
            SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        if (timer > maxtimer && walkPointSet)
        {
            timer = 0f;
            walkPointSet = false;
        }

        if (distanceToWalkPoint.magnitude < 0.3f)
        {
            walkPointSet = false;
            timer = 0f;
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        Vector3 candidatePoint = new Vector3(
            transform.position.x + randomX,
            transform.position.y,
            transform.position.z + randomZ
        );

        if (Physics.Raycast(candidatePoint + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, whatIsGround))
        {
            NavMeshPath path = new NavMeshPath();

            bool hasPath = NavMesh.CalculatePath(transform.position, hit.point, NavMesh.AllAreas, path);

            if (hasPath && path.status == NavMeshPathStatus.PathComplete)
            {
                walkPoint = hit.point;
                walkPointSet = true;
                timer = 0f;
            }
            else
            {
                walkPointSet = false;
            }
        }
    }
}