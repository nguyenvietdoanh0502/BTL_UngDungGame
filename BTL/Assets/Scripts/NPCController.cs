using UnityEngine;
using UnityEngine.InputSystem;

public class NPCController : MonoBehaviour
{
    Rigidbody2D npcRigidbody;
    Animator animator;
    AudioSource audioSource;
    PlayerController player;

    public float interactDistance = 2.0f;
    public GameObject dialogueCanvas;
    public string dialogueCanvasName = "NPCDiaglogue";
    public bool hideDialogueOnStart = true;
    public AudioClip interactSound;
    [UnityEngine.Range(0f, 1f)] public float interactSoundVolume = 1f;
    public bool preventSoundOverlap = true;

    bool broken = true;

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
        if (hideDialogueOnStart)
        {
            SetDialogueVisible(false);
        }
    }

    void Update()
    {
        TryInteract();
    }

    void FixedUpdate()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool("isRunning", false);
        if (!broken)
        {
            animator.SetBool("Farm", true);
        }
    }

    void TryInteract()
    {
        if (Keyboard.current == null || !Keyboard.current.xKey.wasPressedThisFrame)
        {
            return;
        }

        if (!IsPlayerInInteractRange())
        {
            return;
        }

        ToggleDialogueVisible();
        PlayInteractSound();
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

    void SetDialogueVisible(bool visible)
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null)
        {
            return;
        }

        dialogueCanvas.SetActive(visible);
    }

    void ToggleDialogueVisible()
    {
        FindDialogueCanvas();
        if (dialogueCanvas == null)
        {
            return;
        }

        dialogueCanvas.SetActive(!dialogueCanvas.activeSelf);
    }

    void FindDialogueCanvas()
    {
        if (dialogueCanvas != null || string.IsNullOrWhiteSpace(dialogueCanvasName))
        {
            return;
        }

        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            GameObject canvasObject = canvas.gameObject;
            if (canvasObject.name == dialogueCanvasName && canvasObject.scene.IsValid())
            {
                dialogueCanvas = canvasObject;
                return;
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
