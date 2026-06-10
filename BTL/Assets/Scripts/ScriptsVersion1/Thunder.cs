using UnityEngine;

public class Thunder : MonoBehaviour
{
    [Header("Damage")]
    public int damageAmount = 30;
    public float strikeStartTime = 0.52f;
    public float strikeActiveDuration = 0.35f;
    public float damageCooldown = 1f;
    public bool damageOnlyOnce = true;

    [Header("Ellipse Hitbox")]
    public Vector2 damageEllipseOffset = new Vector2(0f, -0.85f);
    public Vector2 damageEllipseSize = new Vector2(2.05f, 0.55f);
    public float hitPadding = 0.05f;
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.8f);

    [Header("Audio")]
    public AudioClip thunderSound;
    [Range(0f, 1f)] public float thunderSoundVolume = 1f;
    public bool playSoundOnStrike = true;

    ContactFilter2D playerHitFilter;
    readonly Collider2D[] hitResults = new Collider2D[16];
    PlayerController playerController;
    Animator animator;
    AudioSource audioSource;
    bool hasDamagedPlayer;
    bool strikeTriggeredThisPlayback;
    float nextDamageTime;
    int currentStateHash;
    float lastNormalizedTime;

    void Awake()
    {
        playerHitFilter = ContactFilter2D.noFilter;
        playerHitFilter.useTriggers = true;
        playerController = FindFirstObjectByType<PlayerController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        UpdateStrikeDamageWindow();
    }

    void OnValidate()
    {
        strikeStartTime = Mathf.Max(0f, strikeStartTime);
        strikeActiveDuration = Mathf.Max(0.01f, strikeActiveDuration);
        damageCooldown = Mathf.Max(0f, damageCooldown);
        damageEllipseSize.x = Mathf.Max(0.01f, damageEllipseSize.x);
        damageEllipseSize.y = Mathf.Max(0.01f, damageEllipseSize.y);
        hitPadding = Mathf.Max(0f, hitPadding);
    }

    void UpdateStrikeDamageWindow()
    {
        if (animator == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = stateInfo.fullPathHash;
        float normalizedTime = stateInfo.normalizedTime;
        if (stateHash != currentStateHash || normalizedTime < lastNormalizedTime)
        {
            ResetStrikePlayback(stateHash);
        }

        lastNormalizedTime = normalizedTime;

        if (!stateInfo.loop && normalizedTime >= 1f)
        {
            return;
        }

        float animationTime = GetAnimationTime(stateInfo, normalizedTime);
        bool isStrikeActive = animationTime >= strikeStartTime && animationTime <= strikeStartTime + strikeActiveDuration;

        if (!isStrikeActive)
        {
            return;
        }

        if (!strikeTriggeredThisPlayback)
        {
            strikeTriggeredThisPlayback = true;
            hasDamagedPlayer = false;
            if (playSoundOnStrike)
            {
                PlayThunderSound();
            }
        }

        TryDamagePlayerInEllipse();
    }

    void ResetStrikePlayback(int stateHash)
    {
        currentStateHash = stateHash;
        lastNormalizedTime = 0f;
        hasDamagedPlayer = false;
        strikeTriggeredThisPlayback = false;
    }

    float GetAnimationTime(AnimatorStateInfo stateInfo, float normalizedTime)
    {
        if (stateInfo.loop)
        {
            return Mathf.Repeat(normalizedTime, 1f) * stateInfo.length;
        }

        return Mathf.Clamp01(normalizedTime) * stateInfo.length;
    }

    public void TryDamagePlayerInEllipse()
    {
        if (damageAmount <= 0 || Time.time < nextDamageTime)
        {
            return;
        }

        if (damageOnlyOnce && hasDamagedPlayer)
        {
            return;
        }

        PlayerController hitPlayer = FindPlayerInEllipse();
        if (hitPlayer == null)
        {
            return;
        }

        hitPlayer.changeHealth(-damageAmount);
        hasDamagedPlayer = true;
        nextDamageTime = Time.time + damageCooldown;
    }

    public void DealDamageNow()
    {
        TryDamagePlayerInEllipse();
    }

    public void PlayThunderSound()
    {
        if (thunderSound == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(thunderSound, thunderSoundVolume);
    }

    PlayerController FindPlayerInEllipse()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        int hitCount = Physics2D.OverlapBox(GetEllipseCenter(), GetEllipseBroadphaseSize(), transform.eulerAngles.z, playerHitFilter, hitResults);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitResults[i];
            if (hitCollider == null)
            {
                continue;
            }

            PlayerController hitPlayer = hitCollider.GetComponentInParent<PlayerController>();
            if (hitPlayer != null && IsColliderTouchingEllipse(hitCollider))
            {
                return hitPlayer;
            }
        }

        if (playerController != null && IsWorldPointInsideEllipse(playerController.transform.position))
        {
            return playerController;
        }

        return null;
    }

    bool IsColliderTouchingEllipse(Collider2D hitCollider)
    {
        Bounds bounds = hitCollider.bounds;
        Vector2 ellipseCenter = GetEllipseCenter();

        if (IsWorldPointInsideEllipse(bounds.center) || IsWorldPointInsideEllipse(hitCollider.ClosestPoint(ellipseCenter)))
        {
            return true;
        }

        return IsWorldPointInsideEllipse(new Vector2(bounds.min.x, bounds.min.y))
            || IsWorldPointInsideEllipse(new Vector2(bounds.min.x, bounds.max.y))
            || IsWorldPointInsideEllipse(new Vector2(bounds.max.x, bounds.min.y))
            || IsWorldPointInsideEllipse(new Vector2(bounds.max.x, bounds.max.y));
    }

    bool IsWorldPointInsideEllipse(Vector2 worldPoint)
    {
        Vector2 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector2 localOffset = localPoint - damageEllipseOffset;
        Vector2 radius = GetLocalEllipseRadius();

        float normalizedX = localOffset.x / radius.x;
        float normalizedY = localOffset.y / radius.y;
        return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
    }

    Vector2 GetEllipseCenter()
    {
        return transform.TransformPoint(damageEllipseOffset);
    }

    Vector2 GetLocalEllipseRadius()
    {
        return damageEllipseSize * 0.5f + Vector2.one * hitPadding;
    }

    Vector2 GetEllipseBroadphaseSize()
    {
        Vector2 localSize = damageEllipseSize + Vector2.one * hitPadding * 2f;
        Vector3 scale = transform.lossyScale;
        return new Vector2(localSize.x * Mathf.Abs(scale.x), localSize.y * Mathf.Abs(scale.y));
    }

    void OnDrawGizmosSelected()
    {
        const int segmentCount = 64;

        Gizmos.color = gizmoColor;
        Vector3 previousPoint = GetEllipseGizmoPoint(segmentCount - 1, segmentCount);
        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 nextPoint = GetEllipseGizmoPoint(i, segmentCount);
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }

    Vector3 GetEllipseGizmoPoint(int index, int segmentCount)
    {
        float angle = (float)index / segmentCount * Mathf.PI * 2f;
        Vector2 radius = GetLocalEllipseRadius();
        Vector2 localPoint = damageEllipseOffset + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
        return transform.TransformPoint(localPoint);
    }
}
