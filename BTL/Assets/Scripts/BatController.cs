using UnityEngine;

public class BatController : MonoBehaviour
{
    public int maxHealth = 20;
    public float moveSpeed = 3f;
    public float detectionRange = 14f;
    public float maxChaseDistanceFromHome = 8f;
    public float returnHomeStopDistance = 0.1f;
    public float attackRange = 1.2f;
    public int attackDamage = 10;
    public float attackCooldown = 1f;
    public string idleStateName = "Bat";
    public string attackClipName = "BatAttack";
    public float attackAnimationDuration = 0.2f;
    public string deathClipName = "BatDie";
    public float deathAnimationDuration = 0.2f;
    public float hurtFlashDuration = 0.12f;
    public float lungeDuration = 0.08f;
    public float retreatDuration = 0.12f;
    public AudioClip projectileHitSound;
    [Range(0f, 1f)] public float projectileHitSoundVolume = 1f;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    PlayerController playerController;
    Transform playerTarget;
    float attackCooldownTimer;
    Coroutine attackCoroutine;
    Coroutine deathCoroutine;
    Coroutine hurtCoroutine;
    bool isAttacking;
    bool isDead;
    bool isReturningHome;
    Vector2 homePosition;
    Color startColor;
    int currentHealth;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        homePosition = transform.position;
        currentHealth = maxHealth;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }
    }

    void Start()
    {
        FindPlayer();
        CacheAnimationDurations();
    }

    void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        if (isAttacking || isDead)
        {
            return;
        }

        Vector2 currentPosition = rb.position;
        float distanceFromHome = Vector2.Distance(currentPosition, homePosition);

        if (isReturningHome)
        {
            if (distanceFromHome <= returnHomeStopDistance)
            {
                isReturningHome = false;
            }
            else
            {
                FaceDirection(homePosition - currentPosition);
                return;
            }
        }

        if (distanceFromHome >= maxChaseDistanceFromHome)
        {
            isReturningHome = true;
            FaceDirection(homePosition - currentPosition);
            return;
        }

        Vector2 toPlayer = playerTarget.position - transform.position;
        float distanceToPlayer = toPlayer.magnitude;
        if (distanceToPlayer > detectionRange)
        {
            if (distanceFromHome > returnHomeStopDistance)
            {
                isReturningHome = true;
                FaceDirection(homePosition - currentPosition);
            }
            return;
        }

        FaceDirection(toPlayer);

        if (distanceToPlayer <= attackRange)
        {
            TryAttack();
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null || isAttacking || isDead)
        {
            return;
        }

        Vector2 currentPosition = rb.position;
        float distanceFromHome = Vector2.Distance(currentPosition, homePosition);

        if (isReturningHome || distanceFromHome >= maxChaseDistanceFromHome)
        {
            isReturningHome = true;
            Vector2 returnPosition = Vector2.MoveTowards(currentPosition, homePosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(returnPosition);
            return;
        }

        Vector2 targetPosition = playerTarget.position;
        Vector2 toPlayer = targetPosition - currentPosition;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > detectionRange)
        {
            if (distanceFromHome > returnHomeStopDistance)
            {
                isReturningHome = true;
                Vector2 returnPosition = Vector2.MoveTowards(currentPosition, homePosition, moveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(returnPosition);
            }
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            return;
        }

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);
    }

    void FindPlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            return;
        }

        playerTarget = playerController.transform;
    }

    void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
        transform.localScale = scale;
    }

    void TryAttack()
    {
        if (attackCooldownTimer > 0f || playerController == null)
        {
            return;
        }

        attackCooldownTimer = attackCooldown;
        playerController.changeHealth(-attackDamage);

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AttackSequence());
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0 || isDead)
        {
            return;
        }

        PlayProjectileHitSound();
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        FlashWhenHit();
    }

    void PlayProjectileHitSound()
    {
        if (projectileHitSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(projectileHitSound, projectileHitSoundVolume);
    }

    void CacheAnimationDurations()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null)
            {
                continue;
            }

            if (clip.name == attackClipName)
            {
                attackAnimationDuration = clip.length;
            }
            else if (clip.name == deathClipName)
            {
                deathAnimationDuration = clip.length;
            }
        }
    }

    System.Collections.IEnumerator AttackSequence()
    {
        isAttacking = true;

        Vector2 startPosition = rb.position;
        Vector2 attackTargetPosition = playerTarget != null ? (Vector2)playerTarget.position : startPosition;

        FaceDirection(attackTargetPosition - startPosition);

        if (animator != null)
        {
            animator.SetTrigger("isAttack");
        }

        float clampedLungeDuration = Mathf.Max(0.01f, lungeDuration);
        float remainingAttackTime = Mathf.Max(0f, attackAnimationDuration - clampedLungeDuration);
        float clampedRetreatDuration = Mathf.Max(0.01f, retreatDuration);

        yield return MoveToPosition(attackTargetPosition, clampedLungeDuration);

        if (remainingAttackTime > 0f)
        {
            yield return new WaitForSeconds(remainingAttackTime);
        }

        if (animator != null)
        {
            animator.CrossFadeInFixedTime(idleStateName, 0.05f);
        }

        yield return MoveToPosition(startPosition, clampedRetreatDuration);

        isAttacking = false;
        attackCoroutine = null;
    }

    System.Collections.IEnumerator MoveToPosition(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = rb.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Vector2 nextPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            FaceDirection(targetPosition - rb.position);
            rb.MovePosition(nextPosition);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPosition);
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        EnemyKillBlockUnlocker.ReportBatKilled();

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }

        isAttacking = false;
        isReturningHome = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (animator != null)
        {
            animator.SetBool("isDeath", true);
        }

        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        deathCoroutine = StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    void FlashWhenHit()
    {
        if (spriteRenderer == null || hurtFlashDuration <= 0f)
        {
            return;
        }

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
        }

        hurtCoroutine = StartCoroutine(HurtFlashSequence());
    }

    System.Collections.IEnumerator HurtFlashSequence()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hurtFlashDuration);
        spriteRenderer.color = startColor;
        hurtCoroutine = null;
    }
}
