using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem;
public class StorySequenceManager : MonoBehaviour
{
    [Header("Cốt truyện (Kéo các Panel vào đây)")]
    public GameObject[] storyPanels;

    [Header("Cài đặt chuyển cảnh")]
    public float fadeDuration = 1f;  // Thời gian mờ dần giữa các bức ảnh
    public string nextSceneName;     // Tên Map tiếp theo sẽ chuyển tới

    [Header("Nhạc nền duy nhất cho Scene")]
    public AudioSource bgmSource;    // Kéo Component AudioSource vào đây
    public AudioClip storyBGM;       // Kéo file nhạc nền muốn phát vào đây

    private int currentIndex = 0;
    private bool isTransitioning = false;

    void Start()
    {
        // 1. Tự động phát nhạc nền ngay khi vừa vào Scene
        if (bgmSource != null && storyBGM != null)
        {
            bgmSource.clip = storyBGM;
            bgmSource.loop = true; // Đảm bảo nhạc lặp lại liên tục không bị ngắt
            bgmSource.Play();
        }

        // 2. Ẩn tất cả các Panel lúc ban đầu
        foreach (GameObject panel in storyPanels)
        {
            if (panel != null)
            {
                CanvasGroup cg = panel.GetComponent<CanvasGroup>();
                if (cg == null) cg = panel.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
                panel.SetActive(false);
            }
        }

        // 3. Hiện Panel đầu tiên lên
        if (storyPanels.Length > 0)
        {
            StartCoroutine(FadeInPanel(currentIndex));
        }
    }

    void Update()
    {
        bool isPressed = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) isPressed = true;
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)) isPressed = true;

        if (isPressed && !isTransitioning)
        {
            NextPanel();
        }
    }

    public void NextPanel()
    {
        if (currentIndex < storyPanels.Length - 1)
        {
            StartCoroutine(TransitionToNextPanel());
        }
        else
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }

    IEnumerator TransitionToNextPanel()
    {
        isTransitioning = true;

        // Làm mờ ảnh cũ
        CanvasGroup currentGroup = storyPanels[currentIndex].GetComponent<CanvasGroup>();
        yield return StartCoroutine(FadeCanvasGroup(currentGroup, 1f, 0f, fadeDuration));
        storyPanels[currentIndex].SetActive(false);

        currentIndex++;

        // Làm rõ ảnh mới
        yield return StartCoroutine(FadeInPanel(currentIndex));

        isTransitioning = false;
    }

    IEnumerator FadeInPanel(int index)
    {
        isTransitioning = true;

        GameObject panel = storyPanels[index];
        panel.SetActive(true);
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        yield return StartCoroutine(FadeCanvasGroup(group, 0f, 1f, fadeDuration));

        isTransitioning = false;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
    }
}
