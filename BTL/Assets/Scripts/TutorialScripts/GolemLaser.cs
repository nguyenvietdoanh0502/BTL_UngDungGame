using UnityEngine;

public class GolemLaser : MonoBehaviour
{
    public int damageAmount = 10;
    public float damageTickRate = 0.5f; // Khoảng thời gian giữa các lần nhận sát thương (giây)

    private float nextDamageTime = 0f;

    // OnTriggerStay2D sẽ chạy liên tục chừng nào người chơi còn nằm trong vùng Collider của tia Laser
    void OnTriggerStay2D(Collider2D collision)
    {
        if (Time.time >= nextDamageTime)
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.changeHealth(-damageAmount);
                nextDamageTime = Time.time + damageTickRate;
            }
        }
    }

    // Reset lại bộ đếm khi tia laser bị tắt đi, đảm bảo lần bắn sau có sát thương ngay lập tức
    void OnDisable()
    {
        nextDamageTime = 0f;
    }
}
