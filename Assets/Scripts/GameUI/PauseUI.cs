using UnityEngine;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [Header("Pause 관련 UI")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _pauseMainMenuButton;

    private GameManager _gm;

    private void Start()
    {
        _gm = GameManager.Instance;

        _pauseButton.onClick.AddListener(OnPauseButtonClicked);
        _resumeButton.onClick.AddListener(OnResumeButtonClicked);
        _pauseMainMenuButton.onClick.AddListener(_gm.ReturnToMainMenu);
        _pausePanel.SetActive(false);

        _gm.OnStateChanged += OnGameOverState;
        _gm.OnMyDisconnect += DisablePauseUI;
        _gm.OnOpponentDisconnect += DisablePauseUI;
    }

    private void OnPauseButtonClicked()
    {
        _pausePanel.SetActive(true);
        _pauseButton.interactable = false;
    }

    private void OnGameOverState(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            DisablePauseUI();
        }
    }

    private void OnResumeButtonClicked()
    {
        _pausePanel.SetActive(false);
        _pauseButton.interactable = true;
    }

    private void DisablePauseUI()
    {
        _pauseButton.interactable = false;
        _pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        _gm.OnStateChanged -= OnGameOverState;
        _gm.OnMyDisconnect -= DisablePauseUI;
        _gm.OnOpponentDisconnect -= DisablePauseUI;
    }
}
