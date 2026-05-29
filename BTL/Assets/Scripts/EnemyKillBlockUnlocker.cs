using UnityEngine;
using System;

public class EnemyKillBlockUnlocker : MonoBehaviour
{
    public int requiredBatKills = 10;
    public int requiredSlimeKills = 10;
    public GameObject blockToHide;

    static EnemyKillBlockUnlocker activeTracker;
    static int batKills;
    static int slimeKills;

    public static event Action<int, int> KillCountsChanged;

    public static int BatKills => batKills;
    public static int SlimeKills => slimeKills;
    public static int RequiredBatKills => activeTracker != null ? activeTracker.requiredBatKills : 10;
    public static int RequiredSlimeKills => activeTracker != null ? activeTracker.requiredSlimeKills : 10;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetKillCounts()
    {
        activeTracker = null;
        batKills = 0;
        slimeKills = 0;
    }

    void Awake()
    {
        if (blockToHide == null)
        {
            blockToHide = gameObject;
        }
    }

    void OnEnable()
    {
        activeTracker = this;
        NotifyKillCountsChanged();
        CheckUnlock();
    }

    void OnDisable()
    {
        if (activeTracker == this)
        {
            activeTracker = null;
        }
    }

    public static void ReportBatKilled()
    {
        batKills++;
        NotifyKillCountsChanged();
        CheckActiveTracker();
    }

    public static void ReportSlimeKilled()
    {
        slimeKills++;
        NotifyKillCountsChanged();
        CheckActiveTracker();
    }

    static void NotifyKillCountsChanged()
    {
        KillCountsChanged?.Invoke(batKills, slimeKills);
    }

    static void CheckActiveTracker()
    {
        if (activeTracker != null)
        {
            activeTracker.CheckUnlock();
        }
    }

    void CheckUnlock()
    {
        if (batKills < requiredBatKills || slimeKills < requiredSlimeKills)
        {
            return;
        }

        GameObject target = blockToHide != null ? blockToHide : gameObject;
        target.SetActive(false);
    }
}
