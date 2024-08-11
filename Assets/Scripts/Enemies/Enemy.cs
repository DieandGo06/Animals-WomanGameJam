using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;


//Movimiento de IA sacado de: https://www.youtube.com/watch?v=HRX0pUSucW4
public class Enemy : MonoBehaviour
{
    private enum State { Patrolling, Idle, Chase };

    [SerializeField] private State state;
    [SerializeField] Transform target;

    [Header("Field of View")]
    public bool canSeePlayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask obstrucLayer;
    float viewRadius = 5;
    float viewAngle = 75f;

    [Header("Patrullaje")]
    [SerializeField] int patrolPointIndex;
    [SerializeField] List<Vector2> patrolPoints = new List<Vector2>();
    NavMeshAgent agent;




    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        patrolPointIndex = patrolPointIndex >= patrolPoints.Count ? 0 : patrolPointIndex;
    }

    void Update()
    {

        switch (state)
        {
            case State.Patrolling:
                Patrol();
                StartCoroutine(CheckPatrolPoint());
                break;

            case State.Chase:
                agent.SetDestination(target.position);
                break;
        }
        StartCoroutine(POVRoutine());
        Rotate();
    }

    private void OnDrawGizmos()
    {
        DrawPatrollingPoints();
        DrawViewSphere();
        DrawRangeView();
    }


    void Rotate()
    {
        Vector3 moveDirection = transform.localPosition + agent.velocity.normalized;
        float angle = Matematicas.RadianesEntre(transform.position, moveDirection) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    //-------------------------------------------------------------------------------------------------------------------
    #region Patrullaje
    void Patrol()
    {
        if (patrolPoints.Count > 0)
        {
            agent.SetDestination(patrolPoints[patrolPointIndex]);
        }
    }

    IEnumerator CheckPatrolPoint()
    {
        yield return new WaitForSeconds(0.2f);

        if (patrolPoints.Count > 0)
        {
            if (Vector2.Distance(transform.position, patrolPoints[patrolPointIndex]) <= 0.5f)
            {
                patrolPointIndex++;
                if (patrolPointIndex == patrolPoints.Count) patrolPointIndex = 0;
            }
        }
    }

    void DrawPatrollingPoints()
    {
        if (patrolPoints.Count > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (Vector2 point in patrolPoints)
            {
                Gizmos.DrawWireSphere(point, 0.5f);
            }
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------------------------------


    //-------------------------------------------------------------------------------------------------------------------
    #region POV View
    //Codigo sacado de https://www.youtube.com/watch?v=OQ1dRX5NyM0
    IEnumerator POVRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.2f);
            FieldViewCheck();
        }
    }

    void FieldViewCheck()
    {
        Collider2D[] rangeCheck = Physics2D.OverlapCircleAll(transform.position, viewRadius, playerLayer);

        //Si el personaje esta dentro del rango circular de vision
        if (rangeCheck.Length > 0)
        {
            Transform _target = rangeCheck[0].transform;
            Vector2 directionToTarget = (_target.position - transform.position).normalized;

            //Si el personaje  esta dentro del rango traingular de vision
            if (Vector2.Angle(transform.up, directionToTarget) < viewAngle / 2)
            {
                //Si tiene vision en linea recta con el personaje
                float distanceToTarget = Vector2.Distance(transform.position, _target.position);
                if (!Physics2D.Raycast(transform.position, directionToTarget, distanceToTarget, obstrucLayer))
                {
                    canSeePlayer = true;
                    state = State.Chase;
                    target = target == null ? _target : target;
                    Debug.DrawLine(transform.position, _target.position, Color.red);
                }
                else canSeePlayer = false;
            }
            else canSeePlayer = false;
        }
        else canSeePlayer = false;
    }

    void DrawRangeView()
    {
        Gizmos.color = Color.yellow;
        Vector3 angle1 = DirectionFromAngle(-transform.eulerAngles.z, -viewAngle / 2);
        Vector3 angle2 = DirectionFromAngle(-transform.eulerAngles.z, viewAngle / 2);
        Gizmos.DrawLine(transform.position, transform.position + angle1 * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + angle2 * viewRadius);
    }

    private Vector2 DirectionFromAngle(float eulerY, float degrees)
    {
        degrees += eulerY;
        return new Vector2(Mathf.Sin(degrees * Mathf.Deg2Rad), Mathf.Cos(degrees * Mathf.Deg2Rad));
    }

    void DrawViewSphere()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
    #endregion
    //-------------------------------------------------------------------------------------------------------------------

}
