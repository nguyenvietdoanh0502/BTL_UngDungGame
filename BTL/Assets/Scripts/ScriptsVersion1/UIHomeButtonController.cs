using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class UIHomeButtonController : MonoBehaviour
{
    public Button homeButton;
    public string homeButtonName = "Home";
    public bool autoFindHomeButton = true;
    public string menuSceneName = "Menu";
    public string menuScenePath = "Assets/Scenes/Menu.unity";

    void Awake()
    {
        BindHomeButton();
    }

    void OnEnable()
    {
        BindHomeButton();
    }

    void OnDestroy()
    {
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(OnHomeButtonClicked);
        }
    }

    void BindHomeButton()
    {
        if (homeButton == null && autoFindHomeButton)
        {
            homeButton = FindButtonByName(homeButtonName);
        }

        if (homeButton == null)
        {
            Debug.LogWarning($"{nameof(UIHomeButtonController)} could not find Home button on {name}.", this);
            return;
        }

        homeButton.onClick.RemoveListener(OnHomeButtonClicked);
        homeButton.onClick.AddListener(OnHomeButtonClicked);
    }

    Button FindButtonByName(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    public void OnHomeButtonClicked()
    {
        Time.timeScale = 1f;
        ResetGameState();
        LoadMenuScene();
    }

    void ResetGameState()
    {
        NPCController.ResetMissionState();
        EnemyKillBlockUnlocker.ResetKillCounts();
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
}
