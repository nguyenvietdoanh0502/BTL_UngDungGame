using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class UIDeathController : MonoBehaviour
{
    public Button replayButton;
    public string replayButtonName = "Replay";
    public bool autoFindReplayButton = true;

    void Awake()
    {
        BindReplayButton();
    }

    void OnEnable()
    {
        BindReplayButton();
    }

    void OnDestroy()
    {
        if (replayButton != null)
        {
            replayButton.onClick.RemoveListener(OnReplayButtonClicked);
        }
    }

    void BindReplayButton()
    {
        if (replayButton == null && autoFindReplayButton)
        {
            replayButton = FindButtonByName(replayButtonName);
        }

        if (replayButton == null)
        {
            Debug.LogWarning($"{nameof(UIDeathController)} could not find Replay button on {name}.", this);
            return;
        }

        replayButton.onClick.RemoveListener(OnReplayButtonClicked);
        replayButton.onClick.AddListener(OnReplayButtonClicked);
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

    public void OnReplayButtonClicked()
    {
        Time.timeScale = 1f;
        ResetGameState();
        ReloadCurrentScene();
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
}
