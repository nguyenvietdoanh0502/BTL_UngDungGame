using System.Collections;
using UnityEngine;

public class BossAppearCanvasHandler : MonoBehaviour
{
    [SerializeField] bool autoHideOnEnable = true;
    [SerializeField] float autoHideDelay = 5f;
    [SerializeField] float fadeInDuration = 0.35f;
    [SerializeField] float fadeOutDuration = 0.35f;

    CanvasGroup canvasGroup;
    Coroutine presentationCoroutine;
    bool suppressOnEnablePresentation;

    void Awake()
    {
        EnsureCanvasGroup();
    }

    void OnValidate()
    {
        autoHideDelay = Mathf.Max(0f, autoHideDelay);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
    }

    void OnEnable()
    {
        if (suppressOnEnablePresentation)
        {
            return;
        }

        if (autoHideOnEnable)
        {
            ShowForSeconds(autoHideDelay);
            return;
        }

        Show();
    }

    void OnDisable()
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
            presentationCoroutine = null;
        }
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            SetActiveWithoutOnEnablePresentation(true);
        }

        StartPresentation(-1f);
    }

    public void ShowForSeconds(float duration)
    {
        if (!gameObject.activeSelf)
        {
            SetActiveWithoutOnEnablePresentation(true);
        }

        StartPresentation(Mathf.Max(0f, duration));
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
        {
            SetAlpha(0f);
            return;
        }

        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
        }

        presentationCoroutine = StartCoroutine(HideAfterFade());
    }

    void StartPresentation(float duration)
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(presentationCoroutine);
        }

        presentationCoroutine = StartCoroutine(PresentationRoutine(duration));
    }

    IEnumerator PresentationRoutine(float duration)
    {
        EnsureCanvasGroup();
        SetAlpha(0f);

        yield return FadeCanvasGroup(0f, 1f, fadeInDuration);

        if (duration >= 0f)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            yield return HideAfterFade();
        }

        presentationCoroutine = null;
    }

    IEnumerator HideAfterFade()
    {
        EnsureCanvasGroup();
        yield return FadeCanvasGroup(canvasGroup.alpha, 0f, fadeOutDuration);

        presentationCoroutine = null;
        gameObject.SetActive(false);
    }

    void SetActiveWithoutOnEnablePresentation(bool active)
    {
        suppressOnEnablePresentation = true;
        gameObject.SetActive(active);
        suppressOnEnablePresentation = false;
    }

    IEnumerator FadeCanvasGroup(float startAlpha, float targetAlpha, float duration)
    {
        EnsureCanvasGroup();

        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, percent));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    void EnsureCanvasGroup()
    {
        if (canvasGroup != null)
        {
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    void SetAlpha(float alpha)
    {
        EnsureCanvasGroup();
        canvasGroup.alpha = alpha;
        canvasGroup.interactable = alpha > 0f;
        canvasGroup.blocksRaycasts = alpha > 0f;
    }
}
