using UnityEngine;
using UnityEngine.SceneManagement;

public class MapTrans : MonoBehaviour
{
    [Header("Cài đặt Chuyển Map")]
    [Tooltip("Gõ chính xác tên Scene mà bạn muốn chuyển tới")]
    public string targetSceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là Player không
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // Kiểm tra xem đã nhập tên map chưa để tránh lỗi
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                Debug.Log("Đang chuyển sang map: " + targetSceneName);
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogWarning("Chưa nhập tên map cần chuyển tới trong script MapTransitionZone!");
            }
        }
    }
}
