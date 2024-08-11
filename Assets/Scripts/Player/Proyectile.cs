using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed; // Velocidad del proyectil
    public Rigidbody2D rb; // Rigidbody2D del proyectil

    private Vector2 moveDirection;

    void Start()
    {
        // Verifica si el Rigidbody2D no está asignado
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        // Establece la dirección del proyectil basado en la rotación o dirección del disparo
        moveDirection = transform.up; // Asume que el proyectil se mueve en la dirección en que está "mirando"
        rb.velocity = moveDirection * speed;
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Enemy") || hitInfo.CompareTag("Player"))
        {
            Destroy(hitInfo.gameObject); // Destruye al enemigo o al jugador
            Destroy(gameObject); // Destruye el proyectil
        }
    }
}
