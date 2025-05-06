using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyNodePathing : MonoBehaviour
{
    private Vector3? lastKnownPlayerPosition = null;
    private float timeSinceLastSeen = 0f;
    public float chaseMemoryDuration = 5f;

    public Transform nodeParent;
    public float moveSpeed = 3f;
    public float reachThreshold = 0.1f;
    public float proximityBiasRange = 10f;
    public float rotationSpeed = 5f;

    private List<Transform> nodes = new List<Transform>();
    private int currentNodeIndex = 0;

    private Queue<int> recentHistory = new Queue<int>();
    private int historyLimit = 4;

    [Header("Vision Settings")]
    public float viewDistance = 10f;
    [Range(0, 360)]
    public float fieldOfView = 90f;
    public Transform target;
    public LayerMask obstructionMask;

    private bool canSeePlayer = false;

    [Header("Obstacle Avoidance Settings")]
    public float obstacleDetectionDistance = 2f;
    public float avoidStrength = 3f;

    void Start()
    {
        if (nodeParent != null)
        {
            nodes.AddRange(nodeParent.GetComponentsInChildren<Transform>());
            nodes.Remove(nodeParent);

            if (nodes.Count > 0)
            {
                currentNodeIndex = Random.Range(0, nodes.Count);
                transform.position = nodes[currentNodeIndex].position;
                AddToHistory(currentNodeIndex);
            }
        }
    }

    void Update()
    {
        float adjustedMoveSpeed = moveSpeed;

        if (target != null && CanSeeTarget(target))
        {
            canSeePlayer = true;
            timeSinceLastSeen = 0f;
            lastKnownPlayerPosition = target.position;

            adjustedMoveSpeed = moveSpeed * 1.5f;

            Vector3 directionToPlayer = (target.position - transform.position).normalized;
            RotateTowardsTarget(target.position);
            MoveWithObstacleAvoidance(directionToPlayer, adjustedMoveSpeed);

            Debug.Log("Chasing Player");
            return;
        }
        else
        {
            if (canSeePlayer)
            {
                canSeePlayer = false;
                timeSinceLastSeen = 0f;
            }
        }

        if (lastKnownPlayerPosition.HasValue && timeSinceLastSeen < chaseMemoryDuration)
        {
            timeSinceLastSeen += Time.deltaTime;
            Vector3 directionToLastKnown = (lastKnownPlayerPosition.Value - transform.position).normalized;

            RotateTowardsTarget(lastKnownPlayerPosition.Value);
            MoveWithObstacleAvoidance(directionToLastKnown, moveSpeed);

            if (Vector3.Distance(transform.position, lastKnownPlayerPosition.Value) < reachThreshold)
            {
                lastKnownPlayerPosition = null;
            }

            Debug.Log("Chasing to last known position");
            return;
        }

        if (nodes == null || nodes.Count == 0) return;

        Transform targetNode = nodes[currentNodeIndex];
        RotateTowardsTarget(targetNode.position);

        Vector3 direction = (targetNode.position - transform.position).normalized;
        MoveWithObstacleAvoidance(direction, moveSpeed);

        if (Vector3.Distance(transform.position, targetNode.position) < reachThreshold)
        {
            int newNodeIndex = GetBiasedNextNodeIndex();
            currentNodeIndex = newNodeIndex;
            AddToHistory(currentNodeIndex);
        }
    }

    void MoveWithObstacleAvoidance(Vector3 moveDirection, float speed)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 forward = moveDirection.normalized;

        if (Physics.Raycast(origin, forward, out RaycastHit hit, obstacleDetectionDistance, obstructionMask))
        {
            Debug.DrawRay(origin, forward * obstacleDetectionDistance, Color.red);

            // Find nearest visible node (like in your OnCollisionEnter)
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 dirToNode = nodes[i].position - transform.position;
                float dist = dirToNode.magnitude;
                float angle = Vector3.Angle(transform.forward, dirToNode);

                if (dist <= viewDistance && angle <= fieldOfView / 2f)
                {
                    if (!Physics.Raycast(transform.position, dirToNode.normalized, dist, obstructionMask))
                    {
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestIndex = i;
                        }
                    }
                }
            }

            if (bestIndex != -1)
            {
                currentNodeIndex = bestIndex;
                AddToHistory(currentNodeIndex);

                // Rotate toward that node
                RotateTowardsTarget(nodes[bestIndex].position);
            }
            else
            {
                // Can't find any visible nodes — maybe just rotate away
                transform.Rotate(0, 180f, 0);
            }

            return;
        }
        else
        {
            Debug.DrawRay(origin, forward * obstacleDetectionDistance, Color.green);
            transform.position += forward * speed * Time.deltaTime;
        }
    }
    void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    void GoToNearestNode()
    {
        if (nodes == null || nodes.Count == 0) return;

        float closestDistance = float.MaxValue;
        int closestIndex = currentNodeIndex;

        for (int i = 0; i < nodes.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, nodes[i].position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }

        currentNodeIndex = closestIndex;
    }

    int GetBiasedNextNodeIndex()
    {
        Transform currentNode = nodes[currentNodeIndex];
        List<int> nearbyIndices = new List<int>();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (recentHistory.Contains(i)) continue;
            float dist = Vector3.Distance(currentNode.position, nodes[i].position);
            if (dist <= proximityBiasRange)
            {
                nearbyIndices.Add(i);
            }
        }

        if (nearbyIndices.Count > 0)
        {
            return nearbyIndices[Random.Range(0, nearbyIndices.Count)];
        }
        else
        {
            List<int> allOtherNodes = new List<int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (!recentHistory.Contains(i))
                    allOtherNodes.Add(i);
            }

            return allOtherNodes.Count > 0 ? allOtherNodes[Random.Range(0, allOtherNodes.Count)] : currentNodeIndex;
        }
    }

    void AddToHistory(int index)
    {
        recentHistory.Enqueue(index);
        if (recentHistory.Count > historyLimit)
        {
            recentHistory.Dequeue();
        }
    }

    bool CanSeeTarget(Transform target)
    {
        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        Debug.DrawRay(transform.position, directionToTarget, Color.green, 21f);

        if (distanceToTarget <= viewDistance && angleToTarget <= fieldOfView / 2f)
        {
            Debug.Log("Target is within the field of view cone!");

            if (!Physics.Raycast(transform.position, directionToTarget.normalized, distanceToTarget, obstructionMask))
            {
                Debug.Log("Target visible in FOV");
                return true;
            }
            else
            {
                Debug.Log("Raycast hit obstruction");
            }
        }
        else
        {
            Debug.Log("Target is outside the field of view cone.");
        }

        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene("EndMenu");
             Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; // Ensure it's unlocked
        }
        if (collision.collider.CompareTag("Obstacle"))
        {
            Debug.Log("Collided with obstacle — rotating 180° and finding nearest visible node");

            // Rotate 180 degrees
            transform.Rotate(0, 90, 0);

            // Find the nearest visible node within FOV
            int bestIndex = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < nodes.Count; i++)
            {
                Vector3 dirToNode = nodes[i].position - transform.position;
                float dist = dirToNode.magnitude;
                float angle = Vector3.Angle(transform.forward, dirToNode);

                if (dist <= viewDistance && angle <= fieldOfView / 2f)
                {
                    if (!Physics.Raycast(transform.position, dirToNode.normalized, dist, obstructionMask))
                    {
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestIndex = i;
                        }
                    }
                }
            }

            if (bestIndex != -1)
            {
                currentNodeIndex = bestIndex;
                AddToHistory(currentNodeIndex);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (nodeParent != null && nodes.Count > 0)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            foreach (Transform node in nodes)
            {
                Gizmos.DrawWireSphere(node.position, proximityBiasRange);
            }
        }

        Gizmos.color = Color.yellow;
        Vector3 leftRay = Quaternion.Euler(0, -fieldOfView / 2, 0) * transform.forward * viewDistance;
        Vector3 rightRay = Quaternion.Euler(0, fieldOfView / 2, 0) * transform.forward * viewDistance;

        Gizmos.DrawRay(transform.position, leftRay);
        Gizmos.DrawRay(transform.position, rightRay);
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        if (target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            float angleToTarget = Vector3.Angle(transform.forward, toTarget);

            if (toTarget.magnitude <= viewDistance && angleToTarget <= fieldOfView / 2f)
            {
                if (!Physics.Raycast(transform.position, toTarget.normalized, toTarget.magnitude, obstructionMask))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, target.position);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, target.position);
                }
            }
        }
    }
}
