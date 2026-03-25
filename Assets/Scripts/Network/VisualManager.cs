using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class VisualManager : MonoBehaviour
{
    [Header("게임 UI")]
    [SerializeField] private GameObject _joinCodeDisplayText; // 방장이 참가 코드를 보여줄 텍스트
    [SerializeField] private TextMeshProUGUI _roundText;
    [SerializeField] private Image _timerGauge;
    [SerializeField] private Image _myChoiceImage;
    [SerializeField] private Image _enemyChoiceImage;

    [SerializeField] private GameObject _playerButtonUI;

    [Header("가위바위보 Sprite")]
    [SerializeField] private Sprite _rockSprite;
    [SerializeField] private Sprite _paperSprite;
    [SerializeField] private Sprite _scissorsSprite;

    [Header("체크마크 관련 변수")]
    [SerializeField] private Image _myCheckMarkImage;
    [SerializeField] private Image _enemyCheckMarkImage;
    [SerializeField] private float _drawSpeed = 3.0f;

    [Header("플레이어 오브젝트")]
    [SerializeField] private Transform _myCharacter;
    [SerializeField] private Transform _enemyCharacter;

    [Header("게임오버 UI")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private TextMeshProUGUI _noticeText;

    private NetworkGameManager gm;
    private bool IsPlayer1 => NetworkManager.Singleton.LocalClientId == gm.P1ClientId.Value;
    private bool _opponentLeft = false;

    private Coroutine _myCheckMarkCoroutine;
    private Coroutine _enemyCheckMarkCoroutine;

    private int CurrentScore => IsPlayer1 ? gm.GameScore : -gm.GameScore;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        gm = NetworkGameManager.Instance;
        gm.TimerFillAmount.OnValueChanged += (oldVal, newVal) => _timerGauge.fillAmount = newVal;
        gm.State.OnValueChanged += (oldVal, newVal) => OnGameStateChanged(oldVal, newVal);
        UpdateUIState(gm.State.Value);

        gm.P1SubmitCount.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal > 0)
            {
                if (IsPlayer1) DrawMyCheckMark();
                else DrawEnemyCheckMark();
            }
        };
        gm.P2SubmitCount.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal > 0)
            {
                if (IsPlayer1) DrawEnemyCheckMark();
                else DrawMyCheckMark();
            }
        };

        _myCharacter.position = new Vector3(Bridge.Instance.GetBlockX(0, true), _myCharacter.position.y, _myCharacter.position.z);
        _enemyCharacter.position = new Vector3(Bridge.Instance.GetBlockX(0, false), _enemyCharacter.position.y, _enemyCharacter.position.z);

        _playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);
        _mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        _gameOverPanel.SetActive(false);

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        StartCoroutine(HandleStateChangeRoutine(newState));
    }

    private IEnumerator HandleStateChangeRoutine(GameState newState)
    {
        yield return null; // Wait one frame to ensure all state changes are processed
        UpdateUIState(newState);
    }

    private void UpdateUIState(GameState newState)
    {
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                _joinCodeDisplayText.GetComponent<TextMeshProUGUI>().text = $"Join Code\n{MainUI.JoinCode}";
                _joinCodeDisplayText.SetActive(true);
                _roundText.text = "Waiting for Players...";
                break;

            case GameState.Ready:
                _joinCodeDisplayText.SetActive(false);
                _roundText.text = "Ready...";
                break;

            case GameState.ReadyForExtraRound:
                _roundText.text = "Ready for Extra Round...";
                break;

            case GameState.Playing:
                HideChoiceImages();
                _playerButtonUI.SetActive(true);
                _roundText.text = gm.IsExtraRound() ? "Extra Round" : $"Round {gm.RoundNum.Value}";

                if (gm.IsDestroyPhase.Value)
                {
                    Bridge.Instance.BeforeDestroyBlock();
                }
                break;

            case GameState.Result:
                _playerButtonUI.SetActive(false);
                ShowChoices();
                break;

            case GameState.Move:
                if (_myCheckMarkCoroutine != null)
                {
                    StopCoroutine(_myCheckMarkCoroutine);
                    _myCheckMarkCoroutine = null;
                }
                if (_enemyCheckMarkCoroutine != null)
                {
                    StopCoroutine(_enemyCheckMarkCoroutine);
                    _enemyCheckMarkCoroutine = null;
                }
                _myCheckMarkImage.fillAmount = 0f;
                _enemyCheckMarkImage.fillAmount = 0f;

                if (gm.IsDestroyPhase.Value)
                {
                    Bridge.Instance.DestroyBlock();
                }
                MovePlayers();
                break;

            case GameState.GameOver:
                HideChoiceImages();
                if (_opponentLeft || CurrentScore > 0)
                {
                    _roundText.text = "YOU WIN!";
                }
                else if (CurrentScore < 0)
                {
                    _roundText.text = "YOU LOSE!";
                }
                else
                {
                    _roundText.text = "DRAW!";
                }
                _gameOverPanel.SetActive(true);

                if (_opponentLeft && !NetworkManager.Singleton.IsServer)
                {
                    _playAgainButton.interactable = false;
                }
                break;
        }
    }

    private void ShowChoices()
    {
        _myCheckMarkImage.fillAmount = 0f;
        _enemyCheckMarkImage.fillAmount = 0f;

        RPS myChoice, enemyChoice;

        if (IsPlayer1)
        {
            myChoice = gm.P1RevealedChoice.Value;
            enemyChoice = gm.P2RevealedChoice.Value;
        }
        else
        {
            myChoice = gm.P2RevealedChoice.Value;
            enemyChoice = gm.P1RevealedChoice.Value;
        }

        if (myChoice != RPS.None)
        {
            _myChoiceImage.sprite = GetChoiceSprite(myChoice);
            _myChoiceImage.gameObject.SetActive(true);
        }
        if (enemyChoice != RPS.None)
        {
            _enemyChoiceImage.sprite = GetChoiceSprite(enemyChoice);
            _enemyChoiceImage.gameObject.SetActive(true);
        }
    }

    private void HideChoiceImages()
    {
        _myChoiceImage.gameObject.SetActive(false);
        _enemyChoiceImage.gameObject.SetActive(false);
    }


    private Sprite GetChoiceSprite(RPS choice)
    {
        return choice switch
        {
            RPS.Rock => _rockSprite,
            RPS.Paper => _paperSprite,
            RPS.Scissors => _scissorsSprite,
            _ => null,
        };
    }

    private void MovePlayers()
    {
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        int score = CurrentScore;
        Vector3 myStartPos = _myCharacter.position;
        Vector3 enemyStartPos = _enemyCharacter.position;

        float mytargetX = Bridge.Instance.GetBlockX(score, true);
        Vector3 myTargetPos = new(mytargetX, myStartPos.y, myStartPos.z);

        float enemyTargetX = Bridge.Instance.GetBlockX(score, false);
        Vector3 enemyTargetPos = new(enemyTargetX, enemyStartPos.y, enemyStartPos.z);

        float elapsedTime = 0f;
        while (elapsedTime < gm.MoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / gm.MoveDuration);

            t = t * t * (3f - 2f * t);

            _myCharacter.position = Vector3.Lerp(myStartPos, myTargetPos, t);
            _enemyCharacter.position = Vector3.Lerp(enemyStartPos, enemyTargetPos, t);
            CameraController.Instance.MoveCamera();
            yield return null;
        }
    }

    private void DrawMyCheckMark()
    {
        if (_myCheckMarkCoroutine != null)
        {
            StopCoroutine(_myCheckMarkCoroutine);
        }
        _myCheckMarkCoroutine = StartCoroutine(DrawCheckMark(_myCheckMarkImage));
    }

    private void DrawEnemyCheckMark()
    {
        if (_enemyCheckMarkCoroutine != null)
        {
            StopCoroutine(_enemyCheckMarkCoroutine);
        }
        _enemyCheckMarkCoroutine = StartCoroutine(DrawCheckMark(_enemyCheckMarkImage));
    }

    private IEnumerator DrawCheckMark(Image checkMarkImage)
    {
        checkMarkImage.fillAmount = 0f;
        while (checkMarkImage.fillAmount < 1f)
        {
            checkMarkImage.fillAmount += Time.deltaTime * _drawSpeed;
            yield return null;
        }

        checkMarkImage.fillAmount = 1f;
    }

    private void OnMainMenuButtonClicked()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainScene");
    }

    private void OnPlayAgainButtonClicked()
    {
        // 1. 호스트 특권: 방에 혼자 남았다면 묻지도 따지지도 않고 바로 대기실로 리로드!
        if (NetworkManager.Singleton.IsServer && _opponentLeft)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("OnlineScene", LoadSceneMode.Single);
            return;
        }

        // 2. 상대가 있다면 버튼 끄고 기다림 모드로 전환
        _playAgainButton.interactable = false;
        _noticeText.text = "Waiting for opponent...";

        // 서버로 재시작 요청 RPC 발송
        var mySender = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInputSender>();
        mySender.SendRestartRequestToServer();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _opponentLeft = true;
        _noticeText.text = "Opponent has disconnected.";

        // 1. 상대방이 나간 경우 (클라이언트가 탈주 / 호스트는 유지됨)
        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            // 내가 호스트라면 상대가 나갔으므로 Play Again을 재활성화!
            if (NetworkManager.Singleton.IsServer)
            {
                _playAgainButton.interactable = true;
            }
        }
        // 2. 내가 끊긴 경우 (호스트가 메인메뉴로 가서 서버가 터짐 -> 클라이언트 강제 종료됨)
        else
        {
            // 클라이언트는 방장이 없으므로 Play Again 무조건 잠금!
            _playAgainButton.interactable = false;

            // 게임 도중 터졌을 상황을 대비해 강제로 게임오버 패널 띄우기
            if (gm.State.Value != GameState.GameOver)
            {
                _gameOverPanel.SetActive(true);
                _roundText.text = "YOU WIN!";
            }
        }
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}