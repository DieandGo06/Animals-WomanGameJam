using UnityEngine;
using UnityEngine.AI;


//Movimiento de IA sacado de: https://www.youtube.com/watch?v=HRX0pUSucW4
public class NewEnemy : MonoBehaviour
{
    [SerializeField] Transform target;
    NavMeshAgent agent;




    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    void Update()
    {
        agent.SetDestination(target.position);
    }
}
