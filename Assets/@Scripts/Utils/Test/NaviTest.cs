using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.UI;

public class NaviTest : MonoBehaviour
{
    NavMeshAgent _navMeshAgent;

    private void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();

        _navMeshAgent.SetDestination(Vector3.zero);
    }

    private void Update()
    {
        transform.position = new Vector3(transform.position.x,0,transform.position.z);
    }
}
