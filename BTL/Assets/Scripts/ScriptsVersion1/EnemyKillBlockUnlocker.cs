using UnityEngine;
using System.Collections;
using System;
using Unity.Cinemachine;

public class EnemyKillBlockUnlocker : MonoBehaviour
{
    public int requiredBatKills = 10;
    public int requiredSlimeKills = 10;
    public GameObject blockToHide;
    public GameObject bossToShow;
    public bool hideBossUntilMissionComplete = true;
    public GameObject bossAppearCanvas;
    public bool showBossAppearCanvasOnMissionComplete = true;

    [Header("Boss Appearance Sequence")]
    public bool removeRemainingEnemiesOnBossAppear = true;
    public bool disableSpawnTriggersOnBossAppear = true;
    public bool disablePlayerDuringBossAppear = true;
    public float cameraPanToBossDuration = 1.25f;
    public float bossAppearCanvasDuration = 5f;
    public float cameraReturnDuration = 1.25f;
    public Vector3 bossCameraOffset = Vector3.zero;

    static EnemyKillBlockUnlocker activeTracker;
    static int batKills;
    static int slimeKills;
    static bool isMissionCompleted;

    public static event Action<int, int> KillCountsChanged;
    public static event Action MissionCompleted;

    public static int BatKills => batKills;
    public static int SlimeKills => slimeKills;
    public static int RequiredBatKills => activeTracker != null ? activeTracker.requiredBatKills : 10;
    public static int RequiredSlimeKills => activeTracker != null ? activeTracker.requiredSlimeKills : 10;
    public static bool IsMissionCompleted => isMissionCompleted;

    Coroutine bossAppearSequenceCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetKillCounts()
    {
        activeTracker = null;
        batKills = 0;
        slimeKills = 0;
        isMissionCompleted = false;
        KillCountsChanged = null;
        MissionCompleted = null;
    }

    void Awake()
    {
        if (blockToHide == null)
        {
            blockToHide = gameObject;
        }

        FindBossIfNeeded();
        SetBossVisible(isMissionCompleted, false);
        FindBossAppearCanvasIfNeeded();
        SetBossAppearCanvasVisible(false);
    }

    void OnValidate()
    {
        cameraPanToBossDuration = Mathf.Max(0f, cameraPanToBossDuration);
        bossAppearCanvasDuration = Mathf.Max(0f, bossAppearCanvasDuration);
        cameraReturnDuration = Mathf.Max(0f, cameraReturnDuration);
    }

    void OnEnable()
    {
        activeTracker = this;
        FindBossIfNeeded();
        SetBossVisible(isMissionCompleted, false);
        FindBossAppearCanvasIfNeeded();
        if (!isMissionCompleted)
        {
            SetBossAppearCanvasVisible(false);
        }
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

        CompleteMissionIfNeeded();
    }

    void CompleteMissionIfNeeded()
    {
        if (isMissionCompleted)
        {
            if (bossAppearSequenceCoroutine == null)
            {
                SetBossVisible(true, false);
            }
            return;
        }

        isMissionCompleted = true;
        MissionCompleted?.Invoke();

        if (bossAppearSequenceCoroutine != null)
        {
            StopCoroutine(bossAppearSequenceCoroutine);
        }

        bossAppearSequenceCoroutine = StartCoroutine(BossAppearSequence());
    }

    IEnumerator BossAppearSequence()
    {
        SetBossAppearCanvasVisible(false);
        RemoveRemainingEnemiesAndSpawns();

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (disablePlayerDuringBossAppear && player != null)
        {
            player.SetControlsEnabled(false);
        }

        SetBossVisible(true, true);
        BossController boss = GetBossController();
        AgisBossController agisBoss = GetAgisBossController();
        if (boss != null)
        {
            boss.SetCombatEnabled(false);
        }
        if (agisBoss != null)
        {
            agisBoss.SetCombatEnabled(false);
        }

        yield return RunCameraAndPopupSequence(boss != null ? boss.transform : GetBossTransform());

        if (boss != null)
        {
            boss.SetCombatEnabled(true);
        }
        if (agisBoss != null)
        {
            agisBoss.SetCombatEnabled(true);
        }

        if (disablePlayerDuringBossAppear && player != null)
        {
            player.SetControlsEnabled(true);
        }

        HideUnlockedBlock();
        bossAppearSequenceCoroutine = null;
    }

