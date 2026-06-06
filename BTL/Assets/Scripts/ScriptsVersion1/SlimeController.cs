using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeController : MonoBehaviour
{
    const string RunningParameter = "isRunning";
    const string AttackParameter = "isAttack";
    const string DeathParameter = "isDeath";

    [Header("Stats")]
    public int maxHealth = 40;
    public int attackDamage = 15;

    [Header("Movement")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 2.5f;
    public float patrolDistance = 3f;
    public float detectionRange = 6f;
    public float maxChaseDistanceFromHome = 8f;
    public float returnHomeStopDistance = 0.1f;

    [Header("Attack")]
    public float attackRange = 0.55f;
    public float attackCooldown = 1.2f;
    public float attackDamageDelay = 0.2f;
    public float attackAnimationDuration = 0.45f;
    public float attackHitPadding = 0.25f;

    [Header("Animation")]
    public string idleStateName = "SlimeIdle";
    public string runStateName = "SlimeRun";
    public string attackStateName = "SlimeAttack";
    public string hurtStateName = "SlimeHurt";
    public string deathStateName = "SlimeDie";
    public float hurtFlashDuration = 0.12f;
    public float deathAnimationDuration = 0.4f;

    [Header("Physics")]
    public bool preventPushingPlayer = true;

    [Header("Sound")]
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackSoundVolume = 1f;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitSoundVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    PlayerController playerController;
    Transform playerTarget;
    Coroutine attackCoroutine;
    Coroutine hurtCoroutine;
    Vector2 homePosition;
    Color startColor;
    float startScaleX;
    float attackCooldownTimer;
    int idleStateHash;
    int runStateHash;
    int attackStateHash;
    int hurtStateHash;
    int deathStateHash;
    int currentHealth;
    int patrolDirection = 1;
    bool isAttacking;
    bool isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        ConfigureNoPushPhysics();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        homePosition = transform.position;
        startScaleX = Mathf.Abs(transform.localScale.x);
        currentHealth = maxHealth;
        CacheAnimationStateHashes();

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

    void OnValidate()
    {
        CacheAnimationStateHashes();
        ConfigureNoPushPhysics();
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (playerTarget == null)
        {
            FindPlayer();
        }
    }

    void FixedUpdate()
    {
        if (isDead || isAttacking)
        {
            SetMoving(false);
            return;
        }

        Vector2 currentPosition = rb.position;
        float distanceFromHome = Vector2.Distance(currentPosition, homePosition);

        if (ShouldChasePlayer(currentPosition, distanceFromHome, out Vector2 playerPosition, out float distanceToPlayer))
        {
            FaceDirection(playerPosition - currentPosition);

            if (distanceToPlayer <= attackRange)
            {
                SetMoving(attackCooldownTimer > 0f);
                TryAttack();
                return;
            }

            MoveToward(playerPosition, chaseSpeed);
            SetMoving(true);
            return;
        }

        if (distanceFromHome > patrolDistance + returnHomeStopDistance)
        {
            FaceDirection(homePosition - currentPosition);
            MoveToward(homePosition, chaseSpeed);
            SetMoving(true);
            return;
        }

        Patrol();
    }

    bool ShouldChasePlayer(Vector2 currentPosition, float distanceFromHome, out Vector2 playerPosition, out float distanceToPlayer)
    {
        playerPosition = currentPosition;
        distanceToPlayer = float.MaxValue;

        if (playerTarget == null || playerController == null)
        {
            return false;
        }

        playerPosition = playerTarget.position;
        distanceToPlayer = Vector2.Distance(currentPosition, playerPosition);
        return distanceToPlayer <= detectionRange && distanceFromHome <= maxChaseDistanceFromHome;
    }

    void Patrol()
    {
        if (patrolDistance <= 0f || patrolSpeed <= 0f)
        {
            SetMoving(false);
            return;
        }

        Vector2 currentPosition = rb.position;
        float leftLimit = homePosition.x - patrolDistance;
        float rightLimit = homePosition.x + patrolDistance;

        if (patrolDirection > 0 && currentPosition.x >= rightLimit)
        {
            patrolDirection = -1;
        }
        else if (patrolDirection < 0 && currentPosition.x <= leftLimit)
        {
            patrolDirection = 1;
        }

        Vector2 patrolTarget = new Vector2(homePosition.x + patrolDirection * patrolDistance, homePosition.y);
        FaceDirection(Vector2.right * patrolDirection);
        MoveToward(patrolTarget, patrolSpeed);
        SetMoving(true);
    }

    void MoveToward(Vector2 targetPosition, float speed)
    {
        Vector2 nextPosition = Vector2.MoveTowards(rb.position, targetPosition, speed * Time.fixedDeltaTime);
        rb.MovePosition(nextPosition);
    }

    void ConfigureNoPushPhysics()
    {
        if (!preventPushingPlayer)
        {
            return;
        }

        Rigidbody2D body = rb != null ? rb : GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D slimeCollider in colliders)
        {
            slimeCollider.isTrigger = false;
        }

        IgnorePlayerCollisions();
    }

    void IgnorePlayerCollisions()
    {
        if (!preventPushingPlayer || playerController == null)
        {
            return;
        }

        Collider2D[] slimeColliders = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] playerColliders = playerController.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D slimeCollider in slimeColliders)
        {
            if (slimeCollider == null)
            {
                continue;
            }

            foreach (Collider2D playerCollider in playerColliders)
            {
                if (playerCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(slimeCollider, playerCollider, true);
            }
        }
    }

    void FindPlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            playerTarget = null;
            return;
        }

        playerTarget = playerController.transform;
        IgnorePlayerCollisions();
    }

    void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float sign = Mathf.Sign(direction.x);
        scale.x = (startScaleX > 0f ? startScaleX : Mathf.Abs(scale.x)) * sign;
        transform.localScale = scale;
    }

    void TryAttack()
    {
        if (attackCooldownTimer > 0f || playerController == null || playerTarget == null)
        {
            return;
        }

        attackCooldownTimer = attackCooldown;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        attackCoroutine = StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        SetMoving(false);
        FaceDirection((Vector2)playerTarget.position - rb.position);
        PlayAttackAnimation();
        PlayAttackSound();

        yield return new WaitForSeconds(Mathf.Max(0f, attackDamageDelay));

        if (!isDead && playerController != null && playerTarget != null)
        {
            float distanceToPlayer = Vector2.Distance(rb.position, playerTarget.position);
            if (distanceToPlayer <= attackRange + attackHitPadding)
            {
                playerController.changeHealth(-attackDamage);
            }
        }

        float remainingAttackTime = Mathf.Max(0f, attackAnimationDuration - attackDamageDelay);
        if (remainingAttackTime > 0f)
        {
            yield return new WaitForSeconds(remainingAttackTime);
        }

        isAttacking = false;
        PlayRunAnimation();
        attackCoroutine = null;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        PlayHitSound();
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        PlayHurtAnimation();
        FlashWhenHit();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        EnemyKillBlockUnlocker.ReportSlimeKilled();

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        SetMoving(false);
        PlayDeathAnimation();
        PlayDeathSound();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        float destroyDelay = deathAnimationDuration;
        if (deathSound != null)
        {
            destroyDelay = Mathf.Max(destroyDelay, deathSound.length);
        }

        Destroy(gameObject, destroyDelay);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeProjectileDamage(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeProjectileDamage(collision.gameObject);
    }

    bool TryTakeProjectileDamage(GameObject source)
    {
        if (isDead || source == null)
        {
            return false;
        }

        Projectile projectile = source.GetComponent<Projectile>();
        if (projectile == null)
        {
            return false;
        }

        TakeDamage(projectile.damageAmount);
        Destroy(source);
        return true;
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

            if (clip.name == attackStateName)
            {
                attackAnimationDuration = clip.length;
            }
            else if (clip.name == deathStateName)
            {
                deathAnimationDuration = clip.length;
            }
        }
    }

    void CacheAnimationStateHashes()
    {
        idleStateHash = HashBaseLayerState(idleStateName);
        runStateHash = HashBaseLayerState(runStateName);
        attackStateHash = HashBaseLayerState(attackStateName);
        hurtStateHash = HashBaseLayerState(hurtStateName);
        deathStateHash = HashBaseLayerState(deathStateName);
    }

    int HashBaseLayerState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            return 0;
        }

        return Animator.StringToHash("Base Layer." + stateName);
    }

    void SetMoving(bool moving)
    {
        if (TrySetBool(RunningParameter, moving))
        {
            return;
        }

        TryPlayState(moving ? runStateName : idleStateName);
    }

    void PlayAttackAnimation()
    {
        if (TrySetTrigger(AttackParameter))
        {
            return;
        }

        TryPlayState(attackStateName);
    }

    void PlayRunAnimation()
    {
        TrySetBool(RunningParameter, true);

        if (!TryPlayState(runStateName))
        {
            TryPlayState(idleStateName);
        }
    }

    void PlayHurtAnimation()
    {
        TryPlayState(hurtStateName);
    }

    void PlayDeathAnimation()
    {
        TrySetBool(DeathParameter, true);
        TryPlayState(deathStateName);
    }

    void PlayHitSound()
    {
        if (hitSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(hitSound, hitSoundVolume);
    }

    void PlayAttackSound()
    {
        if (attackSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(attackSound, attackSoundVolume);
    }

    void PlayDeathSound()
    {
        if (deathSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(deathSound, deathSoundVolume);
    }

    bool TrySetBool(string parameterName, bool value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            return false;
        }

        animator.SetBool(parameterName, value);
        return true;
    }

    bool TrySetTrigger(string parameterName)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            return false;
        }

        animator.SetTrigger(parameterName);
        return true;
    }

    bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    bool TryPlayState(string stateName)
    {
        int stateHash = GetCachedStateHash(stateName);
        if (animator == null || stateHash == 0)
        {
            return false;
        }

        if (!animator.HasState(0, stateHash))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(stateName))
        {
            animator.CrossFadeInFixedTime(stateHash, 0.05f, 0);
        }

        return true;
    }

    int GetCachedStateHash(string stateName)
    {
        if (stateName == idleStateName)
        {
            return idleStateHash;
        }

        if (stateName == runStateName)
        {
            return runStateHash;
        }

        if (stateName == attackStateName)
        {
            return attackStateHash;
        }

        if (stateName == hurtStateName)
        {
            return hurtStateHash;
        }

        if (stateName == deathStateName)
        {
            return deathStateHash;
        }

        return 0;
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

    IEnumerator HurtFlashSequence()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(hurtFlashDuration);
        spriteRenderer.color = startColor;
        hurtCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)homePosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, maxChaseDistanceFromHome);
    }
}
