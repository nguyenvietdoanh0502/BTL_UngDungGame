using UnityEngine;
using System.Collections;

public class LaserTrap : MonoBehaviour
{
    [Header("Trap Timings (Thời gian bẫy)")]
    public float idleTime = 2f;      // Thời gian nằm im không bắn
    public float chargeTime = 1f;    // Thời gian tụ lực (Nên khớp với độ dài clip Trap_Charge)
    public float fireTime = 3f;      // Thời gian duy trì tia laser gây sát thương

    [Header("Damage Settings")]
    public int damageAmount = 10;    // Lượng sát thương

    [Header("References")]
    public BoxCollider2D damageCollider; // Kéo BoxCollider2D của tia laser vào đây
    private Animator animator;
    private Coroutine trapCoroutine;
    void OnEnable()
    {
        animator = GetComponent<Animator>();

        // Bắt đầu vòng lặp hoạt động của bẫy mỗi khi nó được bật lên
        trapCoroutine = StartCoroutine(TrapCycle());
    }

    void OnDisable()
    {
        if (trapCoroutine != null)
        {
            StopCoroutine(trapCoroutine);
        }
    }

    IEnumerator TrapCycle()
    {
        // Vòng lặp vô tận để bẫy hoạt động mãi mãi
        while (true)
        {
            // --- 1. GIAI ĐOẠN NGHỈ (IDLE) ---
            damageCollider.enabled = false; // Tắt sát thương
            animator.Play("laser_idle");     // Chạy anim nằm im
            yield return new WaitForSeconds(idleTime);

            // --- 2. GIAI ĐOẠN TỤ LỰC (CHARGE) ---
            // Chỉ hiện hình ảnh cảnh báo, chưa bật collider sát thương
            animator.Play("laser_charge");
            yield return new WaitForSeconds(chargeTime);

            // --- 3. GIAI ĐOẠN BẮN (FIRE) ---
            animator.Play("trap_fire");
            damageCollider.enabled = true;  // Bật sát thương

            // Lắc camera hoặc thêm âm thanh bíp bíp ở đây nếu muốn

            yield return new WaitForSeconds(fireTime);
        }
    }

    // Cơ chế gây sát thương theo thời gian (giống hệt tia laser của Boss)
    void OnTriggerStay2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        BatController bat = collision.GetComponent<BatController>();
        SlimeController slime = collision.GetComponent<SlimeController>();
        // Kiểm tra xem có trúng Player và đã đến lúc trừ máu tiếp chưa
        if (player != null)
        {
            player.changeHealth(-damageAmount);
        }
        else if (bat != null)
        {
            bat.TakeDamage(damageAmount);
        }
        else if (slime != null)
        {
            slime.TakeDamage(damageAmount);
        }
    }
}
