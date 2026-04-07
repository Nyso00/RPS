using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("게임오버 UI")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private TextMeshProUGUI _noticeText;

    private GameManager _gm;

    private void Start()
    {
        _gm = GameManager.Instance;

        _playAgainButton.onClick.AddListener(_gm.RequestPlayAgain);
        _mainMenuButton.onClick.AddListener(_gm.ReturnToMainMenu);
        _gameOverPanel.SetActive(false);

        _gm.OnStateChanged += OnGameOverState;
        _gm.OnWaitingForRestart += ShowWaitingState;
        _gm.OnMyDisconnect += ShowMyDisconnectState;
        _gm.OnOpponentDisconnect += ShowOpponentDisconnectState;
    }

    private void ShowWaitingState()
    {
        _noticeText.text = GameStrings.NoticeWaitingOpponent;
        _playAgainButton.interactable = false;
    }

    private void ShowMyDisconnectState()
    {
        _noticeText.text = GameStrings.NoticeRoomDisconnection;
        _playAgainButton.interactable = false;
        _gameOverPanel.SetActive(true);
    }

    private void OnGameOverState(GameState newState)
    {
        if (newState == GameState.GameOver)
        {
            _gameOverPanel.SetActive(true);
        }
    }

    private void ShowOpponentDisconnectState()
    {
        _noticeText.text = GameStrings.NoticeOpponentDisconnected;
        _playAgainButton.interactable = true;
        _gameOverPanel.SetActive(true);
    }

    private void OnDestroy()
    {
        _gm.OnStateChanged -= OnGameOverState;
        _gm.OnWaitingForRestart -= ShowWaitingState;
        _gm.OnMyDisconnect -= ShowMyDisconnectState;
        _gm.OnOpponentDisconnect -= ShowOpponentDisconnectState;
    }
}
