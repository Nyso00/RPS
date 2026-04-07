using UnityEngine;
using System.Collections;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;

public class GameManager : NetworkSingleton<GameManager>
{
    [Header("시간 변수")]
    [SerializeField] private float _startDelay = 1.0f;
    [SerializeField] private float _roundDuration = 3.0f;
    [SerializeField] private float _revealToMoveDelay = 0.5f;
    [SerializeField] private float _moveToNextRoundDelay = 0.5f;
    [SerializeField] private float _extraRoundStartDelay = 1.0f;
    [SerializeField] private float _extraRoundDuration = 5.0f;
    [SerializeField] private float _extraRoundDurationDecrement = 0.9f;
    public float MoveDuration = 1.0f;

    [Header("라운드 변수")]
    [SerializeField] private int _maxRounds = 30;
    [SerializeField] private int _maxExtraRounds = 10;
    [SerializeField] private int[] _blockDestroyRounds = new int[4];


    //-------------------------------- Network Variables -------------------------------------
    [NonSerialized] public NetworkVariable<float> TimerFillAmount = new(0f);
    [NonSerialized] public NetworkVariable<int> RoundNum = new(0);
    [NonSerialized] public NetworkVariable<RPS> P1RevealedChoice = new(RPS.None);
    [NonSerialized] public NetworkVariable<RPS> P2RevealedChoice = new(RPS.None);
    [NonSerialized] public NetworkVariable<GameState> State = new(GameState.WaitingForPlayers);
    [NonSerialized] public NetworkVariable<bool> IsDestroyPhase = new(false);

    [NonSerialized] public NetworkVariable<ulong> P1ClientId = new(ulong.MaxValue);
    [NonSerialized] public NetworkVariable<ulong> P2ClientId = new(ulong.MaxValue);
    [NonSerialized] public NetworkVariable<int> P1SubmitCount = new(0);
    [NonSerialized] public NetworkVariable<int> P2SubmitCount = new(0);

    [NonSerialized] public NetworkVariable<int> P1Score = new(0);
    [NonSerialized] public NetworkVariable<int> P2Score = new(0);
    [HideInInspector] public int GameScore => P1Score.Value - P2Score.Value;

    [NonSerialized] public NetworkVariable<bool> P1WantsRestart = new(false);
    [NonSerialized] public NetworkVariable<bool> P2WantsRestart = new(false);

    [NonSerialized] public NetworkVariable<int> ScoreToWin = new(0);
    //-----------------------------------------------------------------------------------------

    private RPS _p1Choice = RPS.None;
    private RPS _p2Choice = RPS.None;

    private int _currentDestroyPhase = 0;
    private enum RoundResult { Draw, Player1Win, Player2Win }

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnPlayerSubmit;
    public event Action OnWaitingForRestart;
    public event Action OnMyDisconnect;
    public event Action OnOpponentDisconnect;

    private bool _opponentLeft = false;
    private Coroutine _playRoundsCoroutine;


    public override void OnNetworkSpawn()
    {
        State.OnValueChanged += (oldState, newState) => StartCoroutine(DelayStateChange(newState));
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        if (MainUI.IsLocalMode)
        {
            P1ClientId.Value = NetworkManager.LocalClientId;
            P2ClientId.Value = NetworkManager.LocalClientId;
            ScoreToWin.Value = Bridge.Instance.BlockCountOfOneSide;
            _playRoundsCoroutine = StartCoroutine(PlayRounds());
        }
        else if (IsServer)
        {
            P1ClientId.Value = NetworkManager.LocalClientId;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            ScoreToWin.Value = Bridge.Instance.BlockCountOfOneSide;

            if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
            {
                foreach (ulong connectedId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    if (connectedId != NetworkManager.LocalClientId)
                    {
                        OnClientConnected(connectedId);
                        break;
                    }
                }
            }
        }
    }

