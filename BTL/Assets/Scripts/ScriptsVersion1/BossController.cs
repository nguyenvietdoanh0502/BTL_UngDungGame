using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BossController : MonoBehaviour
{
    const string BaseLayerPrefix = "Base Layer.";

    [Header("Scene Check")]
    public string expectedBossName = "Boss";
    public bool warnWhenSceneSetupIsUnexpected = true;

    [Header("Face 1")]
    public int face1MaxHealth = 100;
    public int face1Attack1Damage = 15;
    public int face1Attack2Damage = 30;
    public int face1FireBreathCount = 3;
    public float face1Attack1Weight = 2f;
    public float face1Attack2Weight = 1f;
    public float face1Attack3Weight = 1f;

    [Header("Face 2")]
    public int face2MaxHealth = 200;
    public int face2Attack1Damage = 30;
    public int face2Attack2Damage = 60;
    public int face2FireBreathCount = 4;

    [Header("Detection")]
    public float detectionRange = 8f;
    public float attackCooldown = 2f;
    [Range(0f, 1f)] public float attack1Chance = 2f / 3f;
    public float lostPlayerRetryDelay = 0.5f;

    [Header("Boss Arena Gate")]
    public GameObject bossArenaExitBlock;
    public bool reactivateBlockWhenPlayerEntersArena = true;
    public bool useBossArenaEntryLine = true;
    public float bossArenaEntryLineOffsetY = 0.75f;
    public float bossArenaEntryRange = 5f;

    [Header("Attack 1 - Teleport Strike")]
    public float teleportDistanceFromPlayer = 1.2f;
    public float attack1PreAttackDelay = 1f;
    public float attack1DamageDelay = 0.25f;
    public float attack1HitRadius = 1.4f;
    public float attack1ReturnDelay = 0.15f;
    public float attack1FallbackDuration = 0.7f;

    [Header("Attack 2 - Fire Breath")]
    public float attack2DamageDelay = 0.65f;
    public float attack2FireActiveDuration = 0.85f;
    public float attack2FallbackDuration = 0.8f;
    public Vector2 fireDirection = Vector2.down;
    [FormerlySerializedAs("fireOriginOffset")] public Vector2 face1FireOriginOffset = new Vector2(0f, 0.8f);
    [FormerlySerializedAs("fireRange")] public float face1FireRange = 6f;
    [FormerlySerializedAs("fireWidth")] public float face1FireWidth = 1.875f;
    public Vector2 face2FireOriginOffset = new Vector2(0f, 0.8f);
    public float face2FireRange = 8.5f;
    public float face2FireWidth = 2.75f;
    public float sideStepDistance = 2.5f;
    public float sideStepDuration = 0.25f;
    public float timeBetweenFireBreaths = 0.2f;

    [Header("Attack 3 - Projectile Ring")]
    public BossProjectile bossProjectilePrefab;
    public int attack3WaveCount = 3;
    public float attack3TimeBetweenWaves = 1f;
    public int attack3ProjectileCount = 24;
    public float attack3ProjectileForce = 500f;
    public float attack3ProjectileSpawnRadius = 0.8f;
    public float attack3FirstWaveDelay = 0.2f;
    public float attack3WaveRotationStepDegrees = 15f;
    public float attack3FallbackDuration = 2.4f;

    [Header("Animation States")]
    public string face1IdleStateName = "Boss1Idle";
    public string face1Attack1StateName = "Boss1Attack1";
    public string face1Attack2StateName = "Boss1Attack2";
    public string face1Attack3StateName = "Boss1Attack3";
    public string face2IdleStateName = "Boss2Idle";
    public string face2Attack1StateName = "Boss2Attack1";
    public string face2Attack2StateName = "Boss2Attack2";
    public string sharedIdleStateName = "Idle";
    public string sharedAttack1StateName = "Attack1";
    public string sharedAttack2StateName = "Attack2";
    public string transformStateName = "BossTranform";
    public string deathStateName = "BossDie";
    public string transformedParameter = "isTranformed";
    public string deathParameter = "isDeath";
    public float transformFallbackDuration = 1f;
    public float deathFallbackDuration = 1f;
    public float deathDestroyDelay = 2f;

    [Header("Animation Clips")]
    public AnimationClip boss1IdleClip;
    public AnimationClip boss1Attack1Clip;
    public AnimationClip boss1Attack2Clip;
    public AnimationClip boss1Attack3Clip;
    public AnimationClip boss2IdleClip;
    public AnimationClip boss2Attack1Clip;
    public AnimationClip boss2Attack2Clip;
    public AnimationClip bossTransformClip;
    public AnimationClip bossDieClip;

    [Header("Hit Feedback")]
    public bool createHitColliderIfMissing = true;
    public float hurtFlashDuration = 0.12f;
    public Color hurtFlashColor = Color.red;

    [Header("Sound")]
    public AudioClip appearSound;
    [Range(0f, 1f)] public float appearSoundVolume = 1f;
    public AudioClip unlockLoopSound;
    [Range(0f, 1f)] public float unlockLoopSoundVolume = 1f;
    public bool playUnlockLoopSound = true;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float hitSoundVolume = 1f;
    public AudioClip teleportSound;
    [Range(0f, 1f)] public float teleportSoundVolume = 1f;
    public AudioClip face1Attack1Sound;
    [Range(0f, 1f)] public float face1Attack1SoundVolume = 1f;
    public AudioClip face2Attack1Sound;
    [Range(0f, 1f)] public float face2Attack1SoundVolume = 1f;
    public AudioClip face1Attack2Sound;
    [Range(0f, 1f)] public float face1Attack2SoundVolume = 1f;
    public AudioClip face2Attack2Sound;
    [Range(0f, 1f)] public float face2Attack2SoundVolume = 1f;
    public AudioClip attack3FireSound;
    [Range(0f, 1f)] public float attack3FireSoundVolume = 1f;
    public AudioClip laughSound;
    [Range(0f, 1f)] public float laughSoundVolume = 1f;
    public float minLaughInterval = 4f;
    public float maxLaughInterval = 8f;
    public AudioClip transformSound;
    [Range(0f, 1f)] public float transformSoundVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;
    public AudioClip postDestroySound;
    [Range(0f, 1f)] public float postDestroySoundVolume = 1f;

    [Header("Win UI")]
    public GameObject uiWinCanvas;
    public string uiWinCanvasName = "UIWin";
    public bool showWinUIOnDeath = true;
    public bool pauseGameOnWin = true;

    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer spriteRenderer;
    AudioSource audioSource;
    AudioSource unlockLoopAudioSource;
    PlayerController playerController;
    Transform playerTarget;
    ContactFilter2D playerHitFilter;
    readonly Collider2D[] playerHitResults = new Collider2D[16];
    AnimatorOverrideController overrideController;
    RuntimeAnimatorController baseController;
    List<KeyValuePair<AnimationClip, AnimationClip>> clipOverrides;
    Coroutine attackCoroutine;
    Coroutine hurtCoroutine;
    Coroutine deathCoroutine;
    Color startColor;
    Vector2 homePosition;
    float startScaleX;
    float attackCooldownTimer;
    float laughTimer;
    int currentFace = 1;
    int currentHealth;
    bool isAttacking;
    bool isTransforming;
    bool isDead;
    bool isInvincible;
    bool wasPlayerInDetectionRange;
    bool combatEnabled = true;
    bool hasReactivatedArenaExitBlock;
    bool isPlayingUnlockLoopSound;

    public int CurrentFace => currentFace;
    public int CurrentHealth => currentHealth;

    void Awake()
    {
#if UNITY_EDITOR
        AutoAssignBossClips();
#endif

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
        playerHitFilter = ContactFilter2D.noFilter;
        playerHitFilter.useTriggers = true;
        homePosition = GetPosition();
        startScaleX = Mathf.Abs(transform.localScale.x);
        currentFace = 1;
        currentHealth = face1MaxHealth;

        if (spriteRenderer != null)
        {
            startColor = spriteRenderer.color;
        }

        EnsureHitCollider();
        SetupAnimatorOverrideController();
        ApplyCurrentFaceAnimationClips();
        PlayIdleAnimation();
    }

    void Start()
    {
        ValidateSceneSetup();
        FindPlayer();
    }

    void OnDisable()
    {
        StopUnlockLoopSound();
    }

    void OnValidate()
    {
        face1MaxHealth = Mathf.Max(1, face1MaxHealth);
        face2MaxHealth = Mathf.Max(1, face2MaxHealth);
        face1FireBreathCount = Mathf.Max(1, face1FireBreathCount);
        face2FireBreathCount = Mathf.Max(1, face2FireBreathCount);
        face1Attack1Weight = Mathf.Max(0f, face1Attack1Weight);
        face1Attack2Weight = Mathf.Max(0f, face1Attack2Weight);
        face1Attack3Weight = Mathf.Max(0f, face1Attack3Weight);
        detectionRange = Mathf.Max(0f, detectionRange);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        attack1Chance = Mathf.Clamp01(attack1Chance);
        bossArenaEntryRange = Mathf.Max(0.1f, bossArenaEntryRange);
        teleportDistanceFromPlayer = Mathf.Max(0.1f, teleportDistanceFromPlayer);
        attack1PreAttackDelay = Mathf.Max(0f, attack1PreAttackDelay);
        attack1HitRadius = Mathf.Max(0.1f, attack1HitRadius);
        attack2DamageDelay = Mathf.Max(0f, attack2DamageDelay);
        attack2FireActiveDuration = Mathf.Max(0f, attack2FireActiveDuration);
        face1FireRange = Mathf.Max(0.1f, face1FireRange);
        face1FireWidth = Mathf.Max(0.1f, face1FireWidth);
        face2FireRange = Mathf.Max(0.1f, face2FireRange);
        face2FireWidth = Mathf.Max(0.1f, face2FireWidth);
        if (fireDirection.sqrMagnitude <= 0.001f)
        {
            fireDirection = Vector2.down;
        }
        sideStepDuration = Mathf.Max(0.01f, sideStepDuration);
        attack3WaveCount = Mathf.Max(1, attack3WaveCount);
        attack3TimeBetweenWaves = Mathf.Max(0f, attack3TimeBetweenWaves);
        attack3ProjectileCount = Mathf.Max(1, attack3ProjectileCount);
        attack3ProjectileForce = Mathf.Max(0f, attack3ProjectileForce);
        attack3ProjectileSpawnRadius = Mathf.Max(0f, attack3ProjectileSpawnRadius);
        attack3FirstWaveDelay = Mathf.Max(0f, attack3FirstWaveDelay);
        attack3FallbackDuration = Mathf.Max(0.01f, attack3FallbackDuration);
        deathDestroyDelay = Mathf.Max(0f, deathDestroyDelay);
        minLaughInterval = Mathf.Max(0.1f, minLaughInterval);
        maxLaughInterval = Mathf.Max(minLaughInterval, maxLaughInterval);

#if UNITY_EDITOR
        AutoAssignBossClips();
#endif
    }

    void Update()
    {
        if (!combatEnabled || isDead || isTransforming)
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

        UpdateArenaExitBlock();

        bool playerInDetectionRange = IsPlayerInDetectionRange();
        UpdateRandomLaugh(playerInDetectionRange);

        if (!isAttacking && attackCooldownTimer <= 0f && playerInDetectionRange)
        {
            StartRandomAttack();
        }
    }

    void ValidateSceneSetup()
    {
        if (!warnWhenSceneSetupIsUnexpected)
        {
            return;
        }

        if (!string.IsNullOrEmpty(expectedBossName) && gameObject.name != expectedBossName)
        {
            Debug.LogWarning($"{nameof(BossController)} expects the boss GameObject to be named '{expectedBossName}', but this object is '{name}'.", this);
        }

        if (animator == null)
        {
            Debug.LogWarning($"{nameof(BossController)} needs an Animator on {name}.", this);
        }
    }

    void FindPlayer()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        playerTarget = playerController != null ? playerController.transform : null;
    }

    bool IsPlayerInDetectionRange()
    {
        if (playerTarget == null)
        {
            return false;
        }

        return Vector2.Distance(GetPosition(), playerTarget.position) <= detectionRange;
    }

    void UpdateArenaExitBlock()
    {
        if (!reactivateBlockWhenPlayerEntersArena || hasReactivatedArenaExitBlock || playerTarget == null)
        {
            return;
        }

        FindArenaExitBlockIfNeeded();
        if (bossArenaExitBlock == null || bossArenaExitBlock.activeSelf || !HasPlayerEnteredBossArena())
        {
            return;
        }

        bossArenaExitBlock.SetActive(true);
        hasReactivatedArenaExitBlock = true;
    }

    bool HasPlayerEnteredBossArena()
    {
        if (bossArenaExitBlock != null && useBossArenaEntryLine)
        {
            float blockY = bossArenaExitBlock.transform.position.y;
            bool bossIsAboveBlock = transform.position.y >= blockY;
            float entryLineY = blockY + (bossIsAboveBlock ? bossArenaEntryLineOffsetY : -bossArenaEntryLineOffsetY);
            return bossIsAboveBlock
                ? playerTarget.position.y >= entryLineY
                : playerTarget.position.y <= entryLineY;
        }

        return Vector2.Distance(GetPosition(), playerTarget.position) <= bossArenaEntryRange;
    }

    void FindArenaExitBlockIfNeeded()
    {
        if (bossArenaExitBlock != null)
        {
            return;
        }

        EnemyKillBlockUnlocker[] unlockers = Resources.FindObjectsOfTypeAll<EnemyKillBlockUnlocker>();
        foreach (EnemyKillBlockUnlocker unlocker in unlockers)
        {
            if (unlocker == null || !unlocker.gameObject.scene.IsValid())
            {
                continue;
            }

            bossArenaExitBlock = unlocker.blockToHide != null ? unlocker.blockToHide : unlocker.gameObject;
            return;
        }
    }

    void StartRandomAttack()
    {
        attackCooldownTimer = attackCooldown;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            isInvincible = false;
        }

        attackCoroutine = StartCoroutine(SelectRandomAttackSequence());
    }

    IEnumerator SelectRandomAttackSequence()
    {
        if (currentFace == 1)
        {
            float totalWeight = face1Attack1Weight + face1Attack2Weight + face1Attack3Weight;
            if (totalWeight <= 0f)
            {
                return Attack1Sequence();
            }

            float roll = Random.value * totalWeight;
            if (roll < face1Attack1Weight)
            {
                return Attack1Sequence();
            }

            roll -= face1Attack1Weight;
            if (roll < face1Attack2Weight)
            {
                return Attack2Sequence();
            }

            return Attack3Sequence();
        }

        bool useTeleportAttack = Random.value < attack1Chance;
        return useTeleportAttack ? Attack1Sequence() : Attack2Sequence();
    }

    IEnumerator Attack1Sequence()
    {
        isAttacking = true;

        Vector2 returnPosition = GetPosition();
        Vector2 teleportPosition = GetTeleportAttackPosition(returnPosition);
        FaceDirection((Vector2)playerTarget.position - teleportPosition);
        SetPosition(teleportPosition);
        PlayTeleportSound();
        PlayIdleAnimation();

        if (attack1PreAttackDelay > 0f)
        {
            yield return new WaitForSeconds(attack1PreAttackDelay);
        }

        FacePlayer();
        PlayAttack1Animation();
        PlayAttack1Sound();

        float attackDuration = GetAttack1Duration();
        float damageDelay = Mathf.Clamp(attack1DamageDelay, 0f, attackDuration);
        if (damageDelay > 0f)
        {
            yield return new WaitForSeconds(damageDelay);
        }

        float activeHitTime = Mathf.Max(0.01f, attackDuration - damageDelay);
        yield return DamagePlayerWhileInCircle(CurrentAttack1Damage(), attack1HitRadius, activeHitTime);

        if (attack1ReturnDelay > 0f)
        {
            yield return new WaitForSeconds(attack1ReturnDelay);
        }

        SetPosition(returnPosition);
        FacePlayer();
        PlayIdleAnimation();

        isAttacking = false;
        attackCoroutine = null;
    }

    IEnumerator Attack3Sequence()
    {
        isAttacking = true;
        isInvincible = true;

        FacePlayer();
        PlayAttack3Animation();

        float elapsed = 0f;
        float firstWaveDelay = Mathf.Max(0f, attack3FirstWaveDelay);
        if (firstWaveDelay > 0f)
        {
            yield return new WaitForSeconds(firstWaveDelay);
            elapsed += firstWaveDelay;
        }

        int waveCount = Mathf.Max(1, attack3WaveCount);
        for (int wave = 0; wave < waveCount; wave++)
        {
            FireAttack3ProjectileRing(wave);

            if (wave >= waveCount - 1 || attack3TimeBetweenWaves <= 0f)
            {
                continue;
            }

            yield return new WaitForSeconds(attack3TimeBetweenWaves);
            elapsed += attack3TimeBetweenWaves;
        }

        float remainingAnimationTime = Mathf.Max(0f, GetAttack3Duration() - elapsed);
        if (remainingAnimationTime > 0f)
        {
            yield return new WaitForSeconds(remainingAnimationTime);
        }

        isInvincible = false;
        PlayIdleAnimation();
        isAttacking = false;
        attackCoroutine = null;
    }

    void FireAttack3ProjectileRing(int waveIndex)
    {
        if (bossProjectilePrefab == null)
        {
            Debug.LogWarning($"{nameof(BossController)} cannot use Attack3 because Boss Projectile Prefab is missing.", this);
            return;
        }

        PlayAttack3FireSound();

        int projectileCount = Mathf.Max(1, attack3ProjectileCount);
        Vector2 center = GetPosition();
        float waveRotation = attack3WaveRotationStepDegrees * waveIndex * Mathf.Deg2Rad;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = waveRotation + Mathf.PI * 2f * i / projectileCount;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
            Vector2 spawnPosition = center + direction * attack3ProjectileSpawnRadius;
            BossProjectile projectile = Instantiate(bossProjectilePrefab, spawnPosition, Quaternion.identity);
            IgnoreBossCollision(projectile);
            projectile.Launch(direction, attack3ProjectileForce);
        }
    }

    void IgnoreBossCollision(BossProjectile projectile)
    {
        if (projectile == null)
        {
            return;
        }

        Collider2D[] bossColliders = GetComponents<Collider2D>();
        Collider2D[] projectileColliders = projectile.GetComponents<Collider2D>();
        foreach (Collider2D bossCollider in bossColliders)
        {
            foreach (Collider2D projectileCollider in projectileColliders)
            {
                Physics2D.IgnoreCollision(bossCollider, projectileCollider, true);
            }
        }
    }

    IEnumerator Attack2Sequence()
    {
        isAttacking = true;

        int fireCount = CurrentFireBreathCount();

        for (int i = 0; i < fireCount; i++)
        {
            FacePlayer();
            PlayAttack2Animation();
            PlayAttack2Sound();

            float attackDuration = GetAttack2Duration();
            float fireStartDelay = Mathf.Clamp(attack2DamageDelay, 0f, attackDuration);
            if (fireStartDelay > 0f)
            {
                yield return new WaitForSeconds(fireStartDelay);
            }

            float remainingFireTime = Mathf.Max(0f, attackDuration - fireStartDelay);
            float fireActiveTime = attack2FireActiveDuration > 0f
                ? Mathf.Min(attack2FireActiveDuration, remainingFireTime)
                : remainingFireTime;
            fireActiveTime = Mathf.Max(0.01f, fireActiveTime);
            yield return DamagePlayerWhileInFireBreath(CurrentAttack2Damage(), fireActiveTime);

            float afterFireTime = Mathf.Max(0f, attackDuration - fireStartDelay - fireActiveTime);
            if (afterFireTime > 0f)
            {
                yield return new WaitForSeconds(afterFireTime);
            }

            if (i >= fireCount - 1)
            {
                continue;
            }

            if (timeBetweenFireBreaths > 0f)
            {
                yield return new WaitForSeconds(timeBetweenFireBreaths);
            }

            Vector2 moveDirection = GetHorizontalDirectionTowardPlayer();
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Vector2 nextPosition = GetPosition() + moveDirection * sideStepDistance;
                FaceDirection(moveDirection);
                yield return MoveToPosition(nextPosition, sideStepDuration);
            }
        }

        PlayIdleAnimation();
        isAttacking = false;
        attackCoroutine = null;
    }

    Vector2 GetTeleportAttackPosition(Vector2 fallbackPosition)
    {
        if (playerTarget == null)
        {
            return fallbackPosition;
        }

        Vector2 bossPosition = fallbackPosition;
        Vector2 playerPosition = playerTarget.position;
        Vector2 directionToPlayer = playerPosition - bossPosition;

        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            directionToPlayer = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        }

        return playerPosition - directionToPlayer.normalized * teleportDistanceFromPlayer;
    }

    IEnumerator MoveToPosition(Vector2 targetPosition, float duration)
    {
        Vector2 startPosition = GetPosition();
        float elapsed = 0f;
        float clampedDuration = Mathf.Max(0.01f, duration);

        while (elapsed < clampedDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / clampedDuration);
            Vector2 nextPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            SetPosition(nextPosition);
            yield return new WaitForFixedUpdate();
        }

        SetPosition(targetPosition);
    }

    Vector2 GetHorizontalDirectionTowardPlayer()
    {
        if (playerTarget == null)
        {
            return Vector2.zero;
        }

        float directionX = playerTarget.position.x - GetPosition().x;
        if (Mathf.Abs(directionX) <= 0.001f)
        {
            return Vector2.zero;
        }

        return directionX > 0f ? Vector2.right : Vector2.left;
    }

    IEnumerator DamagePlayerWhileInCircle(int damage, float radius, float duration)
    {
        bool hasDamagedPlayer = false;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!hasDamagedPlayer && IsPlayerTouchingCircle(radius))
            {
                playerController.changeHealth(-damage);
                hasDamagedPlayer = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DamagePlayerWhileInFireBreath(int damage, float duration)
    {
        bool hasDamagedPlayer = false;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!hasDamagedPlayer && IsPlayerTouchingFireBreath())
            {
                playerController.changeHealth(-damage);
                hasDamagedPlayer = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    bool IsPlayerTouchingCircle(float radius)
    {
        if (playerController == null || playerTarget == null)
        {
            return false;
        }

        int hitCount = Physics2D.OverlapCircle(GetPosition(), radius, playerHitFilter, playerHitResults);
        for (int i = 0; i < hitCount; i++)
        {
            if (IsPlayerCollider(playerHitResults[i]))
            {
                return true;
            }
        }

        return Vector2.Distance(GetPosition(), playerTarget.position) <= radius;
    }

    bool IsPlayerTouchingFireBreath()
    {
        if (playerController == null || playerTarget == null)
        {
            return false;
        }

        Vector2 forward = GetFireDirection();
        Vector2 fireOrigin = GetFireOrigin();
        float fireRange = CurrentFireRange();
        float fireWidth = CurrentFireWidth();
        Vector2 fireCenter = fireOrigin + forward * fireRange * 0.5f;
        Vector2 fireSize = new Vector2(fireWidth, fireRange);
        float fireAngle = GetFireBoxAngle(forward);
        int hitCount = Physics2D.OverlapBox(fireCenter, fireSize, fireAngle, playerHitFilter, playerHitResults);

        for (int i = 0; i < hitCount; i++)
        {
            if (IsPlayerCollider(playerHitResults[i]))
            {
                return true;
            }
        }

        Vector2 toPlayer = (Vector2)playerTarget.position - fireOrigin;
        float forwardDistance = Vector2.Dot(toPlayer, forward);

        if (forwardDistance < 0f || forwardDistance > fireRange)
        {
            return false;
        }

        Vector2 sideDirection = new Vector2(-forward.y, forward.x);
        float sideDistance = Mathf.Abs(Vector2.Dot(toPlayer, sideDirection));
        return sideDistance <= fireWidth * 0.5f;
    }

    bool IsPlayerCollider(Collider2D hitCollider)
    {
        if (hitCollider == null || playerController == null)
        {
            return false;
        }

        return hitCollider.GetComponentInParent<PlayerController>() == playerController;
    }

    Vector2 GetFacingDirection()
    {
        return transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
    }

    Vector2 GetFireDirection()
    {
        if (fireDirection.sqrMagnitude <= 0.001f)
        {
            return Vector2.down;
        }

        return fireDirection.normalized;
    }

    Vector2 GetFireOrigin()
    {
        return GetFireOrigin(CurrentFireOriginOffset());
    }

    Vector2 GetFireOrigin(Vector2 originOffset)
    {
        float facingSign = transform.localScale.x >= 0f ? 1f : -1f;
        Vector2 mirroredOffset = new Vector2(originOffset.x * facingSign, originOffset.y);
        return GetPosition() + mirroredOffset;
    }

    Vector2 CurrentFireOriginOffset()
    {
        return currentFace == 1 ? face1FireOriginOffset : face2FireOriginOffset;
    }

    float CurrentFireRange()
    {
        return currentFace == 1 ? face1FireRange : face2FireRange;
    }

    float CurrentFireWidth()
    {
        return currentFace == 1 ? face1FireWidth : face2FireWidth;
    }

    float GetFireBoxAngle(Vector2 fireForward)
    {
        return Vector2.SignedAngle(Vector2.up, fireForward);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || isDead || isTransforming || isInvincible)
        {
            return;
        }

        PlayHitSound();
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            if (currentFace == 1)
            {
                StartCoroutine(TransformToFace2Sequence());
            }
            else
            {
                Die();
            }

            return;
        }

        FlashWhenHit();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetCurrentFace()
    {
        return currentFace;
    }

    IEnumerator TransformToFace2Sequence()
    {
        isTransforming = true;
        isAttacking = false;
        isInvincible = false;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        SetPosition(homePosition);
        FacePlayer();
        PlayTransformAnimation();
        PlayTransformSound();
        yield return new WaitForSeconds(GetClipDuration(bossTransformClip, transformFallbackDuration));

        currentFace = 2;
        currentHealth = face2MaxHealth;
        ApplyCurrentFaceAnimationClips();
        SetAnimatorBool(transformedParameter, true);
        PlayIdleAnimation();

        isTransforming = false;
        attackCooldownTimer = attackCooldown;
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isAttacking = false;
        isTransforming = false;
        isInvincible = false;

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

        SetAnimatorBool(deathParameter, false);
        PlayDeathAnimation();
        StopUnlockLoopSound();
        PlayDeathSound();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        float deathAnimationDuration = GetClipDuration(bossDieClip, deathFallbackDuration);
        if (deathCoroutine != null)
        {
            StopCoroutine(deathCoroutine);
        }

        deathCoroutine = StartCoroutine(DeathSequence(deathAnimationDuration));
    }

    IEnumerator DeathSequence(float deathAnimationDuration)
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        FreezeDeathAnimationAtEnd();

        if (deathDestroyDelay > 0f)
        {
            yield return new WaitForSeconds(deathDestroyDelay);
        }

        ShowWinUIAndPauseGame();
        PlayPostDestroySound();
        Destroy(gameObject);
    }

    void FreezeDeathAnimationAtEnd()
    {
        if (animator == null)
        {
            return;
        }

        if (TryPlayState(deathStateName, 0.999f) || TryPlayState("BossDIe", 0.999f))
        {
            animator.Update(0f);
        }

        animator.speed = 0f;
    }

    void PlayIdleAnimation()
    {
        ApplyCurrentFaceAnimationClips();
        if (TryPlayState(CurrentIdleStateName()))
        {
            return;
        }

        TryPlayState(sharedIdleStateName);
    }

    void PlayAttack1Animation()
    {
        ApplyCurrentFaceAnimationClips();
        if (TryPlayState(CurrentAttack1StateName()))
        {
            return;
        }

        TryPlayState(sharedAttack1StateName);
    }

    void PlayAttack2Animation()
    {
        ApplyCurrentFaceAnimationClips();
        if (TryPlayState(CurrentAttack2StateName()))
        {
            return;
        }

        TryPlayState(sharedAttack2StateName);
    }

    void PlayAttack3Animation()
    {
        ApplyCurrentFaceAnimationClips();
        if (TryPlayState(face1Attack3StateName))
        {
            return;
        }

        PlayClipOnIdleSlot(boss1Attack3Clip);
    }

    void PlayTransformAnimation()
    {
        if (TryPlayState(transformStateName))
        {
            return;
        }

        PlayClipOnIdleSlot(bossTransformClip);
    }

    void PlayDeathAnimation()
    {
        if (TryPlayState(deathStateName) || TryPlayState("BossDIe"))
        {
            return;
        }

        PlayClipOnIdleSlot(bossDieClip);
    }

    void PlayTransformSound()
    {
        PlaySound(transformSound, transformSoundVolume);
    }

    public void PlayAppearSound()
    {
        PlaySound(appearSound, appearSoundVolume);

        if (playUnlockLoopSound)
        {
            PlayUnlockLoopSound(unlockLoopSound, unlockLoopSoundVolume);
        }
    }

    void PlayDeathSound()
    {
        PlaySound(deathSound, deathSoundVolume);
    }

    void PlayPostDestroySound()
    {
        PlaySoundAtCamera(postDestroySound, postDestroySoundVolume);
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

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (enabled)
        {
            return;
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        isAttacking = false;
        isInvincible = false;
        wasPlayerInDetectionRange = false;
        attackCooldownTimer = attackCooldown;
        SetPosition(homePosition);
        FacePlayer();
        PlayIdleAnimation();
    }

    void PlayHitSound()
    {
        PlaySound(hitSound, hitSoundVolume);
    }

    void PlayTeleportSound()
    {
        PlaySound(teleportSound, teleportSoundVolume);
    }

    void PlayAttack1Sound()
    {
        if (currentFace == 1)
        {
            PlaySound(face1Attack1Sound, face1Attack1SoundVolume);
            return;
        }

        PlaySound(face2Attack1Sound, face2Attack1SoundVolume);
    }

    void PlayAttack2Sound()
    {
        if (currentFace == 1)
        {
            PlaySound(face1Attack2Sound, face1Attack2SoundVolume);
            return;
        }

        PlaySound(face2Attack2Sound, face2Attack2SoundVolume);
    }

    void PlayAttack3FireSound()
    {
        PlaySound(attack3FireSound, attack3FireSoundVolume);
    }

    void UpdateRandomLaugh(bool playerInDetectionRange)
    {
        if (!playerInDetectionRange)
        {
            wasPlayerInDetectionRange = false;
            laughTimer = 0f;
            return;
        }

        if (!wasPlayerInDetectionRange)
        {
            wasPlayerInDetectionRange = true;
            ScheduleNextLaugh();
            return;
        }

        laughTimer -= Time.deltaTime;
        if (laughTimer > 0f)
        {
            return;
        }

        PlayLaughSound();
        ScheduleNextLaugh();
    }

    void ScheduleNextLaugh()
    {
        laughTimer = Random.Range(minLaughInterval, maxLaughInterval);
    }

    void PlayLaughSound()
    {
        PlaySound(laughSound, laughSoundVolume);
    }

    void PlayUnlockLoopSound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        AudioSource source = GetUnlockLoopAudioSource();
        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.Play();
        isPlayingUnlockLoopSound = true;
    }

    void StopUnlockLoopSound()
    {
        if (!isPlayingUnlockLoopSound || unlockLoopAudioSource == null)
        {
            return;
        }

        unlockLoopAudioSource.Stop();
        unlockLoopAudioSource.clip = null;
        isPlayingUnlockLoopSound = false;
    }

    AudioSource GetUnlockLoopAudioSource()
    {
        if (unlockLoopAudioSource != null)
        {
            return unlockLoopAudioSource;
        }

        unlockLoopAudioSource = gameObject.AddComponent<AudioSource>();
        unlockLoopAudioSource.playOnAwake = false;
        unlockLoopAudioSource.spatialBlend = 0f;
        return unlockLoopAudioSource;
    }

    void PlaySoundAtCamera(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        GameObject soundObject = new GameObject("BossPostDestroySound");
        Camera mainCamera = Camera.main;
        soundObject.transform.position = mainCamera != null ? mainCamera.transform.position : transform.position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.Play();

        Destroy(soundObject, clip.length);
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
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

    void PlayClipOnIdleSlot(AnimationClip clip)
    {
        if (clip == null)
        {
            PlayIdleAnimation();
            return;
        }

        OverrideClipSlot("Idle", clip);
        if (TryPlayState(sharedIdleStateName))
        {
            return;
        }

        TryPlayState(CurrentIdleStateName());
    }

    void SetupAnimatorOverrideController()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        baseController = animator.runtimeAnimatorController;
        overrideController = baseController as AnimatorOverrideController;
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController);
            animator.runtimeAnimatorController = overrideController;
        }

        clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(clipOverrides);
    }

    void ApplyCurrentFaceAnimationClips()
    {
        if (overrideController == null)
        {
            return;
        }

        OverrideClipSlot("Idle", currentFace == 1 ? boss1IdleClip : boss2IdleClip);
        OverrideClipSlot("Attack1", currentFace == 1 ? boss1Attack1Clip : boss2Attack1Clip);
        OverrideClipSlot("Attack2", currentFace == 1 ? boss1Attack2Clip : boss2Attack2Clip);
        if (currentFace == 1)
        {
            OverrideClipSlot("Attack3", boss1Attack3Clip);
        }
    }

    void OverrideClipSlot(string slotName, AnimationClip replacementClip)
    {
        if (overrideController == null || replacementClip == null)
        {
            return;
        }

        if (clipOverrides == null)
        {
            clipOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        }

        overrideController.GetOverrides(clipOverrides);
        for (int i = 0; i < clipOverrides.Count; i++)
        {
            AnimationClip sourceClip = clipOverrides[i].Key;
            if (sourceClip == null || !sourceClip.name.Contains(slotName))
            {
                continue;
            }

            clipOverrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(sourceClip, replacementClip);
        }

        overrideController.ApplyOverrides(clipOverrides);
    }

    bool TryPlayState(string stateName)
    {
        return TryPlayState(stateName, 0f);
    }

    bool TryPlayState(string stateName, float normalizedTime)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        int stateHash = Animator.StringToHash(BaseLayerPrefix + stateName);
        if (!animator.HasState(0, stateHash))
        {
            return false;
        }

        animator.Play(stateHash, 0, normalizedTime);
        return true;
    }

    void SetAnimatorBool(string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }

    float GetAttack1Duration()
    {
        AnimationClip clip = currentFace == 1 ? boss1Attack1Clip : boss2Attack1Clip;
        return GetClipDuration(clip, attack1FallbackDuration);
    }

    float GetAttack2Duration()
    {
        AnimationClip clip = currentFace == 1 ? boss1Attack2Clip : boss2Attack2Clip;
        return GetClipDuration(clip, attack2FallbackDuration);
    }

    float GetAttack3Duration()
    {
        return GetClipDuration(boss1Attack3Clip, attack3FallbackDuration);
    }

    float GetClipDuration(AnimationClip clip, float fallbackDuration)
    {
        if (clip != null)
        {
            return Mathf.Max(0.01f, clip.length);
        }

        return Mathf.Max(0.01f, fallbackDuration);
    }

    int CurrentAttack1Damage()
    {
        return currentFace == 1 ? face1Attack1Damage : face2Attack1Damage;
    }

    int CurrentAttack2Damage()
    {
        return currentFace == 1 ? face1Attack2Damage : face2Attack2Damage;
    }

    int CurrentFireBreathCount()
    {
        return currentFace == 1 ? face1FireBreathCount : face2FireBreathCount;
    }

    string CurrentIdleStateName()
    {
        return currentFace == 1 ? face1IdleStateName : face2IdleStateName;
    }

    string CurrentAttack1StateName()
    {
        return currentFace == 1 ? face1Attack1StateName : face2Attack1StateName;
    }

    string CurrentAttack2StateName()
    {
        return currentFace == 1 ? face1Attack2StateName : face2Attack2StateName;
    }

    Vector2 GetPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    void SetPosition(Vector2 position)
    {
        if (rb != null && rb.simulated)
        {
            rb.position = position;
            return;
        }

        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    void FacePlayer()
    {
        if (playerTarget == null)
        {
            return;
        }

        FaceDirection((Vector2)playerTarget.position - GetPosition());
    }

    void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        float sign = Mathf.Sign(direction.x);
        float baseScale = startScaleX > 0f ? startScaleX : Mathf.Abs(scale.x);
        scale.x = baseScale * sign;
        transform.localScale = scale;
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
        spriteRenderer.color = hurtFlashColor;
        yield return new WaitForSeconds(hurtFlashDuration);
        spriteRenderer.color = startColor;
        hurtCoroutine = null;
    }

    void EnsureHitCollider()
    {
        if (!createHitColliderIfMissing || GetComponent<Collider2D>() != null)
        {
            return;
        }

        BoxCollider2D hitCollider = gameObject.AddComponent<BoxCollider2D>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            hitCollider.size = spriteRenderer.sprite.bounds.size;
            hitCollider.offset = spriteRenderer.sprite.bounds.center;
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

    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)homePosition : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attack1HitRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center + Vector3.left * sideStepDistance, center + Vector3.right * sideStepDistance);

        Vector2 fireForward = GetFireDirection();
        if (Application.isPlaying)
        {
            DrawFireBreathGizmo(fireForward, CurrentFireOriginOffset(), CurrentFireRange(), CurrentFireWidth(), new Color(1f, 0.45f, 0f));
        }
        else
        {
            DrawFireBreathGizmo(fireForward, face1FireOriginOffset, face1FireRange, face1FireWidth, new Color(1f, 0.45f, 0f));
            DrawFireBreathGizmo(fireForward, face2FireOriginOffset, face2FireRange, face2FireWidth, new Color(1f, 0.1f, 0.25f));
        }
    }

    void DrawFireBreathGizmo(Vector2 fireForward, Vector2 originOffset, float range, float width, Color color)
    {
        Gizmos.color = color;
        Vector2 fireCenter = GetFireOrigin(originOffset) + fireForward * range * 0.5f;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(fireCenter, Quaternion.Euler(0f, 0f, GetFireBoxAngle(fireForward)), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, range, 0f));
        Gizmos.matrix = previousMatrix;
    }

