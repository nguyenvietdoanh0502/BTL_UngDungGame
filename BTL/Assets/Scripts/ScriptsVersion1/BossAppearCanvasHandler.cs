using System.Collections;
using UnityEngine;

public class BossAppearCanvasHandler : MonoBehaviour
{
    [SerializeField] bool autoHideOnEnable = true;
    [SerializeField] float autoHideDelay = 5f;

    Coroutine autoHideCoroutine;

    void OnEnable()
    {
        if (autoHideOnEnable)
        {
            StartAutoHide(autoHideDelay);
        }
    }

    void OnDisable()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    public void ShowForSeconds(float duration)
    {
        gameObject.SetActive(true);
        StartAutoHide(duration);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void StartAutoHide(float duration)
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }

        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(Mathf.Max(0f, duration)));
    }

    IEnumerator AutoHideAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        autoHideCoroutine = null;
        Hide();
    }
}
