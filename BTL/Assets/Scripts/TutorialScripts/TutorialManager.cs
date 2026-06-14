using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Default UI (Dùng khi Zone không có UI riêng)")]
    public GameObject defaultPanel;
    public Text defaultInstructionText;
    public Text defaultProgressText;

    [Header("Input Settings")]
    public Key healActionKey = Key.H;

    private TutorialZone currentActiveZone;

    // Lưu trữ UI đang được sử dụng ở khu vực hiện tại
    private GameObject activePanel;
    private Text activeInstructionText;
    private Text activeProgressText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (defaultPanel != null) defaultPanel.SetActive(false);
    }

    void Update()
    {
        if (currentActiveZone == null) return;
        ListenToPlayerInputs();
    }

    void ListenToPlayerInputs()
    {
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                PlayerDidAction(TutorialActionType.Attack);

            if (Mouse.current.rightButton.wasPressedThisFrame)
                PlayerDidAction(TutorialActionType.Defend);
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current[healActionKey].wasPressedThisFrame)
                PlayerDidAction(TutorialActionType.Heal);

            if (Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame ||
                Keyboard.current.sKey.wasPressedThisFrame ||
                Keyboard.current.dKey.wasPressedThisFrame)
            {
                PlayerDidAction(TutorialActionType.Move);
            }
        }
    }

    // NHẬN THÊM CÁC BIẾN UI TÙY CHỈNH TỪ ZONE
    public void ShowInstruction(TutorialZone zone, string instruction, int required, int current, GameObject customPanel = null, Text customInstText = null, Text customProgText = null)
    {
        currentActiveZone = zone;

        // Quyết định dùng UI nào: Có Custom thì dùng Custom, không có thì dùng Default
        activePanel = customPanel != null ? customPanel : defaultPanel;
        activeInstructionText = customInstText != null ? customInstText : defaultInstructionText;
        activeProgressText = customProgText != null ? customProgText : defaultProgressText;

        // Bật Panel
        if (activePanel != null) activePanel.SetActive(true);

        // Cập nhật chữ
        if (activeInstructionText != null) activeInstructionText.text = instruction;
        UpdateProgress(current, required);
    }

    public void UpdateProgress(int current, int required)
    {
        if (activeProgressText != null)
        {
            if (required > 0)
                activeProgressText.text = current + " / " + required;
            else
                activeProgressText.text = "";
        }
    }

    public void HideInstruction()
    {
        // Tắt Panel hiện tại
        if (activePanel != null) activePanel.SetActive(false);

        // Dọn dẹp biến bộ nhớ
        currentActiveZone = null;
        activePanel = null;
        activeInstructionText = null;
        activeProgressText = null;
    }

    private void PlayerDidAction(TutorialActionType actionType)
    {
        if (currentActiveZone != null)
        {
            currentActiveZone.RecordAction(actionType);
        }
    }
}
