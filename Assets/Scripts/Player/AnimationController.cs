using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    public enum Direction { straight, up, down }
    [SerializeField] Direction direction;
    //[SerializeField] Transform arms;
    [SerializeField] Transform body;
    //[SerializeField] Animator legs;

    Animator animator;
    SpriteRenderer armsRenderer;
    PlayerController playerController;
    //MoveController moveController;


    void Start()
    {
        //moveController = GetComponent<MoveController>();
        //armsRenderer = arms.GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }


    void SetDirection()
    {
        Vector2 moveDir = playerController.GetMoveDirection();
        if (moveDir == Vector2.up)
            direction = Direction.up;
        else if (moveDir == Vector2.right || moveDir == Vector2.left)
            direction = Direction.straight;
        else if (moveDir == Vector2.down)
            direction = Direction.down;
    }

    public void Idle()
    {
        //SetArmDirection();
        //legs.SetBool("isRunning", false);
        animator.SetBool("isRunning", false);
    }

    public void Run()
    {
        //if (moveController != null)
        //{
        //    //SetArmDirection();
        //}

        switch (direction)
        {
            case Direction.up:
                animator.SetInteger("angle", 90);
                break;
            //case Direction.tiltUp:
            //    animator.SetInteger("angle", 45);
            //    break;
            case Direction.straight:
                animator.SetInteger("angle", 0);
                break;
            //case Direction.tiltDown:
            //    animator.SetInteger("angle", -45);
            //    break;
            case Direction.down:
                animator.SetInteger("angle", -90);
                break;
        }
        //legs.SetBool("isRunning", true);
        animator.SetBool("isRunning", true);
    }
}
