using System.Collections;
using UnityEngine;

public class AgisBossController : MonoBehaviour
{
    public int maxHealth = 300;
    public float detectionRange = 10f;
    public float attackCooldown = 2f;

    [Header("Projectile Ring")]
    public BossProjectile boss2Projectile1Prefab;
    public BossProjectile boss2Projectile2Prefab;
    public int waveCount = 3;
    public float timeBetweenWaves = 1f;
    public int projectileCount = 24;
    public float projectileForce = 500f;
    public float projectileSpawnRadius = 0.8f;
    public float firstWaveDelay = 0.2f;
    public float waveRotationStepDegrees = 5f;

    [Header("Death")]
    public float deathDestroyDelay = 0f;

    [Header("Background Music")]
    [Tooltip("Drag the boss background music clip here")]
    public AudioClip bossMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    public bool loopMusic = true;

    AudioSource musicSource;

    [Header("SFX")]
    [Tooltip("Drag the attack sound clip here")]
    public AudioClip attackSound;
    [Range(0f, 1f)]
    public float attackSoundVolume = 0.5f;

    [Header("Win UI")]
    public GameObject uiWinCanvas;
    public string uiWinCanvasName = "UIWin";
    public bool showWinUIOnDeath = true;
    public bool pauseGameOnWin = true;

    int currentHealth;
    float attackCooldownTimer;
    bool combatEnabled = true;
    bool isAttacking;
    bool isDead;
    Transform playerTarget;
    Collider2D bossCollider;
    SpriteRenderer spriteRenderer;
    Coroutine attackCoroutine;
    Coroutine deathCoroutine;

    void Awake()
    {
        currentHealth = maxHealth;
        bossCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        FindPlayer();
        PlayBackgroundMusic();
    }

    void PlayBackgroundMusic()
    {
        if (bossMusic == null)
        {
            return;
        }

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.clip = bossMusic;
        musicSource.volume = musicVolume;
        musicSource.loop = loopMusic;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D Sound for background music
        musicSource.Play();
    }

    void Update()
    {
        if (!combatEnabled || isDead)
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
            return;
        }

        FacePlayer();

        if (!isAttacking && attackCooldownTimer <= 0f && IsPlayerInDetectionRange())
        {
            attackCoroutine = StartCoroutine(ProjectileRingAttack());
        }
    }

    IEnumerator ProjectileRingAttack()
    {
        isAttacking = true;
        attackCooldownTimer = attackCooldown;

        if (firstWaveDelay > 0f)
        {
            yield return new WaitForSeconds(firstWaveDelay);
        }

        int waves = Mathf.Max(1, waveCount);
        for (int wave = 0; wave < waves; wave++)
        {
            FireProjectileRing(wave);

            if (wave < waves - 1 && timeBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        isAttacking = false;
        attackCoroutine = null;
    }

    void FireProjectileRing(int waveIndex)
    {
        int count = Mathf.Max(1, projectileCount);
        float angleStep = Mathf.PI * 2f / count;
        float waveRotation = waveRotationStepDegrees * waveIndex * Mathf.Deg2Rad;
        Vector2 center = transform.position;
        BossProjectile projectilePrefab = RandomProjectilePrefab();

        if (projectilePrefab == null)
        {
            return;
        }

        PlayAttackSound();

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i + waveRotation;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 spawnPosition = center + direction * projectileSpawnRadius;

            BossProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            IgnoreBossCollision(projectile);
            projectile.Launch(direction, projectileForce);
        }
    }

    void PlayAttackSound()
    {
        if (attackSound != null && musicSource != null)
        {
            musicSource.PlayOneShot(attackSound, attackSoundVolume);
        }
    }

    BossProjectile RandomProjectilePrefab()
    {
        if (boss2Projectile1Prefab == null)
        {
            return boss2Projectile2Prefab;
        }

        if (boss2Projectile2Prefab == null)
        {
            return boss2Projectile1Prefab;
        }

        return Random.value < 0.5f ? boss2Projectile1Prefab : boss2Projectile2Prefab;
    }

    void IgnoreBossCollision(BossProjectile projectile)
    {
        if (projectile == null || bossCollider == null)
        {
            return;
        }

        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            Physics2D.IgnoreCollision(projectileCollider, bossCollider);
        }
    }

    bool IsPlayerInDetectionRange()
    {
        return Vector2.Distance(transform.position, playerTarget.position) <= detectionRange;
    }

    void FindPlayer()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        playerTarget = player != null ? player.transform : null;
    }

    void FacePlayer()
    {
        if (playerTarget == null)
        {
            return;
        }

        float facingX = playerTarget.position.x >= transform.position.x ? 1f : -1f;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingX;
        transform.localScale = scale;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead)
        {
            return;
        }

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        FlashWhenHit();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (enabled)
        {
            attackCooldownTimer = Mathf.Max(attackCooldownTimer, attackCooldown);
            return;
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        attackCooldownTimer = attackCooldown;
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        deathCoroutine = StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (deathDestroyDelay > 0f)
        {
            yield return new WaitForSeconds(deathDestroyDelay);
        }

        ShowWinUIAndPauseGame();
        Destroy(gameObject);
    }

    void ShowWinUIAndPauseGame()
    {
        GameAudioUtility.StopAllAudioSources();

        if (showWinUIOnDeath)
        {
            FindUIWinCanvasIfNeeded();
            if (uiWinCanvas != null)
            {
                uiWinCanvas.SetActive(true);
            }
        }

        if (pauseGameOnWin)
        {
            Time.timeScale = 0f;
        }
    }

    void FindUIWinCanvasIfNeeded()
    {
        if (uiWinCanvas != null)
        {
            return;
        }

        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject == null || !sceneObject.scene.IsValid())
            {
                continue;
            }

            if (sceneObject.name == uiWinCanvasName || sceneObject.name == "UIWin")
            {
                uiWinCanvas = sceneObject;
                return;
            }
        }
    }

    void FlashWhenHit()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(HitFlash());
        }
    }

    IEnumerator HitFlash()
    {
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.12f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
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
        if (source == null || isDead)
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

    void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        detectionRange = Mathf.Max(0f, detectionRange);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        waveCount = Mathf.Max(1, waveCount);
        timeBetweenWaves = Mathf.Max(0f, timeBetweenWaves);
        projectileCount = Mathf.Max(1, projectileCount);
        projectileForce = Mathf.Max(0f, projectileForce);
        projectileSpawnRadius = Mathf.Max(0f, projectileSpawnRadius);
        firstWaveDelay = Mathf.Max(0f, firstWaveDelay);
        deathDestroyDelay = Mathf.Max(0f, deathDestroyDelay);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.loop = loopMusic;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, projectileSpawnRadius);
    }
}
