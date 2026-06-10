using UnityEngine;
using UnityEngine.UI;

public class AgisBossUIHandle : MonoBehaviour
{
    [SerializeField] AgisBossController boss;
    [SerializeField] Image healthFillImage;
    [SerializeField] Text healthText;
    [SerializeField] Text bossNameText;
    [SerializeField] bool autoFindBoss = true;
    [SerializeField] bool autoFindHealthImage = true;
    [SerializeField] bool useImageFillAmount = true;
    [SerializeField] bool hideWhenBossMissing = true;
    [SerializeField] string bossDisplayName = "Agis";

    Canvas bossCanvas;
    GraphicRaycaster graphicRaycaster;

    void Awake()
    {
        bossCanvas = GetComponent<Canvas>();
        graphicRaycaster = GetComponent<GraphicRaycaster>();
        FindReferencesIfNeeded();
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
            boss = FindFirstObjectByType<AgisBossController>();
        }

        if (healthFillImage == null && autoFindHealthImage)
        {
            healthFillImage = FindBestHealthFillImage();
        }

        if (healthFillImage != null && useImageFillAmount)
        {
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        if (bossNameText == null)
        {
            bossNameText = FindBossNameText();
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

    Text FindBossNameText()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.name.ToLowerInvariant().Contains("name"))
            {
                return text;
            }
        }

        return null;
    }

    void UpdateHealthBar()
    {
        if (boss == null || !boss.gameObject.activeInHierarchy)
        {
            SetVisible(!hideWhenBossMissing);
            return;
        }

        int maxHealth = Mathf.Max(1, boss.GetMaxHealth());
        int currentHealth = Mathf.Clamp(boss.GetCurrentHealth(), 0, maxHealth);
        float percent = (float)currentHealth / maxHealth;

        SetVisible(true);

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Clamp01(percent);
        }

        if (healthText != null)
        {
            healthText.text = currentHealth + " / " + maxHealth;
        }

        if (bossNameText != null)
        {
            bossNameText.text = bossDisplayName;
        }
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
