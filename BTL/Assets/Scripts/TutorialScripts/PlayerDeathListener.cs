using UnityEngine;

public class PlayerDeathListener : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    public PlayerController playerTarget;

    private bool hasTriggeredDeath = false;

    void Update()
    {
        // Nếu đã kích hoạt chết rồi, hoặc không có mục tiêu thì bỏ qua
        if (hasTriggeredDeath || playerTarget == null) return;

        // KIỂM TRA MÁU CỦA PLAYER
        // *Lưu ý: Thay chữ 'currentHealth' bằng đúng tên biến máu đang dùng trong PlayerController của bạn.
        // Biến máu đó bắt buộc phải là 'public' (ví dụ: public int currentHealth;)
        if (playerTarget.getCurrentHealth() <= 0)
        {
            hasTriggeredDeath = true; // Khóa lại để không gọi nhiều lần

            // Gọi Tổng đài Cinematic mà bạn vừa tạo lúc nãy
            if (PlayerDeath.Instance != null)
            {
                PlayerDeath.Instance.PlayDeathSequence();
            }
        }
    }
}
