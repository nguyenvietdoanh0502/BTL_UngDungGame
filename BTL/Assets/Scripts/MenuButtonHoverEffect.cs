using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    Image backgroundImage;
    AudioSource audioSource;
    AudioClip hoverSound;
    Color visibleColor;
    float fadeDuration = 0.15f;
    float hoverSoundVolume = 1f;
    bool pointerInside;
    bool selected;
    Coroutine fadeRoutine;

    public void Initialize(Image background, Color color, float duration, AudioSource source, AudioClip sound, float soundVolume)
    {
        backgroundImage = background;
        audioSource = source;
        hoverSound = sound;
        visibleColor = color;
        fadeDuration = Mathf.Max(0f, duration);
        hoverSoundVolume = Mathf.Clamp01(soundVolume);

        if (backgroundImage == null)
        {
            return;
        }

        backgroundImage.enabled = true;
        backgroundImage.raycastTarget = false;
        SetBackgroundAlpha(0f);
        backgroundImage.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        PlayHoverSound();
        FadeTo(visibleColor.a);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;

        if (!selected)
        {
            FadeTo(0f);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        FadeTo(visibleColor.a);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;

        if (!pointerInside)
        {
            FadeTo(0f);
        }
    }

    void OnDisable()
    {
        pointerInside = false;
        selected = false;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (backgroundImage != null)
        {
            SetBackgroundAlpha(0f);
            backgroundImage.gameObject.SetActive(false);
        }
    }

    void FadeTo(float targetAlpha)
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeBackground(targetAlpha));
    }

    void PlayHoverSound()
    {
        if (audioSource == null || hoverSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(hoverSound, hoverSoundVolume);
    }

    IEnumerator FadeBackground(float targetAlpha)
    {
        backgroundImage.gameObject.SetActive(true);

        float startAlpha = backgroundImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            SetBackgroundAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetBackgroundAlpha(targetAlpha);

        if (targetAlpha <= 0f)
        {
            backgroundImage.gameObject.SetActive(false);
        }

        fadeRoutine = null;
    }

    void SetBackgroundAlpha(float alpha)
    {
        Color color = visibleColor;
        color.a = alpha;
        backgroundImage.color = color;
    }
}
