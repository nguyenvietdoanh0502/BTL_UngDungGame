using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuHandle : MonoBehaviour
{
    [SerializeField] string playButtonName = "Play";
    [SerializeField] Color hoverBackgroundColor = new Color(0.18f, 0.62f, 0.86f, 0.55f);
    [SerializeField] float buttonFadeDuration = 0.1f;
    [SerializeField] RectTransform boxLevelPanel;
    [SerializeField] string boxLevelName = "BoxLevel";
    [SerializeField] Vector2 boxLevelSlideOffset = new Vector2(1920f, 0f);
    [SerializeField] float boxLevelSlideDuration = 0.25f;
    [SerializeField] bool hideBoxLevelOnStart = true;
    [SerializeField] AudioSource menuAudioSource;
    [SerializeField] AudioClip buttonHoverSound;
    [SerializeField] AudioClip buttonClickSound;
    [SerializeField, Range(0f, 1f)] float hoverSoundVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] float clickSoundVolume = 0.9f;

    Vector2 boxLevelShownPosition;
    Coroutine boxLevelSlideRoutine;
    bool isBoxLevelShown;

    void Start()
    {
        EnsureAudioSource();
        ResolveBoxLevelPanel();
        SetupBoxLevelStartState();
        ConfigureMenuButtons();
        ConnectPlayButton();
    }

    public void Play()
    {
        PlayClickSound();
        ShowBoxLevel();
    }

    public void PlayGame()
    {
        Play();
    }

    void ConfigureMenuButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Image> hoverBackgrounds = FindHoverBackgroundImages(buttons);
        HashSet<Image> usedBackgrounds = new HashSet<Image>();

        foreach (Button button in buttons)
        {
            if (button.targetGraphic == null)
            {
                continue;
            }

            button.transition = Selectable.Transition.None;
            button.interactable = true;
            button.targetGraphic.color = Color.clear;
            button.targetGraphic.CrossFadeColor(Color.clear, 0f, true, true);
            button.onClick.RemoveListener(PlayClickSound);

            if (button.name != playButtonName)
            {
                button.onClick.AddListener(PlayClickSound);
            }

            Image hoverBackground = FindClosestHoverBackground(button, hoverBackgrounds, usedBackgrounds);

            if (hoverBackground == null)
            {
                continue;
            }

            usedBackgrounds.Add(hoverBackground);

            MenuButtonHoverEffect hoverEffect = button.GetComponent<MenuButtonHoverEffect>();

            if (hoverEffect == null)
            {
                hoverEffect = button.gameObject.AddComponent<MenuButtonHoverEffect>();
            }

            hoverEffect.Initialize(hoverBackground, hoverBackgroundColor, buttonFadeDuration, menuAudioSource, buttonHoverSound, hoverSoundVolume);
        }
    }

    void EnsureAudioSource()
    {
        if (menuAudioSource == null)
        {
            menuAudioSource = GetComponent<AudioSource>();
        }

        if (menuAudioSource == null)
        {
            menuAudioSource = gameObject.AddComponent<AudioSource>();
        }

        menuAudioSource.playOnAwake = false;
    }

    void ResolveBoxLevelPanel()
    {
        if (boxLevelPanel != null)
        {
            return;
        }

        RectTransform[] rectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform.gameObject.name == boxLevelName && rectTransform.gameObject.scene.IsValid())
            {
                boxLevelPanel = rectTransform;
                return;
            }
        }
    }

    void SetupBoxLevelStartState()
    {
        if (boxLevelPanel == null)
        {
            return;
        }

        boxLevelShownPosition = boxLevelPanel.anchoredPosition;

        if (hideBoxLevelOnStart)
        {
            boxLevelPanel.gameObject.SetActive(false);
            isBoxLevelShown = false;
        }
        else
        {
            isBoxLevelShown = boxLevelPanel.gameObject.activeSelf;
        }
    }

    void ShowBoxLevel()
    {
        if (boxLevelPanel == null)
        {
            ResolveBoxLevelPanel();
        }

        if (boxLevelPanel == null || isBoxLevelShown)
        {
            return;
        }

        if (boxLevelSlideRoutine != null)
        {
            StopCoroutine(boxLevelSlideRoutine);
        }

        boxLevelPanel.gameObject.SetActive(true);
        boxLevelPanel.anchoredPosition = boxLevelShownPosition + boxLevelSlideOffset;
        boxLevelSlideRoutine = StartCoroutine(SlideBoxLevel(boxLevelShownPosition + boxLevelSlideOffset, boxLevelShownPosition));
    }

    IEnumerator SlideBoxLevel(Vector2 startPosition, Vector2 endPosition)
    {
        float elapsed = 0f;

        while (elapsed < boxLevelSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = boxLevelSlideDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / boxLevelSlideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            boxLevelPanel.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            yield return null;
        }

        boxLevelPanel.anchoredPosition = endPosition;
        boxLevelSlideRoutine = null;
        isBoxLevelShown = true;
    }

    void PlayClickSound()
    {
        PlayClickSound(false);
    }

    void PlayClickSound(bool persistAcrossScene)
    {
        if (buttonClickSound == null)
        {
            return;
        }

        if (persistAcrossScene)
        {
            GameObject soundPlayer = new GameObject("Menu Click Sound");
            DontDestroyOnLoad(soundPlayer);

            AudioSource source = soundPlayer.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.PlayOneShot(buttonClickSound, clickSoundVolume);
            Destroy(soundPlayer, buttonClickSound.length + 0.1f);
            return;
        }

        if (menuAudioSource != null)
        {
            menuAudioSource.PlayOneShot(buttonClickSound, clickSoundVolume);
        }
    }

    List<Image> FindHoverBackgroundImages(Button[] buttons)
    {
        HashSet<Image> buttonImages = new HashSet<Image>();

        foreach (Button button in buttons)
        {
            if (button.targetGraphic is Image image)
            {
                buttonImages.Add(image);
            }
        }

        List<Image> hoverBackgrounds = new List<Image>();
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (buttonImages.Contains(image) || image.transform.parent != transform || image.gameObject.name == "Background")
            {
                continue;
            }

            if (!image.gameObject.activeSelf || image.gameObject.name.StartsWith("Image"))
            {
                hoverBackgrounds.Add(image);
            }
        }

        return hoverBackgrounds;
    }

    Image FindClosestHoverBackground(Button button, List<Image> hoverBackgrounds, HashSet<Image> usedBackgrounds)
    {
        RectTransform buttonRect = button.transform as RectTransform;
        Image closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (Image hoverBackground in hoverBackgrounds)
        {
            if (usedBackgrounds.Contains(hoverBackground))
            {
                continue;
            }

            RectTransform backgroundRect = hoverBackground.transform as RectTransform;

            if (buttonRect == null || backgroundRect == null)
            {
                continue;
            }

            float distance = Mathf.Abs(buttonRect.anchoredPosition.y - backgroundRect.anchoredPosition.y);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hoverBackground;
            }
        }

        return closest;
    }

    void ConnectPlayButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            if (button.name != playButtonName)
            {
                continue;
            }

            button.onClick.RemoveListener(Play);
            button.onClick.AddListener(Play);
            return;
        }
    }
}
