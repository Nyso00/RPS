using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Netcode;

public class VisualManager : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI _roundText;
    [SerializeField] private Image _timerGauge;
    [SerializeField] private Image _myChoiceImage;
    [SerializeField] private Image _enemyChoiceImage;

    [SerializeField] private GameObject _playerButtonUI;

    [SerializeField] private Sprite _rockSprite;
    [SerializeField] private Sprite _paperSprite;
    [SerializeField] private Sprite _scissorsSprite;

    [SerializeField] private Image _myCheckMarkImage;
    [SerializeField] private Image _enemyCheckMarkImage;
    [SerializeField] private float _drawSpeed = 3.0f;

    [SerializeField] private Transform _myCharacter;
    [SerializeField] private Transform _enemyCharacter;

    private NetworkGameManager gm;
    private bool IsPlayer1 => NetworkManager.Singleton.LocalClientId == gm.P1ClientId.Value;

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
                _roundText.text = "Waiting for Players...";
                break;

            case GameState.Ready:
                _roundText.text = "Ready...";
                break;

            case GameState.ReadyForExtraRound:
                _roundText.text = "Ready for Extra Round...";
                break;

            case GameState.Playing:
                HideChoiceImages();
                _playerButtonUI.SetActive(true);
                _roundText.text = gm.IsExtraRound() ? "Extra Round" : $"Round {gm.RoundNum.Value}";

                Debug.Log($"IsDestroyPhase: {gm.IsDestroyPhase.Value}");
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
                if (CurrentScore > 0)
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
}