    void SetBossVisible(bool visible, bool playAppearSound)
    {
        if (!hideBossUntilMissionComplete)
        {
            return;
        }

        FindBossIfNeeded();
        if (bossToShow == null)
        {
            return;
        }

        bossToShow.SetActive(visible);

        if (!visible || !playAppearSound)
        {
            return;
        }

        BossController boss = bossToShow.GetComponent<BossController>();
        if (boss != null)
        {
            boss.PlayAppearSound();
        }
    }

    BossController GetBossController()
    {
        FindBossIfNeeded();
        return bossToShow != null ? bossToShow.GetComponent<BossController>() : null;
    }

    AgisBossController GetAgisBossController()
    {
        FindBossIfNeeded();
        return bossToShow != null ? bossToShow.GetComponent<AgisBossController>() : null;
    }

    Transform GetBossTransform()
    {
        FindBossIfNeeded();
        return bossToShow != null ? bossToShow.transform : null;
    }

    void FindBossIfNeeded()
    {
        if (bossToShow != null)
        {
            return;
        }

        BossController[] bosses = Resources.FindObjectsOfTypeAll<BossController>();
        foreach (BossController boss in bosses)
        {
            if (boss == null || !boss.gameObject.scene.IsValid())
            {
                continue;
            }

            bossToShow = boss.gameObject;
            return;
        }
    }

    IEnumerator RunCameraAndPopupSequence(Transform bossTransform)
    {
        CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        Camera mainCamera = Camera.main;
        Transform originalFollow = null;
        Transform originalLookAt = null;
        GameObject cutsceneTarget = null;
        Vector3 fallbackCameraStart = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        bool usingCinemachine = virtualCamera != null;

        if (usingCinemachine)
        {
            originalFollow = virtualCamera.Follow;
            originalLookAt = virtualCamera.LookAt;
            cutsceneTarget = new GameObject("BossCameraCutsceneTarget");
            cutsceneTarget.transform.position = originalFollow != null
                ? originalFollow.position
                : GetCameraFocusPosition(mainCamera);
            virtualCamera.Follow = cutsceneTarget.transform;
            virtualCamera.LookAt = cutsceneTarget.transform;

            yield return MoveTransformToPosition(cutsceneTarget.transform, GetBossFocusPosition(bossTransform), cameraPanToBossDuration);
            ShowBossAppearCanvasForDuration();
            yield return new WaitForSeconds(bossAppearCanvasDuration);
            SetBossAppearCanvasVisible(false);

            Vector3 returnPosition = originalFollow != null ? originalFollow.position : GetCameraFocusPosition(mainCamera);
            yield return MoveTransformToPosition(cutsceneTarget.transform, returnPosition, cameraReturnDuration);

            virtualCamera.Follow = originalFollow;
            virtualCamera.LookAt = originalLookAt;
            Destroy(cutsceneTarget);
            yield break;
        }

        if (mainCamera != null)
        {
            Vector3 bossCameraPosition = GetBossFocusPosition(bossTransform);
            bossCameraPosition.z = fallbackCameraStart.z;
            yield return MoveTransformToPosition(mainCamera.transform, bossCameraPosition, cameraPanToBossDuration);
        }

        ShowBossAppearCanvasForDuration();
        yield return new WaitForSeconds(bossAppearCanvasDuration);
        SetBossAppearCanvasVisible(false);

        if (mainCamera != null)
        {
            yield return MoveTransformToPosition(mainCamera.transform, fallbackCameraStart, cameraReturnDuration);
        }
    }

