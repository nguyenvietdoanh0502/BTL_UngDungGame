using UnityEngine;

public class RemainHeal : MonoBehaviour
{
    [Header("Hiệu ứng (Tùy chọn)")]
    public ParticleSystem healParticlePrefab; // Hạt lấp lánh khi hồi máu
    public AudioClip healSound;               // Âm thanh khi chạm vào

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra xem người chạm vào có phải là Player không
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // 1. Hồi đầy thanh máu (truyền một lượng máu khổng lồ bằng maxHealth)
            player.changeHealth(player.maxHealth);

            // 2. Nạp đầy 3 bình máu
            player.RefillPotions();

            // 3. Chạy hiệu ứng và âm thanh (nếu có)
            if (healParticlePrefab != null)
            {
                Instantiate(healParticlePrefab, player.transform.position, Quaternion.identity);
            }

            if (healSound != null)
            {
                AudioSource.PlayClipAtPoint(healSound, transform.position);
            }
        }
    }
}
