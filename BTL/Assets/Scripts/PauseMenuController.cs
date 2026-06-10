using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] Button pauseButton;
    [SerializeField] string pauseButtonName = "PauseButton";
    [SerializeField] GameObject pauseBox;
    [SerializeField] string pauseBoxName = "PauseBox";
    [SerializeField] Button continueButton;
    [SerializeField] string continueButtonName = "Continue";
    [SerializeField] Button resetButton;
    [SerializeField] string resetButtonName = "Reset";
    [SerializeField] Button returnToMenuButton;
    [SerializeField] string returnToMenuButtonName = "Return to Menu";
    [SerializeField] string menuSceneName = "Menu";
    [SerializeField] string menuScenePath = "Assets/Scenes/Menu.unity";
    [SerializeField] bool hidePauseBoxOnStart = true;

    void Awake()
    {
        ResolveReferences();
        BindButtons();

        if (hidePauseBoxOnStart && pauseBox != null)
        {
            pauseBox.SetActive(false);
        }
    }

    void OnEnable()
    {
        ResolveReferences();
        BindButtons();
    }

    void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetCurrentScene);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
        }
    }

    public void PauseGame()
    {
        ResolveReferences();

        if (pauseBox != null)
        {
            pauseBox.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (pauseBox != null)
        {
            pauseBox.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void ResetCurrentScene()
    {
        Time.timeScale = 1f;
        ResetGameState();
        ReloadCurrentScene();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        ResetGameState();
        LoadMenuScene();
    }

    void BindButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseGame);
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
            continueButton.onClick.AddListener(ResumeGame);
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetCurrentScene);
            resetButton.onClick.AddListener(ResetCurrentScene);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(ReturnToMenu);
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    void ResolveReferences()
    {
        if (pauseButton == null)
        {
            pauseButton = FindButtonByName(pauseButtonName);
        }

        if (pauseBox == null)
        {
            pauseBox = FindSceneGameObject(pauseBoxName);
        }

        if (continueButton == null && pauseBox != null)
        {
            continueButton = FindButtonInChildren(pauseBox, continueButtonName);
        }

        if (resetButton == null && pauseBox != null)
        {
            resetButton = FindButtonInChildren(pauseBox, resetButtonName);
        }

        if (returnToMenuButton == null && pauseBox != null)
        {
            returnToMenuButton = FindButtonInChildren(pauseBox, returnToMenuButtonName);
        }
    }

    void ResetGameState()
    {
        NPCController.ResetMissionState();
        EnemyKillBlockUnlocker.ResetKillCounts();
    }

    void ReloadCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.LoadSceneInPlayMode(activeScene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        if (!string.IsNullOrEmpty(activeScene.name))
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    void LoadMenuScene()
    {
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(menuScenePath))
        {
            EditorSceneManager.LoadSceneInPlayMode(menuScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }

    Button FindButtonByName(string buttonName)
    {
        if (string.IsNullOrWhiteSpace(buttonName))
        {
            return null;
        }

        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.scene.IsValid() && button.gameObject.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    Button FindButtonInChildren(GameObject root, string buttonName)
    {
        if (root == null || string.IsNullOrWhiteSpace(buttonName))
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    GameObject FindSceneGameObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (transform != null && transform.gameObject.scene.IsValid() && transform.gameObject.name == objectName)
            {
                return transform.gameObject;
            }
        }

        return null;
    }
}