#if UNITY_EDITOR
    void AutoAssignBossClips()
    {
        boss1IdleClip = LoadBossClipIfMissing(boss1IdleClip, "Boss1Idle");
        boss1Attack1Clip = LoadBossClipIfMissing(boss1Attack1Clip, "Boss1Attack1");
        boss1Attack2Clip = LoadBossClipIfMissing(boss1Attack2Clip, "Boss1Attack2");
        boss1Attack3Clip = LoadBossClipIfMissing(boss1Attack3Clip, "Boss1Attack3");
        boss2IdleClip = LoadBossClipIfMissing(boss2IdleClip, "Boss2Idle");
        boss2Attack1Clip = LoadBossClipIfMissing(boss2Attack1Clip, "Boss2Attack1");
        boss2Attack2Clip = LoadBossClipIfMissing(boss2Attack2Clip, "Boss2Attack2");
        bossTransformClip = LoadBossClipIfMissing(bossTransformClip, "BossTranform");
        bossDieClip = LoadBossClipIfMissing(bossDieClip, "BossDie");
        bossDieClip = LoadBossClipIfMissing(bossDieClip, "BossDIe");
        bossProjectilePrefab = LoadBossProjectilePrefabIfMissing(bossProjectilePrefab);
    }

    AnimationClip LoadBossClipIfMissing(AnimationClip currentClip, string clipName)
    {
        if (currentClip != null)
        {
            return currentClip;
        }

        return AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Animation/Boss/{clipName}.anim");
    }

    BossProjectile LoadBossProjectilePrefabIfMissing(BossProjectile currentPrefab)
    {
        if (currentPrefab != null)
        {
            return currentPrefab;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossProjectile.prefab");
        return prefab != null ? prefab.GetComponent<BossProjectile>() : null;
    }
#endif
}