    IEnumerator MoveTransformToPosition(Transform target, Vector3 destination, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            target.position = destination;
            yield break;
        }

        Vector3 start = target.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, destination, percent);
            yield return null;
        }

        target.position = destination;
    }

    Vector3 GetBossFocusPosition(Transform bossTransform)
    {
        if (bossTransform == null)
        {
            return GetCameraFocusPosition(Camera.main);
        }

        return bossTransform.position + bossCameraOffset;
    }

    Vector3 GetCameraFocusPosition(Camera cameraToUse)
    {
        if (cameraToUse == null)
        {
            return Vector3.zero;
        }

        Vector3 position = cameraToUse.transform.position;
        position.z = 0f;
        return position;
    }

    void SetBossAppearCanvasVisible(bool visible)
    {
        if (!showBossAppearCanvasOnMissionComplete)
        {
            return;
        }

        FindBossAppearCanvasIfNeeded();
        if (bossAppearCanvas == null)
        {
            return;
        }

        BossAppearCanvasHandler handler = bossAppearCanvas.GetComponent<BossAppearCanvasHandler>();
        if (handler != null)
        {
            if (visible)
            {
                handler.Show();
            }
            else
            {
                handler.Hide();
            }

            return;
        }

        bossAppearCanvas.SetActive(visible);
    }

    void ShowBossAppearCanvasForDuration()
    {
        if (!showBossAppearCanvasOnMissionComplete)
        {
            return;
        }

        FindBossAppearCanvasIfNeeded();
        if (bossAppearCanvas == null)
        {
            return;
        }

        BossAppearCanvasHandler handler = bossAppearCanvas.GetComponent<BossAppearCanvasHandler>();
        if (handler != null)
        {
            handler.ShowForSeconds(bossAppearCanvasDuration);
            return;
        }

        bossAppearCanvas.SetActive(true);
    }

    void FindBossAppearCanvasIfNeeded()
    {
        if (bossAppearCanvas != null)
        {
            return;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform foundTransform in transforms)
        {
            if (foundTransform == null || !foundTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            if (foundTransform.gameObject.name == "BossAppear")
            {
                bossAppearCanvas = foundTransform.gameObject;
                return;
            }
        }
    }

    void RemoveRemainingEnemiesAndSpawns()
    {
        if (disableSpawnTriggersOnBossAppear)
        {
            SetSceneComponentsEnabled<BatSpawnTrigger>(false);
            SetSceneComponentsEnabled<SlimeSpawnTrigger>(false);
        }

        if (!removeRemainingEnemiesOnBossAppear)
        {
            return;
        }

        DestroySceneObjectsOfType<BatController>();
        DestroySceneObjectsOfType<SlimeController>();
        DestroySceneObjectsOfType<EnemyController>();
    }

    void DestroySceneObjectsOfType<T>() where T : Component
    {
        T[] sceneObjects = Resources.FindObjectsOfTypeAll<T>();
        foreach (T sceneObject in sceneObjects)
        {
            if (sceneObject == null || !sceneObject.gameObject.scene.IsValid() || !sceneObject.gameObject.activeInHierarchy)
            {
                continue;
            }

            Destroy(sceneObject.gameObject);
        }
    }

    void SetSceneComponentsEnabled<T>(bool enabled) where T : Behaviour
    {
        T[] sceneComponents = Resources.FindObjectsOfTypeAll<T>();
        foreach (T sceneComponent in sceneComponents)
        {
            if (sceneComponent == null || !sceneComponent.gameObject.scene.IsValid())
            {
                continue;
            }

            sceneComponent.enabled = enabled;
        }
    }

    void HideUnlockedBlock()
    {
        GameObject target = blockToHide != null ? blockToHide : gameObject;
        if (target != null)
        {
            target.SetActive(false);
        }
    }
}