    private IEnumerator DelayStateChange(GameState newState)
    {
        yield return null;
        OnStateChanged?.Invoke(newState);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            P2ClientId.Value = clientId;
            _playRoundsCoroutine = StartCoroutine(PlayRounds());
        }
    }

    private IEnumerator PlayRounds()
    {
        State.Value = GameState.Ready;
        yield return new WaitForSeconds(_startDelay);

        while (RoundNum.Value < _maxRounds + _maxExtraRounds)
        {
            RoundNum.Value++;

            // 1. 라운드 준비: 라운드 시간, UI 설정, 블록 파괴 준비
            yield return StartCoroutine(SetupRoundRoutine());

            // 2. 타이머 시작: 플레이어 입력 허용, 게이지 UI
            yield return StartCoroutine(TimerRoutine());

            // 3. 라운드 결과 판정: 패 공개, 플레이어 이동
            yield return StartCoroutine(EvaluateRoutine());

            // 4. 1차 게임 오버 체크
            if (CheckGameOver())
            {
                State.Value = GameState.GameOver;
                yield break;
            }

            yield return new WaitForSeconds(_moveToNextRoundDelay);
        }

        State.Value = GameState.GameOver;
    }

    private IEnumerator SetupRoundRoutine()
    {
        _p1Choice = RPS.None;
        _p2Choice = RPS.None;

        P1SubmitCount.Value = 0;
        P2SubmitCount.Value = 0;

        if (RoundNum.Value == _maxRounds + 1)
        {
            _roundDuration = _extraRoundDuration;
            State.Value = GameState.ReadyForExtraRound;
            yield return new WaitForSeconds(_extraRoundStartDelay);
        }
        else if (RoundNum.Value > _maxRounds)
        {
            _roundDuration *= _extraRoundDurationDecrement;
        }

        IsDestroyPhase.Value = ShouldDestroyBlock();
    }

    private IEnumerator TimerRoutine()
    {
        State.Value = GameState.Playing;
        TimerFillAmount.Value = 0f;

        float passedTime = 0f;
        while (passedTime < _roundDuration)
        {
            passedTime += Time.deltaTime;

            TimerFillAmount.Value = passedTime / _roundDuration;

            yield return null;
        }
        TimerFillAmount.Value = 1f;
    }

    private IEnumerator EvaluateRoutine()
    {
        RoundResult winner = FindWinner();

        State.Value = GameState.Result;
        yield return new WaitForSeconds(_revealToMoveDelay);

        if (winner == RoundResult.Player1Win)
        {
            P1Score.Value++;
        }
        else if (winner == RoundResult.Player2Win)
        {
            P2Score.Value++;
        }

        State.Value = GameState.Move;
        yield return new WaitForSeconds(MoveDuration);
    }

    private bool ShouldDestroyBlock()
    {
        bool shouldDestroyBlock = _currentDestroyPhase < _blockDestroyRounds.Length && RoundNum.Value == _blockDestroyRounds[_currentDestroyPhase];
        if (shouldDestroyBlock)
        {
            _currentDestroyPhase++;
            ScoreToWin.Value--;
        }
        return shouldDestroyBlock;
    }

    private RoundResult FindWinner()
    {
        P1RevealedChoice.Value = _p1Choice;
        P2RevealedChoice.Value = _p2Choice;

        if (_p1Choice == _p2Choice)
        {
            Debug.Log($"It's a tie! Both players chose {_p1Choice}");
            return RoundResult.Draw;
        }
        else if ((_p1Choice == RPS.Rock && _p2Choice == RPS.Scissors) ||
                 (_p1Choice == RPS.Paper && _p2Choice == RPS.Rock) ||
                 (_p1Choice == RPS.Scissors && _p2Choice == RPS.Paper) ||
                 (_p1Choice != RPS.None && _p2Choice == RPS.None))
        {
            Debug.Log($"Player 1 wins the round! ({_p1Choice} beats {_p2Choice})");
            return RoundResult.Player1Win;
        }
        else
        {
            Debug.Log($"Player 2 wins the round! ({_p2Choice} beats {_p1Choice})");
            return RoundResult.Player2Win;
        }
    }

    private bool CheckGameOver()
    {
        return GameScore >= ScoreToWin.Value || GameScore <= -ScoreToWin.Value;
    }

    public bool IsExtraRound()
    {
        return RoundNum.Value > _maxRounds;
    }

    public void SetPlayerChoice(ulong clientId, RPS choice, int playerNum)
    {
        if (State.Value != GameState.Playing)
        {
            return;
        }

        int targetPlayer = 0;

        if (MainUI.IsLocalMode)
        {
            targetPlayer = playerNum;
        }
        else
        {
            if (clientId == P1ClientId.Value)
            {
                targetPlayer = 1;
            }
            else if (clientId == P2ClientId.Value)
            {
                targetPlayer = 2;
            }
        }

        if (targetPlayer == 1)
        {
            _p1Choice = choice;
            P1SubmitCount.Value++;
        }
        else if (targetPlayer == 2)
        {
            _p2Choice = choice;
            P2SubmitCount.Value++;
        }
        else
        {
            Debug.LogWarning($"Received choice from unknown client ID: {clientId}");
        }

        NotifySubmitClientRpc(targetPlayer);
    }

    public void RequestRestartFromClient(ulong clientId)
    {
        if (clientId == P1ClientId.Value)
        {
            P1WantsRestart.Value = true;
        }
        else if (clientId == P2ClientId.Value)
        {
            P2WantsRestart.Value = true;
        }

        if (P1WantsRestart.Value && P2WantsRestart.Value)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
        }
    }

    public void RequestPlayAgain()
    {
        // 로컬 or 호스트인데 상대 나감 -> 바로 재시작
        if (MainUI.IsLocalMode || (NetworkManager.Singleton.IsServer && _opponentLeft))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
        }
        else // 상대가 있는데 재시작 요청 -> 서버로 재시작 요청 RPC 발송
        {
            OnWaitingForRestart?.Invoke();

            // 서버로 재시작 요청 RPC 발송
            var mySender = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInputSender>();
            mySender.SendRestartRequestToServer();
        }
    }

    public void ReturnToMainMenu()
    {
        // NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
    private void OnClientDisconnected(ulong clientId)
    {
        // 내가 호스트인데 연결 끊김 / 내가 클라이언트인데 상대나 내가 끊김
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            OnMyDisconnect?.Invoke();
        }
        else if (IsServer) // 내가 호스트인데 상대가 끊김
        {
            OnOpponentDisconnect?.Invoke();
            _opponentLeft = true;
        }

        if (_playRoundsCoroutine != null)
        {
            StopCoroutine(_playRoundsCoroutine);
        }
    }

    private void OnApplicationQuit()
    {
        if (NetworkManager.Singleton != null &&
           (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        base.OnDestroy();
    }


    [ClientRpc]
    private void NotifySubmitClientRpc(int playerNum, ClientRpcParams rpcParams = default)
    {
        OnPlayerSubmit?.Invoke(playerNum);
    }
}
