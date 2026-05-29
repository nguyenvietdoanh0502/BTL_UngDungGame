using UnityEngine;
using UnityEngine.UI;

public class MissionCanvasHandler : MonoBehaviour
{
    public Text batText;
    public Text slimeText;
    public string batTextFormat = "So doi: {0}/{1}";
    public string slimeTextFormat = "So slime: {0}/{1}";
    public Vector2 textSize = new Vector2(95f, 14f);

    void Awake()
    {
        FindTextReferences();
    }

    void OnEnable()
    {
        EnemyKillBlockUnlocker.KillCountsChanged += UpdateMissionText;
        UpdateMissionText(EnemyKillBlockUnlocker.BatKills, EnemyKillBlockUnlocker.SlimeKills);
    }

    void OnDisable()
    {
        EnemyKillBlockUnlocker.KillCountsChanged -= UpdateMissionText;
    }

    void Start()
    {
        UpdateMissionText(EnemyKillBlockUnlocker.BatKills, EnemyKillBlockUnlocker.SlimeKills);
    }

    void FindTextReferences()
    {
        if (batText != null && slimeText != null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text == null)
            {
                continue;
            }

            string objectName = text.gameObject.name.ToLowerInvariant();
            if (batText == null && objectName.Contains("bat"))
            {
                batText = text;
            }
            else if (slimeText == null && objectName.Contains("slime"))
            {
                slimeText = text;
            }
        }
    }

    void UpdateMissionText(int batKills, int slimeKills)
    {
        FindTextReferences();

        if (batText != null)
        {
            PrepareText(batText);
            batText.text = string.Format(batTextFormat, batKills, EnemyKillBlockUnlocker.RequiredBatKills);
        }

        if (slimeText != null)
        {
            PrepareText(slimeText);
            slimeText.text = string.Format(slimeTextFormat, slimeKills, EnemyKillBlockUnlocker.RequiredSlimeKills);
        }
    }

    void PrepareText(Text text)
    {
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectTransform = text.rectTransform;
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = textSize;
        }
    }
}
