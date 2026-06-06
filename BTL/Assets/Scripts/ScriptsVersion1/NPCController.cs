using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NPCController : MonoBehaviour
{
    public static bool IsMissionAccepted { get; private set; }

    Rigidbody2D npcRigidbody;
    Animator animator;
    AudioSource audioSource;
    PlayerController player;
    readonly List<GameObject> hiddenEnemies = new List<GameObject>();

    public float interactDistance = 2.0f;
    public GameObject dialogueCanvas;
    public string dialogueCanvasName = "NPCDiaglogue";
    public bool hideDialogueOnStart = true;
    public GameObject missionCanvas;
    public string missionCanvasName = "Misson";
    public bool hideMissionOnStart = true;
    public Button yesButton;
    public string yesButtonName = "Yes";
    public Button noButton;
    public string noButtonName = "No";
    public bool closeDialogueAfterAccept = true;
    public bool hideEnemiesUntilMissionAccepted = true;
    public AudioClip interactSound;
    [UnityEngine.Range(0f, 1f)] public float interactSoundVolume = 1f;
    public bool preventSoundOverlap = true;

    bool broken = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetMissionState()
    {
        IsMissionAccepted = false;
    }

    void Start()
    {
        npcRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        player = FindFirstObjectByType<PlayerController>();
        FindDialogueCanvas();
        FindMissionCanvas();
        BindDialogueButtons();

        if (hideDialogueOnStart)
        {
            SetDialogueVisible(false);
        }

        if (hideMissionOnStart)
        {
            SetMissionVisible(false);
        }

        if (hideEnemiesUntilMissionAccepted && !IsMissionAccepted)
        {
            HideMissionEnemies();
        }
    }

    void OnDestroy()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnYesButtonClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnNoButtonClicked);
        }
    }

    void Update()
    {
        HideDialogueWhenPlayerLeaves();
        TryInteract();
    }

    void FixedUpdate()
    {
        if (animator == null)
        {
            return;
        }

        if (!broken)
        {
            animator.SetBool("Farm", true);
        }
    }

    void TryInteract()
    {
        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (!IsPlayerInInteractRange())
        {
            return;
        }

        bool dialogueOpened = ToggleDialogueVisible();
        if (dialogueOpened)
        {
            PlayInteractSound();
        }
    }

    bool IsPlayerInInteractRange()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                return false;
            }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        return distanceToPlayer <= interactDistance;
    }

    void HideDialogueWhenPlayerLeaves()
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null || !dialogueCanvas.activeSelf)
        {
            return;
        }

        if (!IsPlayerInInteractRange())
        {
            SetDialogueVisible(false);
        }
    }

    void SetDialogueVisible(bool visible)
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null)
        {
            return;
        }

        dialogueCanvas.SetActive(visible);
    }

    void SetMissionVisible(bool visible)
    {
        FindMissionCanvas();
        if (missionCanvas == null)
        {
            return;
        }

        missionCanvas.SetActive(visible);
    }

    bool ToggleDialogueVisible()
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null)
        {
            return false;
        }

        bool visible = !dialogueCanvas.activeSelf;
        dialogueCanvas.SetActive(visible);
        return visible;
    }

    void FindDialogueCanvas()
    {
        if (dialogueCanvas != null || string.IsNullOrWhiteSpace(dialogueCanvasName))
        {
            return;
        }

        dialogueCanvas = FindSceneCanvasByName(dialogueCanvasName);
    }

    void FindMissionCanvas()
    {
        if (missionCanvas != null || string.IsNullOrWhiteSpace(missionCanvasName))
        {
            return;
        }

        missionCanvas = FindSceneCanvasByName(missionCanvasName);
    }

    GameObject FindSceneCanvasByName(string canvasName)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            GameObject canvasObject = canvas.gameObject;
            if (canvasObject.name == canvasName && canvasObject.scene.IsValid())
            {
                return canvasObject;
            }
        }

        return null;
    }

    void BindDialogueButtons()
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null)
        {
            return;
        }

        yesButton = FindOrCreateDialogueButton(yesButton, yesButtonName);
        noButton = FindOrCreateDialogueButton(noButton, noButtonName);

        if (yesButton != null)
        {
            yesButton.onClick.RemoveListener(OnYesButtonClicked);
            yesButton.onClick.AddListener(OnYesButtonClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.RemoveListener(OnNoButtonClicked);
            noButton.onClick.AddListener(OnNoButtonClicked);
        }
    }

    Button FindOrCreateDialogueButton(Button currentButton, string buttonName)
    {
        if (currentButton != null)
        {
            return currentButton;
        }

        if (string.IsNullOrWhiteSpace(buttonName))
        {
            return null;
        }

        Transform buttonTransform = FindChildByName(dialogueCanvas.transform, buttonName);
        if (buttonTransform == null)
        {
            return null;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            button = buttonTransform.gameObject.AddComponent<Button>();
        }

        if (button.targetGraphic == null)
        {
            button.targetGraphic = buttonTransform.GetComponent<Graphic>();
        }

        return button;
    }

    Transform FindChildByName(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    void OnYesButtonClicked()
    {
        AcceptMission();
        SetMissionVisible(true);

        if (closeDialogueAfterAccept)
        {
            SetDialogueVisible(false);
        }
    }

    void OnNoButtonClicked()
    {
        SetDialogueVisible(false);
    }

    void AcceptMission()
    {
        if (!IsMissionAccepted)
        {
            IsMissionAccepted = true;
        }

        ShowMissionEnemies();
    }

    void HideMissionEnemies()
    {
        hiddenEnemies.Clear();
        AddEnemiesToHiddenList(FindObjectsByType<BatController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AddEnemiesToHiddenList(FindObjectsByType<SlimeController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AddEnemiesToHiddenList(FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        foreach (GameObject enemy in hiddenEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(false);
            }
        }
    }

    void ShowMissionEnemies()
    {
        foreach (GameObject enemy in hiddenEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }

        AddEnemiesToHiddenList(FindObjectsByType<BatController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AddEnemiesToHiddenList(FindObjectsByType<SlimeController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AddEnemiesToHiddenList(FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        foreach (GameObject enemy in hiddenEnemies)
        {
            if (enemy != null)
            {
                enemy.SetActive(true);
            }
        }
    }

    void AddEnemiesToHiddenList<T>(T[] enemies) where T : Component
    {
        foreach (T enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.scene.IsValid())
            {
                continue;
            }

            GameObject enemyObject = enemy.gameObject;
            if (!hiddenEnemies.Contains(enemyObject))
            {
                hiddenEnemies.Add(enemyObject);
            }
        }
    }

    void PlayInteractSound()
    {
        if (interactSound == null || audioSource == null)
        {
            return;
        }

        if (preventSoundOverlap && audioSource.isPlaying)
        {
            return;
        }

        audioSource.PlayOneShot(interactSound, interactSoundVolume);
    }

    public void Fix()
    {
        broken = false;
        if (npcRigidbody != null)
        {
            npcRigidbody.simulated = false;
        }
    }
}
