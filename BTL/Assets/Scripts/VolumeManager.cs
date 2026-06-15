using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [Header("UI Cài đặt")]
    public Slider volumeSlider;

    void Start()
    {
        // 1. Tải mức âm lượng đã lưu từ lần chơi trước (nếu chưa có thì mặc định là 1 - mức tối đa)
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);

        // 2. Cài đặt âm lượng tổng của toàn bộ game
        AudioListener.volume = savedVolume;

        // 3. Cập nhật vị trí cục kéo của thanh trượt cho khớp với âm lượng
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;

            // Tự động lắng nghe mỗi khi người chơi kéo thanh trượt
            volumeSlider.onValueChanged.AddListener(ChangeVolume);
        }
    }

    // Hàm này sẽ tự động chạy mỗi khi thanh Slider bị kéo
    public void ChangeVolume(float value)
    {
        // Đổi âm lượng của game ngay lập tức
        AudioListener.volume = value;

        // Lưu lại mức âm lượng mới vào hệ thống máy tính
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }
}
