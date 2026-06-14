using UnityEngine;
using System.Collections;
//using System;

public class GolemController : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;
    public int GetCurrentHealth() { return currentHealth; }
    public bool IsDead() { return isDead; }

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float stopDistance = 1.5f;

    [Header("Attack Ranges & Cooldowns")]
    public float meleeRange = 1f;
    public float rangedRange = 8f;
    public float attackCooldown = 5f;
    private float attackCooldownTimer = 0f;
    public int meleeDamage = 20;

    [Header("Attack Durations (Fallback)")]
    public float meleeDuration = 1.2f;
    public float laserDuration = 2.5f;
    public float handShootDuration = 1.0f;

    [Header("Aim & Attack Objects")]
    public Transform player;
    public Transform aimPivot;
    public Transform firePoint;
    public GameObject laserBeamObj;
    public GameObject handProjectilePrefab;
    public SpriteRenderer laserSpriteRenderer;

    [Header("Sound")]
    public AudioClip meleeAttackSound;
    [Range(0f, 1f)] public float meleeAttackSoundVolume = 1f;
    public AudioClip handThrowSound;
    [Range(0f, 1f)] public float handThrowSoundVolume = 1f;
    public AudioClip laserShootSound;
    [Range(0f, 1f)] public float laserShootSoundVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    private Animator animator;
    private AudioSource audioSource;
    private bool isAttacking = false;
    private bool isDead = false;
    private Coroutine attackCoroutine;

    private bool isAimLocked = false;
    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        currentHealth = maxHealth;

        // Tự động tìm Player nếu chưa được kéo thả vào Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (laserBeamObj != null) laserBeamObj.SetActive(false);
    }

    void Update()
    {
        if (isDead) return;

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // --- SỬA QUAN TRỌNG: Đưa lệnh ngắm bắn lên trên ---
        // Boss sẽ luôn luôn ngắm theo người chơi, TRỪ KHI bị khóa ngắm (isAimLocked = true)
        if (player != null && !isAimLocked)
        {
            UpdateAimAndLookDirection();
        }

        // Lệnh này giữ nguyên: Nếu đang bận tấn công thì không di chuyển hay gọi chiêu mới
        if (player == null || isAttacking) return;
        
        Collider2D bossCollider = GetComponent<Collider2D>();
        Vector2 bossCenter = bossCollider != null ? (Vector2)bossCollider.bounds.center : (Vector2)transform.position;
        float distanceToPlayer = Vector2.Distance(bossCenter, player.position);

        // Quyết định tấn công hoặc di chuyển (giữ nguyên như cũ)
        if (distanceToPlayer <= meleeRange && attackCooldownTimer <= 0f)
        {
            StartAttackSequence(MeleeAttackSequence());
        }
        else if (distanceToPlayer <= rangedRange && distanceToPlayer > meleeRange && attackCooldownTimer <= 0f)
        {
            StartRandomRangedAttack();
        }
        else if (distanceToPlayer > stopDistance)
        {
            ChasePlayer();
        }
    }

    void UpdateAimAndLookDirection()
    {
        Vector2 targetDirection = player.position - aimPivot.position;

        // Cập nhật hướng quay mặt trái/phải dựa vào LookX
        animator.SetFloat("LookX", targetDirection.x);

        // Xoay trục ngắm bắn 360 độ
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // Lật hình tia laser nếu hướng bắn nằm ở nửa bên trái màn hình
        if (laserSpriteRenderer != null)
        {
            laserSpriteRenderer.flipY = (angle > 90 || angle < -90);
        }
    }

    public void LockAim()
    {
        isAimLocked = true; // Khóa cứng trục xoay, Boss không lia súng theo nữa
    }

    public void UnlockAim()
    {
        isAimLocked = false; // Mở khóa ngắm trở lại
    }

    void ChasePlayer()
    {
        // Boss sẽ di chuyển trong khi vẫn đang ở state "idle"
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    void StartRandomRangedAttack()
    {
        int roll = Random.Range(0, 2);
        if (roll == 0) StartAttackSequence(LaserAttackSequence());
        else StartAttackSequence(HandShootAttackSequence());
    }

    void StartAttackSequence(IEnumerator sequence)
    {
        attackCooldownTimer = attackCooldown;
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(sequence);
    }

    // --- COROUTINES QUẢN LÝ TẤN CÔNG ---

    IEnumerator MeleeAttackSequence()
    {
        isAttacking = true;
        // Kích hoạt mũi tên từ idle -> melee
        animator.SetTrigger("Melee");

        yield return new WaitForSeconds(meleeDuration);

        isAttacking = false;
        attackCoroutine = null;
    }

    IEnumerator LaserAttackSequence()
    {
        isAttacking = true;
        // Kích hoạt mũi tên từ idle -> Lasercast
        animator.SetTrigger("LaserShoot");

        yield return new WaitForSeconds(laserDuration);

        UnlockAim();
        DeactivateLaser(); // Tắt laser để an toàn
        isAttacking = false;
        attackCoroutine = null;
    }

    IEnumerator HandShootAttackSequence()
    {
        isAttacking = true;
        // Kích hoạt mũi tên từ idle -> Shoot
        animator.SetTrigger("HandShoot");

        yield return new WaitForSeconds(handShootDuration);

        UnlockAim();
        isAttacking = false;
        attackCoroutine = null;
    }

    // --- CƠ CHẾ MÁU VÀ CÁI CHẾT ---

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;

        if (attackCoroutine != null) StopCoroutine(attackCoroutine);

        // Kích hoạt mũi tên chuyển sang Die
        animator.SetBool("Death", true);
        PlaySound(deathSound, deathSoundVolume);
        DeactivateLaser();

        // Dừng lập tức mọi di chuyển vật lý
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Tắt luôn vật lý để đạn của người chơi bay xuyên qua xác
        }

        // Tắt Hitbox để người chơi không dẫm phải xác Boss
        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null)
        {
            coll.enabled = false;
        }
    }

    // --- ANIMATION EVENTS ---
    // Gọi các hàm này trong khung hình của Animation Clip bằng Animation Event

    public void ActivateLaser()
    {
        PlaySound(laserShootSound, laserShootSoundVolume);
        if (laserBeamObj != null) laserBeamObj.SetActive(true);
    }

    public void DeactivateLaser()
    {
        if (laserBeamObj != null) laserBeamObj.SetActive(false);
    }

    public void ShootHandProjectile()
    {
        PlaySound(handThrowSound, handThrowSoundVolume);

        if (handProjectilePrefab != null && firePoint != null)
        {
            // 1. Tạo ra viên đạn và lưu vào biến 'bullet'
            GameObject bullet = Instantiate(handProjectilePrefab, firePoint.position, firePoint.rotation);

            // 2. Lấy component GolemProjectile từ viên đạn vừa tạo
            GolemProjectile proj = bullet.GetComponent<GolemProjectile>();

            if (proj != null)
            {
                // 3. Gọi hàm Launch để đẩy đạn bay đi.
                // - firePoint.right: Hướng bay (mũi tên đỏ trục X của firePoint đang chĩa vào người chơi)
                // - 500f: Lực đẩy (bạn có thể tăng giảm số này để đạn bay nhanh hay chậm)
                proj.Launch(firePoint.right, 500f);
            }
        }
    }

    // Gắn Animation Event vào frame vung kiếm/đấm trúng người chơi trong clip "melee"
    public void DealMeleeDamage()
    {
        PlaySound(meleeAttackSound, meleeAttackSoundVolume);

        Collider2D bossCollider = GetComponent<Collider2D>();
        Vector2 bossCenter = bossCollider != null ? (Vector2)bossCollider.bounds.center : (Vector2)transform.position;
        float distance = Vector2.Distance(bossCenter, player.position);
        if (distance <= meleeRange)
        {
            PlayerController playerScript = player.GetComponent<PlayerController>();

            if (playerScript != null)
            {
                playerScript.changeHealth(-meleeDamage);
            }
        }
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}
