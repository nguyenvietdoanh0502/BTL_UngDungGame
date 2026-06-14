using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtonGroupHoverSound : MonoBehaviour
{
    [SerializeField] string firstMapButtonName = "First Map";
    [SerializeField] string secondMapButtonName = "Second Map";
    [SerializeField] string thirdMapButtonName = "Third Map";
    [SerializeField] string tutorialButtonName = "Tutorial";
    [SerializeField] string baseButtonName = "BaseMap";
    [SerializeField] string firstMapSceneName = "FirstMap";
    [SerializeField] string secondMapSceneName = "SecondMap";
    [SerializeField] string thirdMapSceneName = "SampleScene";
    [SerializeField] string tutorialSceneName = "TutorialScene";
    [SerializeField] string baseSceneName = "BaseMap";
    [SerializeField] Color hoverBackgroundColor = new Color(0.18f, 0.62f, 0.86f, 0.55f);
    [SerializeField] float hoverFadeDuration = 0.1f;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hoverSound;
    [SerializeField] AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] float hoverSoundVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] float clickSoundVolume = 0.9f;
    [SerializeField] bool hideButtonTargetGraphic = true;

    void OnEnable()
    {
        ConfigureButtons();
    }

    void ConfigureButtons()
    {
        EnsureAudioSource();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Image> hoverBackgrounds = FindHoverBackgroundImages(buttons);
        HashSet<Image> usedBackgrounds = new HashSet<Image>();

        foreach (Button button in buttons)
        {
            button.transition = Selectable.Transition.None;
            button.interactable = true;

            if (hideButtonTargetGraphic && button.targetGraphic != null)
            {
                button.targetGraphic.color = Color.clear;
                button.targetGraphic.CrossFadeColor(Color.clear, 0f, true, true);
            }

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.RemoveListener(LoadFirstMap);
            button.onClick.RemoveListener(LoadSecondMap);
            button.onClick.RemoveListener(LoadThirdMap);
            button.onClick.RemoveListener(LoadTutorial);
            button.onClick.RemoveListener(LoadBase);

            if (button.name == firstMapButtonName)
            {
                button.onClick.AddListener(LoadFirstMap);
            }
            else if (button.name == secondMapButtonName)
            {
                button.onClick.AddListener(LoadSecondMap);
            }
            else if (button.name == thirdMapButtonName)
            {
                button.onClick.AddListener(LoadThirdMap);
            }
            else if (button.name == tutorialButtonName)
            {
                button.onClick.AddListener(LoadTutorial);
            }
            else if(button.name == baseButtonName)
            {
                button.onClick.AddListener(LoadBase);
            }
            else
            {
                button.onClick.AddListener(PlayClickSound);
            }

            if (button.targetGraphic == null)
            {
                continue;
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

            hoverEffect.Initialize(hoverBackground, hoverBackgroundColor, hoverFadeDuration, audioSource, hoverSound, hoverSoundVolume);
        }
    }

    void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    void PlayClickSound()
    {
        PlayClickSound(false);
    }

    void PlayClickSound(bool persistAcrossScene)
    {
        if (audioSource == null || clickSound == null)
        {
            return;
        }

        if (persistAcrossScene)
        {
            GameObject soundPlayer = new GameObject("Level Select Click Sound");
            DontDestroyOnLoad(soundPlayer);

            AudioSource source = soundPlayer.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.PlayOneShot(clickSound, clickSoundVolume);
            Destroy(soundPlayer, clickSound.length + 0.1f);
            return;
        }

        audioSource.PlayOneShot(clickSound, clickSoundVolume);
    }

    void LoadFirstMap()
    {
        LoadScene(firstMapSceneName);
    }

    void LoadSecondMap()
    {
        LoadScene(secondMapSceneName);
    }

    void LoadThirdMap()
    {
        LoadScene(thirdMapSceneName);
    }

    void LoadTutorial()
    {
        LoadScene(tutorialSceneName);
    }

    void LoadBase()
    {
        LoadScene(baseSceneName);
    }

    void LoadScene(string sceneName)
    {
        PlayClickSound(true);
        SceneManager.LoadScene(sceneName);
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
            if (buttonImages.Contains(image))
            {
                continue;
            }

            if (!image.gameObject.activeSelf)
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

            float distance = Vector2.Distance(buttonRect.anchoredPosition, backgroundRect.anchoredPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = hoverBackground;
            }
        }

        return closest;
    }
}
