using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Necesario si estás usando UI para mostrar la lista de muertes

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb2d;
    public GameObject corpsePrefab; // Prefab del cuerpo muerto
    public Transform respawnPoint; // Punto de reaparición
    public Text deathListText; // UI para mostrar la lista de muertes
    public GameObject projectilePrefab; // Prefab del proyectil
    public Transform firePoint; // Punto de disparo
    private Camera mainCam;


    private Vector2 moveInput;
    private List<Vector2> deathZones = new List<Vector2>();

    void Update()
    {
        // Movimiento del jugador
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();
        rb2d.velocity = moveInput * moveSpeed;

        // Disparo del jugador
        if (Input.GetButtonDown("Fire1"))
        {
            FireProjectile();
        }
    }

    public void Die()
    {
        // Guardar la posición de la muerte
        Vector2 deathPos = transform.position;
        deathZones.Add(deathPos);

        // Dejar el cuerpo en la zona
        Instantiate(corpsePrefab, deathPos, Quaternion.identity);

        // Reaparecer en el punto de inicio
        transform.position = respawnPoint.position;

        // Actualizar la lista de muertes
        UpdateDeathList();
    }

    void FireProjectile()
{
    if (firePoint != null && projectilePrefab != null)
    {
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = mousePos - firePoint.position;
        rb.velocity = new Vector2(direction.x, direction.y).normalized * (moveSpeed + 10f);

        // Rotación del proyectil
        float rot = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, rot - 90);
    }
    else
    {
        Debug.LogError("firePoint o projectilePrefab no está asignado.");
    }
}


    void UpdateDeathList()
    {
        if (deathListText != null)
        {
            deathListText.text = "";
            foreach (Vector2 deathZone in deathZones)
            {
                deathListText.text += "Morir en " + deathZone.ToString() + "\n";
            }
        }
    }

    public void ReviveCorpse(Vector2 position)
    {
        // Lógica para revivir el cuerpo anterior en la misma posición
        foreach (GameObject corpse in GameObject.FindGameObjectsWithTag("Corpse"))
        {
            if ((Vector2)corpse.transform.position == position)
            {
                corpse.SetActive(true);
                break;
            }
        }
    }


    public Vector2 GetMoveDirection()
    {
        return Vector2.zero;
    }
}