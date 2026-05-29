using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    const string GuardParameter = "isGuard";
    const string AttackParameter = "isAttack";
    const string RunningParameter = "isRunning";
    const string GuardState = "Guard";
    const string IdleState = "Idle";
    const string RunState = "Run";

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
    public GameObject projectilePrefab;
    public InputAction LaunchAction;
    public InputAction GuardAction;
    public AudioClip attackSound;
    [Range(0f, 1f)] public float attackSoundVolume = 1f;
    public AudioClip guardBlockSound;
    [Range(0f, 1f)] public float guardBlockSoundVolume = 1f;
    public float guardDuration = 0.5f;
    public float guardInvincibleDuration = 0.8f;
    public float launchCooldown = 0.5f;
    public float launchDelay = 0f;
    float launchCooldownTimer;
    float attackFacingTimer;
    bool isGuarding;
    bool isDead;
    float guardTimer;
    float guardInvincibleTimer;
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
        LaunchAction.Enable();
        GuardAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
        {
            return;
        }

        move = moveAction.ReadValue<Vector2>();
        if (move != Vector2.zero)
        {
            animator.SetBool(RunningParameter, !isGuarding);
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
        if (isDead)
        {
            return;
        }

        Vector2 position = (Vector2)rigidbody2D.position + move*speed*Time.deltaTime;
        rigidbody2D.position = position;
    }
    public void changeHealth(int amount)
    {
        if (isDead)
        {
            return;
        }

        if (amount < 0)
        {
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
        move = Vector2.zero;
        moveAction.Disable();
        LaunchAction.Disable();
        GuardAction.Disable();
        animator.SetBool(RunningParameter, false);
        Time.timeScale = 0f;
    }

    IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);
        if (isGuarding)
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
        if (isGuarding || currentStamina < guardStaminaCost)
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
}
