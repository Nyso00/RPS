using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class InGameHUD : MonoBehaviour
{
    [Header("게임 UI")]
    [SerializeField] private GameObject _joinCodeDisplayText;
    [SerializeField] private TextMeshProUGUI _roundText;
    [SerializeField] private Image _timerGauge;
    [SerializeField] private Image _myChoiceImage;
    [SerializeField] private Image _enemyChoiceImage;

    [SerializeField] private GameObject _player1ButtonUI;
    [SerializeField] private GameObject _player2ButtonUI;

    [Header("가위바위보 Sprite")]
    [SerializeField] private Sprite _rockSprite;
    [SerializeField] private Sprite _paperSprite;
    [SerializeField] private Sprite _scissorsSprite;

    [Header("체크마크 관련 변수")]
    [SerializeField] private Image _myCheckMarkImage;
    [SerializeField] private Image _enemyCheckMarkImage;
    [SerializeField] private float _drawSpeed = 3.0f;

    private GameManager _gm;
    private bool IsPlayer1 => NetworkManager.Singleton.LocalClientId == _gm.P1ClientId.Value; // 로컬일때 항상 true

    private Coroutine _myCheckMarkCoroutine;
    private Coroutine _enemyCheckMarkCoroutine;

    private int CurrentScore => IsPlayer1 ? _gm.GameScore : -_gm.GameScore;

    private void Start()
    {
        _gm = GameManager.Instance;
        _gm.OnStateChanged += UpdateUIState;
        _gm.OnPlayerSubmit += HandlePlayerSubmitted;

        _gm.TimerFillAmount.OnValueChanged += UpdateTimerGauge;

        UpdateUIState(_gm.State.Value);
    }

    private void UpdateTimerGauge(float oldVal, float newVal)
    {
        _timerGauge.fillAmount = newVal;
    }

    private void UpdateUIState(GameState newState)
    {
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                _joinCodeDisplayText.GetComponent<TextMeshProUGUI>().text = GameStrings.JoinCodeDisplay(MainUI.JoinCode);
                _joinCodeDisplayText.SetActive(true);
                _roundText.text = GameStrings.WaitingForPlayers;
                break;

            case GameState.Ready:
                _joinCodeDisplayText.SetActive(false);
                _roundText.text = GameStrings.Ready;
                break;

            case GameState.ReadyForExtraRound:
                HideChoiceImages();
                _roundText.text = GameStrings.ReadyExtra;
                break;

            case GameState.Playing:
                HideChoiceImages();
                _player1ButtonUI.SetActive(true);
                if (MainUI.IsLocalMode)
                {
                    _player2ButtonUI.SetActive(true);
                }
                _roundText.text = _gm.IsExtraRound() ? GameStrings.ExtraRound : GameStrings.Round(_gm.RoundNum.Value);
                break;

            case GameState.Result:
                _player1ButtonUI.SetActive(false);
                _player2ButtonUI.SetActive(false);
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
                break;

            case GameState.GameOver:
                HideChoiceImages();

                if (MainUI.IsLocalMode)
                {
                    _roundText.text = CurrentScore > 0 ? GameStrings.PlayerWin(1) : CurrentScore < 0 ? GameStrings.PlayerWin(2) : GameStrings.Draw;
                }
                else
                {
                    _roundText.text = CurrentScore > 0 ? GameStrings.YouWin : CurrentScore < 0 ? GameStrings.YouLose : GameStrings.Draw;
                }
                break;
        }
    }

    private void HideChoiceImages()
    {
        _myChoiceImage.gameObject.SetActive(false);
        _enemyChoiceImage.gameObject.SetActive(false);
    }

    private void ShowChoices()
    {
        RPS myChoice = IsPlayer1 ? _gm.P1RevealedChoice.Value : _gm.P2RevealedChoice.Value;
        RPS enemyChoice = IsPlayer1 ? _gm.P2RevealedChoice.Value : _gm.P1RevealedChoice.Value;

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

    private void HandlePlayerSubmitted(int playerNum)
    {
        bool isMySubmit = (IsPlayer1 && playerNum == 1) || (!IsPlayer1 && playerNum == 2);

        if (isMySubmit) DrawMyCheckMark();
        else DrawEnemyCheckMark();
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

    private void OnDestroy()
    {
        _gm.TimerFillAmount.OnValueChanged -= UpdateTimerGauge;
        _gm.OnStateChanged -= UpdateUIState;
        _gm.OnPlayerSubmit -= HandlePlayerSubmitted;
    }
}
