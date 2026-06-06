using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float maxTravelDistance = 5f;
    public int damageAmount = 20;
    public Vector2 spriteForwardDirection = Vector2.down;

    Rigidbody2D rigidbody2D;
    Vector2 startPosition;

    void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

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
        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector2.down;
        }

        direction.Normalize();
        startPosition = rigidbody2D.position;
        RotateAlongDirection(direction);

        rigidbody2D.AddForce(direction * force);
    }

    void RotateAlongDirection(Vector2 direction)
    {
        Vector2 forward = spriteForwardDirection.sqrMagnitude > 0.001f
            ? spriteForwardDirection.normalized
            : Vector2.down;

        float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float spriteForwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, directionAngle - spriteForwardAngle);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.changeHealth(-damageAmount);
        }

        Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.changeHealth(-damageAmount);
        }

        Destroy(gameObject);
    }
}
