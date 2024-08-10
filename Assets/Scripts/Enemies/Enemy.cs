using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.TextCore.Text;



[RequireComponent(typeof(MoveController))]
public class Enemy : MonoBehaviour
{
    //-------------------------------------------------------------------------------------
    private enum State { idle, moving, death };

    #region Inspector
    //-------------------------------------------------------------------------------------
    [Header("Estados")]
    [SerializeField] private State state;

    //-------------------------------------------------------------------------------------
    [Header("Propiedades")]
    [SerializeField] private int initialHelth;
    [SerializeField] int health;
    public int damage;

    [Header("IA de movimiento")]
    [SerializeField] LayerMask obstacleLayer;

    #endregion

    #region Privadas
    //Componentes
    //-------------------------------------------------------------------------------------
    private IAMove iaMove;
    private Rigidbody2D rb;
    //private Animator animator;
    private SpriteRenderer spriteRenderer;

    private MoveController moveController;
    private RaycastHit2D[] weaponRayCast = new RaycastHit2D[1];
    #endregion




    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //animator = GetComponent<Animator>();
        moveController = GetComponent<MoveController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        iaMove = GetComponentInChildren<IAMove>();
        if (health == 0) health = 100;
        initialHelth = health;
    }



    private void OnEnable()
    {
        Reset();
        if (initialHelth == 0) Debug.LogWarning("la vida incial de " + name + " no puede ser 0");
    }



    void Update()
    {
        //if (target != null)
        //{
        //    CalculateMoveDirection();
        //}

        //if (health > 0)
        //{
        //    if (moveController.GetMoveDirection() == Vector2.zero)
        //    {
        //        state = State.idle;
        //    }
        //    else if (moveController.GetMoveDirection() != Vector2.zero)
        //    {
        //        state = State.moving;
        //    }
        //}
        //else state = State.death;
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.idle:
                rb.velocity = Vector2.zero;
                //animator.SetBool("isRunning", false);
                //animator.SetBool("isStanding", true);
                break;

            case State.moving:
                CalculateMoveDirection();
                moveController.Move();
                //animator.SetBool("isRunning", true);
                //animator.SetBool("isStanding", false);
                break;

            case State.death:
                rb.velocity = Vector2.zero;
                //animator.SetBool("isDead", true);
                StartCoroutine(Death());
                break;
        }
    }

    private void OnDrawGizmos()
    {

    }





    //----------------------------------------------------------------------------------------------------------
    #region Movimiento 
    void CalculateMoveDirection()
    {
        //FindPath();

        iaMove.Chase();
        // MoveDirection debe ser normalizada para que la velocidad no cambie si va horizontal o diagonal, por ello se usa Vector2.zero
        //float radianEnemyCharacter = Matematicas.RadianesEntre(transform.position, moveController.target.position);
        //moveController.SetMoveDirection(Matematicas.PolaresToRectangulares(1, radianEnemyCharacter, Vector2.zero));
        //Debug.DrawRay(transform.position, moveController.GetMoveDirection());
    }

    void FindPath()
    {
        float maxDistance = 2;
        Debug.DrawRay(transform.position, moveController.GetMoveDirection() * maxDistance);
        if (Physics2D.RaycastNonAlloc(transform.position, moveController.GetMoveDirection(), weaponRayCast, maxDistance, obstacleLayer) != 0)
        {
            float radianToTarget = Matematicas.RadianesEntre(transform.position, moveController.target.position);
        }
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------


    //----------------------------------------------------------------------------------------------------------
    #region Ciclo de vida 
    private void Reset()
    {
        //state = State.idle;
        health = initialHelth;
        //animator.SetBool("isDead", false);
        //animator.SetBool("isRunning", false);
        //animator.SetBool("isStanding", true);
        moveController.ResumeMoving();
        StopAllCoroutines();
    }

    public void Die()
    {
        health = 0;
        state = State.death;
        //animator.SetBool("isRunning", false);
        //animator.SetBool("isStanding", false);
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(1.5f);
        gameObject.SetActive(false);
    }
    #endregion
    //----------------------------------------------------------------------------------------------------------



    public IEnumerator Stunned(float duration)
    {
        moveController.StopMoving();
        yield return new WaitForSeconds(duration);
        moveController.ResumeMoving();
    }




}
