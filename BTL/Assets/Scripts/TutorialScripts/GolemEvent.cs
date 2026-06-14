using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GolemEvent : MonoBehaviour
{

    [Header("1. Boss Setup")]
    public GameObject bossObject;
    public GolemController bossScript;

    [Header("2. Hiệu Ứng Xuất Hiện (Pre-spawn)")]
    public GameObject spawnEffectPrefab;
    public Transform spawnPoint;
    public float effectDuration = 2f;

    [Header("3. Giao Diện Giới Thiệu (Intro)")]
    public BossAppearCanvasHandler appearCanvas;
    public float appearCanvasDuration = 3f;
    public GameObject bossHealthCanvas;

    [Header("4. Nhiệm Vụ & Rào Chắn")]
    [TextArea] public string missionText = "Tiêu diệt Vua Golem!";
    public GameObject[] barrierObjects;

    [Header("5. Custom Mission UI (Tùy chọn)")]
    public GameObject customMissionPanel;
    public Text customMissionText;
    public Text customProgressText;

    [Header("6. Sự kiện Boss Chết (Cinematic)")]
    public AudioClip collapseSound;        // Âm thanh đổ sập
    public float shakeDuration = 2f;       // Thời gian rung màn hình
    public float shakeMagnitude = 0.3f;    // Độ giật của màn hình
    public Image fadeScreen;               // Bức ảnh đen che màn hình
    public float fadeDuration = 3f;        // Thời gian tối dần
    public GameObject dialogCanvas;        // Canvas hộp thoại kể chuyện
    public string storySceneName;          // Tên Scene tiếp theo
    public CinemachineImpulseSource impulseSource;

    private bool isActivated = false;
    private bool isCompleted = false;

    void Start()
    {
        if (bossObject != null) bossObject.SetActive(false);
        if (bossHealthCanvas != null) bossHealthCanvas.SetActive(false);
        if (dialogCanvas != null) dialogCanvas.SetActive(false); // Giấu hộp thoại đi lúc đầu

        // Đảm bảo màn hình không bị đen lúc mới vào
        if (fadeScreen != null)
        {
            Color c = fadeScreen.color;
            c.a = 0f;
            fadeScreen.color = c;
            fadeScreen.gameObject.SetActive(false);
        }

        foreach (GameObject barrier in barrierObjects)
            if (barrier != null)
            {
                barrier.SetActive(false);
            }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !isActivated && !isCompleted)
        {
            StartCoroutine(BossSpawnSequence());
        }
    }

    IEnumerator BossSpawnSequence()
    {
        isActivated = true;

        foreach (GameObject barrier in barrierObjects)
            if (barrier != null)
            {
                barrier.SetActive(true);
            }

        if (spawnEffectPrefab != null && spawnPoint != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, spawnPoint.position, Quaternion.identity);
            yield return new WaitForSeconds(effectDuration);
            Destroy(effect);
        }

        if (bossObject != null) bossObject.SetActive(true);
        if (bossHealthCanvas != null) bossHealthCanvas.SetActive(true);

        if (appearCanvas != null)
        {
            appearCanvas.ShowForSeconds(appearCanvasDuration);
            yield return new WaitForSeconds(appearCanvasDuration);
        }

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowInstruction(
                null, missionText, 0, 0,
                customMissionPanel, customMissionText, customProgressText
            );
        }
    }

    void Update()
    {
        if (isActivated && !isCompleted)
        {
            if (bossScript != null && bossScript.IsDead())
            {
                isCompleted = true; // Ngăn chặn việc gọi liên tục nhiều lần
                Debug.Log("Boss đã chết, bắt đầu sự kiện sập hầm...");
                StartCoroutine(BossDeathSequence());
            }
        }
    }

    // --- COROUTINE SỰ KIỆN KẾT THÚC ---
    IEnumerator BossDeathSequence()
    {
        // 1. Tắt các UI không cần thiết
        if (TutorialManager.Instance != null) TutorialManager.Instance.HideInstruction();
        if (bossHealthCanvas != null) bossHealthCanvas.SetActive(false);

        // 2. Phát âm thanh sập đổ
        if (collapseSound != null)
        {
            AudioSource.PlayClipAtPoint(collapseSound, Camera.main.transform.position);
        }

        // 3. Hiện Canvas Hộp thoại kể chuyện
        if (dialogCanvas != null) dialogCanvas.SetActive(true);

        // 4. Bật ảnh đen lên để bắt đầu làm mờ
        if (fadeScreen != null) fadeScreen.gameObject.SetActive(true);

        float elapsed = 0f;
        Color fadeColor = fadeScreen != null ? fadeScreen.color : Color.black;

        // Khóa mục tiêu: Lưu lại tọa độ lúc Boss vừa chết
        Vector3 originalCamPos = Camera.main.transform.position;

        // Vòng lặp Rung Camera và Tối màn hình cùng lúc
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            // Xử lý Tối dần màn hình (Tăng Alpha)
            if (fadeScreen != null)
            {
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeColor.a = alpha;
                fadeScreen.color = fadeColor;
            }

            // --- ĐIỂM QUAN TRỌNG NHẤT ---
            // Lệnh này bắt Unity phải đợi tất cả các script khác (bao gồm cả Camera Follow) chạy xong hết
            yield return new WaitForEndOfFrame();

            // SAU ĐÓ mới xử lý Rung Camera, để không bị ghi đè
            if (impulseSource != null) impulseSource.GenerateImpulse();

            // Bật ảnh đen lên để bắt đầu làm mờ
            if (fadeScreen != null) fadeScreen.gameObject.SetActive(true);
        }

        // Đảm bảo trạng thái cuối cùng chuẩn xác
        Camera.main.transform.position = originalCamPos;
        if (fadeScreen != null)
        {
            fadeColor.a = 1f;
            fadeScreen.color = fadeColor;
        }

        // 5. Chờ thêm một chút (để người chơi đọc nốt hộp thoại)
        yield return new WaitForSeconds(2f);

        // 6. Chuyển Scene
        if (!string.IsNullOrEmpty(storySceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(storySceneName);
        }
        else
        {
            Debug.LogWarning("Chưa nhập tên Story Scene để chuyển cảnh!");
        }
    }
}
