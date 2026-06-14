using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    const string GuardParameter = "isGuard";
    const string AttackParameter = "isAttack";
    const string HealingParameter = "Healing";
    const string IsHealingParameter = "isHealing";
    const string RunningParameter = "isRunning";
    const string HealingState = "Healing";
    const string GuardState = "Guard";
    const string IdleState = "Idle";
    const string RunState = "Run";
    const string DefaultHealBinding = "<Keyboard>/h";
    static readonly int HealingStateHash = Animator.StringToHash("Base Layer.Healing");
    static readonly string[] DefaultHealUseImageNames = { "Poison1", "Poison2", "Poison3" };

    public InputAction moveAction;
    public float speed = 5f;
    public int maxHealth=100;
    int currentHealth;
    public int getCurrentHealth()
    {
        return currentHealth;
    }
    public float maxStamina = 100f;
    public float guardStaminaCost = 15f;
    public float staminaRecoverPerSecond = 3f;
    float currentStamina;
    public float getCurrentStamina()
    {
        return currentStamina;
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
    AudioSource audioSource;
    AudioSource moveLoopAudioSource;
    public GameObject projectilePrefab;
    public InputAction LaunchAction;
    public InputAction GuardAction;
    public InputAction HealAction = new InputAction("Heal", InputActionType.Button, DefaultHealBinding);
    public int healAmount = 30;
    public float healingDuration = 1.5f;
    public int maxHealUses = 3;
    public GameObject[] healUseImages;
    public AudioClip moveLoopSound;
    [Range(0f, 1f)] public float moveLoopSoundVolume = 1f;
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackSoundVolume = 1f;
    public AudioClip guardBlockSound;
    [Range(0f, 1f)] public float guardBlockSoundVolume = 1f;
    public AudioClip healingSound;
    [Range(0f, 1f)] public float healingSoundVolume = 1f;
    public AudioClip hurtSound;
    [Range(0f, 1f)] public float hurtSoundVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathSoundVolume = 1f;

    [Header("Death UI")]
    public GameObject uiDeathCanvas;
    public string uiDeathCanvasName = "UIDeath";
    public bool showDeathUIOnDeath = true;
    public bool pauseGameOnDeath = true;

    public float guardDuration = 0.5f;
    public float guardInvincibleDuration = 0.8f;
    public float launchCooldown = 0.5f;
    public float launchDelay = 0f;
    float launchCooldownTimer;
    float attackFacingTimer;
    bool isGuarding;
    bool isHealing;
    bool isDead;
    bool controlsEnabled = true;
    int remainingHealUses;
    int healthBeforeHealing;
    float guardTimer;
    float guardInvincibleTimer;
    Coroutine healingCoroutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        moveAction.Enable();
        rigidbody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        EnsureHealAction();
        remainingHealUses = Mathf.Max(0, maxHealUses);
        EnsureHealUseImages();
        UpdateHealUseImages();
        FindUIDeathCanvasIfNeeded();
        if (uiDeathCanvas != null)
        {
            uiDeathCanvas.SetActive(false);
        }
        LaunchAction.Enable();
        GuardAction.Enable();
        HealAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
        {
            StopMoveLoopSound();
            return;
        }

        if (!controlsEnabled)
        {
            move = Vector2.zero;
            animator.SetBool(RunningParameter, false);
            StopMoveLoopSound();
            return;
        }

        move = moveAction.ReadValue<Vector2>();
        if (isHealing && move != Vector2.zero)
        {
            CancelHealing();
        }

        if (move != Vector2.zero)
        {
            animator.SetBool(RunningParameter, !isGuarding && !isHealing);
        }
        else
        {
            animator.SetBool(RunningParameter, false);
        }
        UpdateGuardTimer();
        UpdateGuardInvincibleTimer();
        RecoverStamina();
        if (attackFacingTimer > 0f)
        {
            attackFacingTimer -= Time.deltaTime;
        }
        if (attackFacingTimer <= 0f && move.x > 0) // Đang di chuyển sang phải
        {
            // Set scale X về 1 (giữ nguyên hướng gốc)
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (attackFacingTimer <= 0f && move.x < 0) // Đang di chuyển sang trái
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
        if (GuardAction.WasPressedThisFrame())
        {
            StartGuard();
        }
        if (HealAction.WasPressedThisFrame())
        {
            Heal();
        }
        UpdateMoveLoopSound(move != Vector2.zero && !isGuarding && !isHealing);
        if (isHealing)
        {
            return;
        }
        if (!isGuarding && LaunchAction.WasPressedThisFrame() && launchCooldownTimer <= 0f)
        {
            launchCooldownTimer = launchCooldown;
            if (TryFaceMouse())
            {
                attackFacingTimer = Mathf.Max(launchDelay, launchCooldown);
            }
            StartCoroutine(LaunchAfterDelay());
            animator.SetTrigger(AttackParameter);
            animator.SetFloat("random",(int)Random.Range(0,2));
            PlayAttackSound();
        }
    }
    void FixedUpdate()
    {
        if (isDead || !controlsEnabled)
        {
            return;
        }

        Vector2 position = (Vector2)rigidbody2D.position + move*speed*Time.deltaTime;
        rigidbody2D.position = position;
    }

    public void SetControlsEnabled(bool enabled)
    {
        if (isDead)
        {
            return;
        }

        controlsEnabled = enabled;

        if (enabled)
        {
            moveAction?.Enable();
            LaunchAction?.Enable();
            GuardAction?.Enable();
            EnsureHealAction();
            HealAction?.Enable();
            return;
        }

        move = Vector2.zero;
        moveAction?.Disable();
        LaunchAction?.Disable();
        GuardAction?.Disable();
        HealAction?.Disable();
        StopMoveLoopSound();
        CancelHealing();
        if (isGuarding)
        {
            StopGuard();
        }

        if (animator != null)
        {
            animator.SetBool(RunningParameter, false);
            animator.ResetTrigger(AttackParameter);
        }
    }

    public void changeHealth(int amount)
    {
        if (isDead)
        {
            return;
        }

        if (amount < 0)
        {
            if (isHealing)
            {
                CancelHealing();
            }

            if (guardInvincibleTimer > 0f)
            {
                PlayGuardBlockSound();
                return;
            }

            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCoolDown = timeInvincible;
            blinkTimer = 0f;
            blinkState = true;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        if (amount < 0 && currentHealth > 0)
        {
            PlayHurtSound();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        CancelHealing();
        move = Vector2.zero;
        moveAction.Disable();
        LaunchAction.Disable();
        GuardAction.Disable();
        HealAction.Disable();
        animator.SetBool(RunningParameter, false);
        StopMoveLoopSound();
        ShowDeathUIAndPauseGame();
        PlayDeathSound();
    }

    void OnDisable()
    {
        StopMoveLoopSound();
    }

    void EnsureHealAction()
    {
        if (HealAction == null)
        {
            HealAction = new InputAction("Heal", InputActionType.Button, DefaultHealBinding);
            return;
        }

        if (HealAction.bindings.Count == 0)
        {
            HealAction.AddBinding(DefaultHealBinding);
        }
    }

    void Heal()
    {
        if (isHealing || remainingHealUses <= 0 || currentHealth >= maxHealth || move != Vector2.zero)
        {
            return;
        }

        if (isGuarding)
        {
            StopGuard();
        }

        isHealing = true;
        healthBeforeHealing = currentHealth;
        healingCoroutine = StartCoroutine(HealOverTime());
        PlayHealingAnimation();
        PlayHealingSound();
    }

    void EnsureHealUseImages()
    {
        int imageCount = Mathf.Max(0, maxHealUses);
        if (healUseImages == null || healUseImages.Length != imageCount)
        {
            GameObject[] existingImages = healUseImages;
            healUseImages = new GameObject[imageCount];
            if (existingImages != null)
            {
                int copyCount = Mathf.Min(existingImages.Length, healUseImages.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    healUseImages[i] = existingImages[i];
                }
            }
        }

        for (int i = 0; i < healUseImages.Length; i++)
        {
            if (healUseImages[i] == null && i < DefaultHealUseImageNames.Length)
            {
                healUseImages[i] = FindSceneGameObject(DefaultHealUseImageNames[i]);
            }
        }
    }

    GameObject FindSceneGameObject(string objectName)
    {
        foreach (GameObject sceneObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (sceneObject.name == objectName && sceneObject.scene.IsValid())
            {
                return sceneObject;
            }
        }

        return null;
    }

    void UpdateHealUseImages()
    {
        if (healUseImages == null)
        {
            return;
        }

        for (int i = 0; i < healUseImages.Length; i++)
        {
            if (healUseImages[i] == null)
            {
                continue;
            }

            healUseImages[i].SetActive(i < remainingHealUses);
        }
    }

    IEnumerator HealOverTime()
    {
        float duration = Mathf.Max(0.01f, healingDuration);
        float elapsed = 0f;
        int startHealth = healthBeforeHealing;
        int targetHealth = Mathf.Min(maxHealth, startHealth + healAmount);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);
            currentHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, percent));
            yield return null;
        }

        currentHealth = targetHealth;
        FinishHealing();
    }

    void FinishHealing()
    {
        remainingHealUses = Mathf.Max(0, remainingHealUses - 1);
        UpdateHealUseImages();
        isHealing = false;
        healingCoroutine = null;
        StopHealingAnimation();
    }

    void CancelHealing()
    {
        if (!isHealing)
        {
            return;
        }

        if (healingCoroutine != null)
        {
            StopCoroutine(healingCoroutine);
            healingCoroutine = null;
        }

        isHealing = false;
        currentHealth = healthBeforeHealing;
        StopHealingAnimation();
    }

    void PlayHealingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackParameter);
        animator.SetBool(RunningParameter, false);

        if (animator.HasState(0, HealingStateHash))
        {
            animator.Play(HealingState, 0, 0f);
            return;
        }

        TrySetAnimatorTrigger(HealingParameter);
        TrySetAnimatorTrigger(IsHealingParameter);
    }

    void StopHealingAnimation()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(HealingState))
        {
            animator.CrossFade(move == Vector2.zero ? IdleState : RunState, 0.05f);
        }
    }

    bool TrySetAnimatorTrigger(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(parameterName);
                return true;
            }
        }

        return false;
    }

    IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);
        if (!controlsEnabled || isGuarding || isHealing)
        {
            yield break;
        }
        Launch();
    }

    void UpdateGuardTimer()
    {
        if (!isGuarding)
        {
            return;
        }

        guardTimer -= Time.deltaTime;
        if (guardTimer <= 0f)
        {
            StopGuard();
        }
    }

    void StartGuard()
    {
        if (isGuarding || isHealing || currentStamina < guardStaminaCost)
        {
            return;
        }

        currentStamina -= guardStaminaCost;
        isGuarding = true;
        guardTimer = guardDuration;
        guardInvincibleTimer = guardInvincibleDuration;
        launchCooldownTimer = Mathf.Max(launchCooldownTimer, guardDuration);
        animator.ResetTrigger(AttackParameter);
        animator.SetBool(RunningParameter, false);

        if (!SetGuardAnimatorParameter(true))
        {
            animator.Play(GuardState, 0, 0f);
        }
    }

    void StopGuard()
    {
        isGuarding = false;
        SetGuardAnimatorParameter(false);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(GuardState))
        {
            animator.CrossFade(move == Vector2.zero ? IdleState : RunState, 0.05f);
        }
    }

    bool SetGuardAnimatorParameter(bool active)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name != GuardParameter)
            {
                continue;
            }

            if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(GuardParameter, active);
            }
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                if (active)
                {
                    animator.SetTrigger(GuardParameter);
                }
                else
                {
                    animator.ResetTrigger(GuardParameter);
                }
            }

            return true;
        }

        return false;
    }

    void UpdateGuardInvincibleTimer()
    {
        if (guardInvincibleTimer <= 0f)
        {
            return;
        }

        guardInvincibleTimer -= Time.deltaTime;
    }

    void RecoverStamina()
    {
        if (currentStamina >= maxStamina)
        {
            return;
        }

        currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoverPerSecond * Time.deltaTime);
    }

    bool TryGetMouseWorldPosition(out Vector2 mouseWorldPosition)
    {
        mouseWorldPosition = Vector2.zero;
        Camera mainCamera = Camera.main;
        if (mainCamera == null || Mouse.current == null)
        {
            return false;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        return true;
    }

    void PlayAttackSound()
    {
        if (attackSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(attackSound, attackSoundVolume);
    }

    void PlayGuardBlockSound()
    {
        if (guardBlockSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(guardBlockSound, guardBlockSoundVolume);
    }

    void PlayHealingSound()
    {
        if (healingSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(healingSound, healingSoundVolume);
    }

    void PlayHurtSound()
    {
        if (hurtSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(hurtSound, hurtSoundVolume);
    }

    void PlayDeathSound()
    {
        if (deathSound == null || audioSource == null)
        {
            return;
        }

        audioSource.ignoreListenerPause = true;
        audioSource.PlayOneShot(deathSound, deathSoundVolume);
    }

    void ShowDeathUIAndPauseGame()
    {
        GameAudioUtility.StopAllAudioSources();

        if (showDeathUIOnDeath)
        {
            FindUIDeathCanvasIfNeeded();
            if (uiDeathCanvas != null)
            {
                uiDeathCanvas.SetActive(true);
            }
        }

        if (pauseGameOnDeath)
        {
            Time.timeScale = 0f;
        }
    }

    void UpdateMoveLoopSound(bool shouldPlay)
    {
        if (!shouldPlay || moveLoopSound == null)
        {
            StopMoveLoopSound();
            return;
        }

        AudioSource source = GetMoveLoopAudioSource();
        if (source.clip != moveLoopSound)
        {
            source.clip = moveLoopSound;
        }

        source.volume = moveLoopSoundVolume;
        source.loop = true;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    void StopMoveLoopSound()
    {
        if (moveLoopAudioSource != null && moveLoopAudioSource.isPlaying)
        {
            moveLoopAudioSource.Stop();
        }
    }

    AudioSource GetMoveLoopAudioSource()
    {
        if (moveLoopAudioSource != null)
        {
            return moveLoopAudioSource;
        }

        moveLoopAudioSource = gameObject.AddComponent<AudioSource>();
        moveLoopAudioSource.playOnAwake = false;
        moveLoopAudioSource.spatialBlend = 0f;
        moveLoopAudioSource.loop = true;
        return moveLoopAudioSource;
    }

    void FindUIDeathCanvasIfNeeded()
    {
        if (uiDeathCanvas != null)
        {
            return;
        }

        uiDeathCanvas = FindSceneGameObject(uiDeathCanvasName);
        if (uiDeathCanvas == null && uiDeathCanvasName != "UIDeath")
        {
            uiDeathCanvas = FindSceneGameObject("UIDeath");
        }
    }

    void FaceToward(Vector2 worldPosition)
    {
        float directionX = worldPosition.x - rigidbody2D.position.x;
        if (Mathf.Abs(directionX) <= 0.001f)
        {
            return;
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(directionX);
        transform.localScale = scale;
    }

    bool TryFaceMouse()
    {
        if (!TryGetMouseWorldPosition(out Vector2 mouseWorldPosition))
        {
            return false;
        }

        FaceToward(mouseWorldPosition);
        return true;
    }

    void Launch()
    {
        if (!controlsEnabled)
        {
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector2 mouseWorldPosition))
        {
            return;
        }

        FaceToward(mouseWorldPosition);

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
    public void RefillPotions()
    {
        // Đặt lại số bình máu bằng với mức tối đa (maxHealUses = 3)
        remainingHealUses = maxHealUses;

        // Cập nhật lại UI hiển thị 3 bình máu trên màn hình
        UpdateHealUseImages();
    }
}
