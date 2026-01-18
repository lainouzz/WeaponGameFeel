using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private ITarget target;
    private NavMeshAgent agent;
    
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float updateDestinationInterval = 0.2f; // How often to update path

    private float nextDestinationUpdate;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed * 100f; // NavMesh uses degrees/sec
        }
    }

    public void SetTarget(ITarget newTarget)
    {
        target = newTarget;
    }

    private void Update()
    {
        if (target == null || !target.IsAlive) return;

        // Use NavMeshAgent if available
        if (agent != null && agent.enabled)
        {
            // Update destination periodically (not every frame for performance)
            if (Time.time >= nextDestinationUpdate)
            {
                agent.SetDestination(target.position);
                nextDestinationUpdate = Time.time + updateDestinationInterval;
            }

            // Smoothly rotate to face target (NavMesh handles movement)
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0f; // Keep rotation on horizontal plane
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Fallback: simple movement without NavMesh
            Vector3 direction = (target.position - transform.position).normalized;
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }
            
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}
