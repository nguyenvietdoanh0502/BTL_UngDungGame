using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSoundController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;
    [Range(0f, 1f)] public float hoverSoundVolume = 0.7f;
    [Range(0f, 1f)] public float clickSoundVolume = 0.9f;
    public bool persistClickSoundAcrossScene = true;

    void Awake()
    {
        ConfigureButtons();
    }

    void OnEnable()
    {
        ConfigureButtons();
    }

    void OnDestroy()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }
    }

    void ConfigureButtons()
    {
        EnsureAudioSource();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveListener(PlayClickSound);
            button.onClick.AddListener(PlayClickSound);

            UIButtonSoundEmitter emitter = button.GetComponent<UIButtonSoundEmitter>();
            if (emitter == null)
            {
                emitter = button.gameObject.AddComponent<UIButtonSoundEmitter>();
            }

            emitter.Initialize(this, button);
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
        audioSource.spatialBlend = 0f;
    }

    public void PlayHoverSound()
    {
        if (audioSource == null || hoverSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(hoverSound, hoverSoundVolume);
    }

    public void PlayClickSound()
    {
        if (clickSound == null)
        {
            return;
        }

        if (!persistClickSoundAcrossScene)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound, clickSoundVolume);
            }

            return;
        }

        GameObject soundPlayer = new GameObject("UI Button Click Sound");
        DontDestroyOnLoad(soundPlayer);

        AudioSource source = soundPlayer.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.PlayOneShot(clickSound, clickSoundVolume);
        Destroy(soundPlayer, clickSound.length + 0.1f);
    }
}

public class UIButtonSoundEmitter : MonoBehaviour, IPointerEnterHandler
{
    UIButtonSoundController soundController;
    Button button;

    public void Initialize(UIButtonSoundController controller, Button ownerButton)
    {
        soundController = controller;
        button = ownerButton;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
        {
            return;
        }

        if (soundController != null)
        {
            soundController.PlayHoverSound();
        }
    }
}
