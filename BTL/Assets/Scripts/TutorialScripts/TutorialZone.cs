using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;

public class TutorialZone : MonoBehaviour
{
    [Header("UI Instruction")]
    [TextArea] public string instructionText;

    [Header("Custom UI (Tùy chọn) - Bỏ trống sẽ dùng mặc định")]
    public GameObject customPanel;
    public Text customInstructionText;
    public Text customProgressText;

    [Header("Obstacles & Spawns")]
    // Đã chuyển thành mảng (Array) để chứa được nhiều rào chắn
    public GameObject[] barrierObjects;

    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;

    [Header("Action Requirements")]
    public TutorialActionType requiredAction;
    public int requiredCount = 1;
    private int currentCount = 0;

    private bool isActivated = false;
    private bool isCompleted = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !isActivated && !isCompleted)
        {
            ActivateZone();
        }
    }

    void ActivateZone()
    {
        isActivated = true;

        // 1. Kích hoạt TẤT CẢ các rào chắn có trong danh sách
        if (barrierObjects != null)
        {
            foreach (GameObject barrier in barrierObjects)
            {
                if (barrier != null) barrier.SetActive(true);
            }
        }

        // 2. Spawn quái vật
        int spawnCount = Mathf.Min(enemyPrefabs.Length, spawnPoints.Length);
        for (int i = 0; i < spawnCount; i++)
        {
            if (enemyPrefabs[i] != null && spawnPoints[i] != null)
            {
                GameObject enemy = Instantiate(enemyPrefabs[i], spawnPoints[i].position, Quaternion.identity);
                spawnedEnemies.Add(enemy);
            }
        }

        // 3. Hiển thị UI
        TutorialManager.Instance.ShowInstruction(this, instructionText, requiredCount, currentCount, customPanel, customInstructionText, customProgressText);
    }

    public void RecordAction(TutorialActionType action)
    {
        if (!isActivated || isCompleted) return;

        if (action == requiredAction && currentCount < requiredCount)
        {
            currentCount++;
            TutorialManager.Instance.UpdateProgress(currentCount, requiredCount);
            CheckCompletion();
        }
    }

    void Update()
    {
        if (isActivated && !isCompleted)
        {
            // Tự động loại bỏ các quái vật đã bị tiêu diệt
            spawnedEnemies.RemoveAll(enemy => enemy == null);
            CheckCompletion();
        }
    }

    void CheckCompletion()
    {
        // Kiểm tra điều kiện qua màn
        if (currentCount >= requiredCount && spawnedEnemies.Count == 0)
        {
            CompleteZone();
        }
    }

    void CompleteZone()
    {
        isCompleted = true;

        // Mở TẤT CẢ các rào chắn khi hoàn thành xong nhiệm vụ
        if (barrierObjects != null)
        {
            foreach (GameObject barrier in barrierObjects)
            {
                if (barrier != null) barrier.SetActive(false);
            }
        }

        TutorialManager.Instance.HideInstruction();
    }
}
