using UnityEngine;
using UnityEngine.UI;

public class BossUIHandle : MonoBehaviour
{
    [SerializeField] BossController boss;
    [SerializeField] Image healthFillImage;
    [SerializeField] Slider healthSlider;
    [SerializeField] Text healthText;
    [SerializeField] Text faceText;
    [SerializeField] bool autoFindBoss = true;
    [SerializeField] bool autoFindHealthImage = true;
    [SerializeField] bool useImageFillAmount = true;
    [SerializeField] bool hideWhenBossMissing = true;

    RectTransform healthFillRect;
    Canvas bossCanvas;
    GraphicRaycaster graphicRaycaster;
    float originalFillWidth;

    void Awake()
    {
        bossCanvas = GetComponent<Canvas>();
        graphicRaycaster = GetComponent<GraphicRaycaster>();
        FindReferencesIfNeeded();
        CacheOriginalFillWidth();
        UpdateHealthBar();
    }

    void Update()
    {
        FindReferencesIfNeeded();
        UpdateHealthBar();
    }

    void FindReferencesIfNeeded()
    {
        if (boss == null && autoFindBoss)
        {
            boss = FindFirstObjectByType<BossController>();
        }

        if (healthFillImage == null && autoFindHealthImage)
        {
            healthFillImage = FindBestHealthFillImage();
        }

        if (healthFillImage != null && healthFillRect == null)
        {
            healthFillRect = healthFillImage.rectTransform;
            ConfigureFillImage(healthFillImage);
            CacheOriginalFillWidth();
        }
    }

    Image FindBestHealthFillImage()
    {
        Image bestImage = null;
        int bestScore = 0;
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            int score = GetHealthImageScore(image.name);
            if (score > bestScore)
            {
                bestScore = score;
                bestImage = image;
            }
        }

        return bestImage;
    }

    int GetHealthImageScore(string imageName)
    {
        string lowerName = imageName.ToLowerInvariant();
        int score = 0;

        if (lowerName.Contains("boss"))
        {
            score += 80;
        }

        if (lowerName.Contains("health") || lowerName.Contains("hp"))
        {
            score += 80;
        }

        if (lowerName.Contains("bar") || lowerName.Contains("fill"))
        {
            score += 100;
        }

        if (lowerName.Contains("back") || lowerName.Contains("background") || lowerName.Contains("bg") || lowerName.Contains("frame"))
        {
            score -= 200;
        }

        return score;
    }

    void ConfigureFillImage(Image fillImage)
    {
        if (fillImage == null || !useImageFillAmount)
        {
            return;
        }

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    void CacheOriginalFillWidth()
    {
        if (healthFillRect == null || originalFillWidth > 0f)
        {
            return;
        }

        originalFillWidth = healthFillRect.rect.width;
        if (originalFillWidth <= 0f)
        {
            originalFillWidth = healthFillRect.sizeDelta.x;
        }

        if (!useImageFillAmount)
        {
            KeepFillLeftEdgeFixed(healthFillRect, originalFillWidth);
        }
    }

    void KeepFillLeftEdgeFixed(RectTransform fillRect, float originalWidth)
    {
        if (fillRect == null || Mathf.Approximately(fillRect.pivot.x, 0f))
        {
            return;
        }

        Vector2 anchoredPosition = fillRect.anchoredPosition;
        anchoredPosition.x -= originalWidth * fillRect.pivot.x;

        Vector2 pivot = fillRect.pivot;
        pivot.x = 0f;

        fillRect.pivot = pivot;
        fillRect.anchoredPosition = anchoredPosition;
    }

    void UpdateHealthBar()
    {
        if (boss == null || !boss.gameObject.activeInHierarchy)
        {
            SetVisible(!hideWhenBossMissing);
            return;
        }

        int maxHealth = Mathf.Max(1, GetBossMaxHealth());
        int currentHealth = Mathf.Clamp(boss.GetCurrentHealth(), 0, maxHealth);
        float percent = (float)currentHealth / maxHealth;

        SetVisible(true);

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        SetFillPercent(percent);

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }

        if (faceText != null)
        {
            faceText.text = "Face " + boss.GetCurrentFace();
        }
    }

    int GetBossMaxHealth()
    {
        if (boss.GetCurrentFace() == 2)
        {
            return boss.face2MaxHealth;
        }

        return boss.face1MaxHealth;
    }

    void SetFillPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);

        if (healthFillImage == null)
        {
            return;
        }

        if (useImageFillAmount)
        {
            healthFillImage.fillAmount = percent;
            return;
        }

        if (healthFillRect == null)
        {
            return;
        }

        Vector2 sizeDelta = healthFillRect.sizeDelta;
        sizeDelta.x = originalFillWidth * percent;
        healthFillRect.sizeDelta = sizeDelta;
    }

    void SetVisible(bool visible)
    {
        if (bossCanvas != null)
        {
            bossCanvas.enabled = visible;
        }

        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = visible;
        }
    }
}
