using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float maxTravelDistance = 5f;
    public int damageAmount = 5;

    Rigidbody2D rigidbody2D;
    Vector2 startPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 currentPosition = transform.position;
        if ((currentPosition - startPosition).sqrMagnitude >= maxTravelDistance * maxTravelDistance)
        {
            Destroy(gameObject);
        }
    }
    public void Launch(Vector2 direction, float force)
    {
        startPosition = rigidbody2D.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        rigidbody2D.AddForce(direction*force);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        BatController bat = collision.GetComponent<BatController>();
        if (bat != null)
        {
            bat.TakeDamage(damageAmount);
            Destroy(gameObject);
            return;
        }

        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.Fix();
        }
        Destroy(gameObject);
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject);
    }
}
