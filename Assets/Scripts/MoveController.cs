using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class MoveController : MonoBehaviour
{

    [SerializeField, Range(50f, 500f)] private float movementSpeed;
    public float speedMultiplier = 1;
    [SerializeField, Space(3)] private bool isFacingRight;
    
    private Rigidbody2D rb;
    private Vector2 moveDirection;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        speedMultiplier = 1;
    }


    public void Move()
    {
        rb.velocity = moveDirection * (movementSpeed * speedMultiplier * Time.fixedDeltaTime);
        Flip();
    }

    public Vector2 GetMoveDirection()
    {
        return moveDirection;
    }
    public void SetMoveDirection(Vector2 newDirection)
    {
        moveDirection = newDirection.normalized;
    }
    public float GetMovementSpeed()
    {
        return movementSpeed;
    }
    public void SetMovementSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;
    }

    void Flip()
    {
        if (moveDirection.x > 0 && !isFacingRight || moveDirection.x < 0 && isFacingRight)
        {
            isFacingRight = !isFacingRight;
            Vector3 currentScale = transform.localScale;
            currentScale.x = currentScale.x * -1;
            transform.localScale = currentScale;
        }
    }

    public void StopMoving()
    {
        speedMultiplier = 0;
    }

    public void ResumeMoving()
    {
        speedMultiplier = 1;
    }

}
