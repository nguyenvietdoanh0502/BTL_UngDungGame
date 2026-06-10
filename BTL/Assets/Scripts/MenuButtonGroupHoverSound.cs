using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuButtonGroupHoverSound : MonoBehaviour
{
    [SerializeField] string firstMapButtonName = "First Map";
    [SerializeField] string secondMapButtonName = "Second Map";
    [SerializeField] string firstMapSceneName = "FirstMap";
    [SerializeField] string secondMapSceneName = "SecondMap";
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

            if (button.name == firstMapButtonName)
            {
                button.onClick.AddListener(LoadFirstMap);
            }
            else if (button.name == secondMapButtonName)
            {
                button.onClick.AddListener(LoadSecondMap);
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
