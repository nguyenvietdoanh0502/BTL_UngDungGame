using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDeath : MonoBehaviour
{
    public static PlayerDeath Instance;

    [Header("UI & Cinematic Setup")]
    public GameObject dialogCanvas;
    public float dialogWaitTime = 3f;
    public Image fadeScreen;
    public float fadeDuration = 4f;

    [Header("Dọn dẹp màn hình (Tắt Canvas cũ)")]
    public GameObject[] uiToHide;          // Kéo các Canvas máu, minimap... vào đây để tắt đi

    [Header("Scene Transition")]
    public string deathStorySceneName;

    private bool isDead = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dialogCanvas != null) dialogCanvas.SetActive(false);

        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            c.a = 0f;
            fadeScreen.color = c;
            fadeScreen.gameObject.SetActive(false);
        }
    }

    public void PlayDeathSequence()
    {
        if (isDead) return;
        isDead = true;

        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        // 1. TẮT CÁC UI CŨ ĐANG ĐÈ LÊN NHAU
        foreach (GameObject ui in uiToHide)
        {
            if (ui != null) ui.SetActive(false);
        }

        // Tắt luôn UI nhiệm vụ nếu đang hiển thị
        if (TutorialManager.Instance != null) TutorialManager.Instance.HideInstruction();

        // 2. Hiện Canvas trò chuyện
        if (dialogCanvas != null) dialogCanvas.SetActive(true);

        // THAY ĐỔI 1: Dùng WaitForSecondsRealtime để không bị kẹt nếu game đang Pause
        yield return new WaitForSecondsRealtime(dialogWaitTime);

        // 3. Bắt đầu làm tối màn hình
        if (fadeScreen != null) fadeScreen.gameObject.SetActive(true);

        float elapsed = 0f;
        Color fadeColor = fadeScreen != null ? fadeScreen.color : Color.black;

        while (elapsed < fadeDuration)
        {
            // THAY ĐỔI 2: Dùng unscaledDeltaTime để cộng dồn thời gian thật, thoát khỏi vòng lặp chết
            elapsed += Time.unscaledDeltaTime;

            if (fadeScreen != null)
            {
                fadeColor.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeScreen.color = fadeColor;
            }
            yield return null;
        }

        if (fadeScreen != null)
        {
            fadeColor.a = 1f;
            fadeScreen.color = fadeColor;
        }

        yield return new WaitForSecondsRealtime(1f);

        // 4. Chuyển Scene
        if (!string.IsNullOrEmpty(deathStorySceneName))
        {
            // Rất quan trọng: Phục hồi lại thời gian trước khi sang Scene mới
            Time.timeScale = 1f;
            SceneManager.LoadScene(deathStorySceneName);
        }
    }
}
