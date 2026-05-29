using UnityEngine;
using UnityEngine.UI;

public class UIHealthBarHandle : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] Image healthFillImage;
    [SerializeField] Slider healthSlider;
    [SerializeField] Text healthText;
    [SerializeField] Image staminaFillImage;
    [SerializeField] Slider staminaSlider;
    [SerializeField] Text staminaText;
    [SerializeField] bool autoFindPlayer = true;
    [SerializeField] bool autoFindHealthImage = true;
    [SerializeField] bool useImageFillAmount = true;

    RectTransform healthFillRect;
    RectTransform staminaFillRect;
    float originalFillWidth;
    float originalStaminaFillWidth;

    void Awake()
    {
        FindReferencesIfNeeded();
        CacheOriginalFillWidth();
        CacheOriginalStaminaFillWidth();
        UpdateHealthBar();
        UpdateStaminaBar();
    }

    void Update()
    {
        FindReferencesIfNeeded();
        UpdateHealthBar();
        UpdateStaminaBar();
    }

    void FindReferencesIfNeeded()
    {
        if (player == null && autoFindPlayer)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (healthFillImage == null && autoFindHealthImage)
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

            healthFillImage = bestImage;
        }

        if (healthFillImage != null && healthFillRect == null)
        {
            healthFillRect = healthFillImage.rectTransform;
            ConfigureFillImage(healthFillImage);
            CacheOriginalFillWidth();
        }

        if (staminaFillImage != null && staminaFillRect == null)
        {
            staminaFillRect = staminaFillImage.rectTransform;
            ConfigureFillImage(staminaFillImage);
            CacheOriginalStaminaFillWidth();
        }
    }

    int GetHealthImageScore(string imageName)
    {
        string lowerName = imageName.ToLowerInvariant();
        int score = 0;

        if (lowerName.Contains("fill"))
        {
            score += 100;
        }

        if (lowerName.Contains("bar"))
        {
            score += 80;
        }

        if (lowerName.Contains("hp"))
        {
            score += 60;
        }

        if (lowerName.Contains("health"))
        {
            score += 50;
        }

        if (lowerName.Contains("back") || lowerName.Contains("background") || lowerName.Contains("bg") || lowerName.Contains("frame"))
        {
            score -= 100;
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
        if (healthFillRect != null && originalFillWidth <= 0f)
        {
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
    }

    void CacheOriginalStaminaFillWidth()
    {
        if (staminaFillRect != null && originalStaminaFillWidth <= 0f)
        {
            originalStaminaFillWidth = staminaFillRect.rect.width;

            if (originalStaminaFillWidth <= 0f)
            {
                originalStaminaFillWidth = staminaFillRect.sizeDelta.x;
            }

            if (!useImageFillAmount)
            {
                KeepFillLeftEdgeFixed(staminaFillRect, originalStaminaFillWidth);
            }
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

    void SetFillPercent(Image fillImage, RectTransform fillRect, float originalWidth, float percent)
    {
        if (fillImage == null)
        {
            return;
        }

        if (useImageFillAmount)
        {
            fillImage.fillAmount = percent;
            return;
        }

        if (fillRect == null)
        {
            return;
        }

        Vector2 sizeDelta = fillRect.sizeDelta;
        sizeDelta.x = originalWidth * percent;
        fillRect.sizeDelta = sizeDelta;
    }

    void UpdateHealthBar()
    {
        if (player == null)
        {
            return;
        }

        int maxHealth = Mathf.Max(1, player.maxHealth);
        int currentHealth = Mathf.Clamp(player.getCurrentHealth(), 0, maxHealth);
        float percent = (float)currentHealth / maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFillImage != null)
        {
            SetFillPercent(healthFillImage, healthFillRect, originalFillWidth, percent);
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }
    }

    void UpdateStaminaBar()
    {
        if (player == null)
        {
            return;
        }

        float maxStamina = Mathf.Max(1f, player.maxStamina);
        float currentStamina = Mathf.Clamp(player.getCurrentStamina(), 0f, maxStamina);
        float percent = currentStamina / maxStamina;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        if (staminaFillImage != null)
        {
            SetFillPercent(staminaFillImage, staminaFillRect, originalStaminaFillWidth, percent);
        }

        if (staminaText != null)
        {
            staminaText.text = Mathf.CeilToInt(currentStamina) + " / " + Mathf.CeilToInt(maxStamina);
        }
    }
}
