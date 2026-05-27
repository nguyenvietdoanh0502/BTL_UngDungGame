using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction moveAction;
    public float speed = 5f;
    public int maxHealth=5;
    int currentHealth;
    public int getCurrentHealth()
    {
        return currentHealth;
    }
    public float timeInvincible = 2f;
    bool isInvincible;
    float damageCoolDown;
    SpriteRenderer spriteRenderer;
    public float blinkInterval = 0.1f;
    public float blinkAlpha = 0.35f;
    float blinkTimer;
    bool blinkState = true;
    Vector2 move;
    Rigidbody2D rigidbody2D;
    Animator animator;
    public GameObject projectilePrefab;
    public InputAction LaunchAction;
    public float launchCooldown = 0.5f;
    public float launchDelay = 0.55f;
    float launchCooldownTimer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        moveAction.Enable();
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        LaunchAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        move = moveAction.ReadValue<Vector2>();
        if (move != Vector2.zero)
        {
            animator.SetBool("isRunning", true);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }
        if (move.x > 0) // Đang di chuyển sang phải
        {
            // Set scale X về 1 (giữ nguyên hướng gốc)
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (move.x < 0) // Đang di chuyển sang trái
        {
            // Set scale X về -1 (quay ngược lại theo chiều ngang)
            transform.localScale = new Vector3(-1, 1, 1);
        }
        if (isInvincible)
        {
            damageCoolDown-=Time.deltaTime;
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                blinkState = !blinkState;
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = blinkState ? 1f : blinkAlpha;
                    spriteRenderer.color = color;
                }
                blinkTimer = blinkInterval;
            }
            if (damageCoolDown < 0)
            {
                isInvincible = false;
                blinkState = true;
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = 1f;
                    spriteRenderer.color = color;
                }
            }
        }
        if (launchCooldownTimer > 0f)
        {
            launchCooldownTimer -= Time.deltaTime;
        }
        if (LaunchAction.WasPressedThisFrame() && launchCooldownTimer <= 0f)
        {
            launchCooldownTimer = launchCooldown;
            StartCoroutine(LaunchAfterDelay());
            animator.SetTrigger("isAttack");
            animator.SetFloat("random",(int)Random.Range(0,2));
        }
    }
    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2D.position + move*speed*Time.deltaTime;
        rigidbody2D.position = position;
    }
    public void changeHealth(int amount)
    {
        if (amount < 0)
        {
            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCoolDown = timeInvincible;
            blinkTimer = 0f;
            blinkState = true;
        }
    }
    IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);
        Launch();
    }
    void Launch()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || Mouse.current == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector2 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        Vector2 launchDirection = mouseWorldPosition - rigidbody2D.position;
        if (launchDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }
        launchDirection.Normalize();

        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2D.position + launchDirection * 0.5f, Quaternion.identity);
        Projectile projectile= projectileObject.GetComponent<Projectile>();
        projectile.Launch(launchDirection,1000);
    }
}
