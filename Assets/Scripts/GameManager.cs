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

    [NonSerialized] private NetworkVariable<int> _p1Score = new(0);
    [NonSerialized] private NetworkVariable<int> _p2Score = new(0);
    //-----------------------------------------------------------------------------------------

    // Score 관련 변수들
    private int _scoreToWin;
    public int GameScore => _p1Score.Value - _p2Score.Value;

    private RPS _p1Choice = RPS.None;
    private RPS _p2Choice = RPS.None;

    private int _currentDestroyPhase = 0;
    private enum RoundResult { Draw, Player1Win, Player2Win }

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnPlayerSubmit;
    public event Action OnWaitingForRestart;
    public event Action OnMyDisconnect;
    public event Action OnOpponentDisconnect;
    public event Action OnExecuteBlockDestroy;

    private bool _p1WantsRestart = false;
    private bool _p2WantsRestart = false;

    private bool _opponentLeft = false;
    private Coroutine _playRoundsCoroutine;


    public override void OnNetworkSpawn()
    {
        // 구독
        State.OnValueChanged += OnStateValueChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        _scoreToWin = Bridge.Instance.BlockCountOfOneSide;

        // P1, P2 ClientId 설정 및 게임 시작
        if (MainUI.IsLocalMode)
        {
            P1ClientId.Value = NetworkManager.LocalClientId;
            P2ClientId.Value = NetworkManager.LocalClientId;
            _playRoundsCoroutine = StartCoroutine(PlayRounds());
        }
        else if (IsServer)
        {
            P1ClientId.Value = NetworkManager.LocalClientId;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // 이미 상대가 연결되어 있는 경우(모두 Play Again을 누른 경우) OnClientConnected를 직접 호출하여 게임 시작
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

    private void OnStateValueChanged(GameState oldState, GameState newState)
    {
        StartCoroutine(DelayStateChange(newState));
    }

    // NetworkVariable의 모든 변경이 마무리 되도록 1프레임 지연 후에 이벤트를 발생시킴
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

    // -----------------------------------------------------------------------------------------
    //   게임 진행 코루틴 모음
    // -----------------------------------------------------------------------------------------
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

            // 4. 게임 오버 체크
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
            _p1Score.Value++;
        }
        else if (winner == RoundResult.Player2Win)
        {
            _p2Score.Value++;
        }

        if (IsDestroyPhase.Value && Mathf.Abs(GameScore) <= _scoreToWin)
        {
            NotifyBlockDestroyRpc();
        }

        State.Value = GameState.Move;
        yield return new WaitForSeconds(MoveDuration);
    }

    // -----------------------------------------------------------------------------------------
    //   게임 진행 관련 함수 모음
    // -----------------------------------------------------------------------------------------
    private bool ShouldDestroyBlock()
    {
        bool shouldDestroyBlock = _currentDestroyPhase < _blockDestroyRounds.Length && RoundNum.Value == _blockDestroyRounds[_currentDestroyPhase];
        if (shouldDestroyBlock)
        {
            _currentDestroyPhase++;
            _scoreToWin--;
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
        return GameScore >= _scoreToWin || GameScore <= -_scoreToWin;
    }

    public bool IsExtraRound()
    {
        return RoundNum.Value > _maxRounds;
    }

    private void SetPlayerChoice(ulong clientId, RPS choice, int playerNum)
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
        }
        else if (targetPlayer == 2)
        {
            _p2Choice = choice;
        }
        else
        {
            Debug.LogWarning($"Received choice from unknown client ID: {clientId}");
        }

        NotifySubmitRpc(targetPlayer);
    }

    // -----------------------------------------------------------------------------------------
    //   게임 재시작, 종료 관련 함수 모음
    // -----------------------------------------------------------------------------------------

    // 둘 모두 재시작을 원할 경우(Play Again 버튼을 눌렀을 경우) 재시작
    private void RequestRestartFromClient(ulong clientId)
    {
        if (clientId == P1ClientId.Value)
        {
            _p1WantsRestart = true;
        }
        else if (clientId == P2ClientId.Value)
        {
            _p2WantsRestart = true;
        }

        if (_p1WantsRestart && _p2WantsRestart)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.GameScene, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// 재시작 요청을 하는 함수입니다. Play Again 버튼에 부착합니다.
    /// - 로컬 모드이거나, 내가 호스트인데 상대가 이미 나간 경우에는 바로 게임 씬을 로딩하여 재시작합니다.
    /// - 상대가 나가지 않은 경우에는 상대의 재시작 요청을 기다립니다. 상대가 재시작 요청을 하면 게임 씬을 로딩하여 재시작합니다.
    /// </summary>
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
            RequestRestartRpc();
        }
    }

    /// <summary>
    /// 연결을 끊고 메인 메뉴 씬으로 돌아가는 함수입니다. Main Menu 버튼에 부착합니다.
    /// </summary>
    public void ReturnToMainMenu()
    {
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
        State.OnValueChanged -= OnStateValueChanged;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        base.OnDestroy();
    }

    // -----------------------------------------------------------------------------------------
    //   RPCs
    // -----------------------------------------------------------------------------------------

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifySubmitRpc(int playerNum, RpcParams rpcParams = default)
    {
        OnPlayerSubmit?.Invoke(playerNum);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void NotifyBlockDestroyRpc(RpcParams rpcParams = default)
    {
        OnExecuteBlockDestroy?.Invoke();
    }

    /// <summary>
    /// ServerRpc / 플레이어가 가위, 바위, 보 중 하나를 선택했을 때 호출됩니다. 해당 플레이어의 선택을 서버에 전달하여 저장합니다.
    /// </summary>
    /// <param name="choice">플레이어가 고른 패</param>
    /// <param name="playerNum">플레이어 번호(1, 2)</param>
    /// <param name="rpcParams">RPC 파라미터</param>
    [Rpc(SendTo.Server)]
    public void SubmitChoiceRpc(RPS choice, int playerNum, RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        SetPlayerChoice(senderId, choice, playerNum);
    }

    // 서버에서 둘 모두의 재시작 요청 여부를 파악할 수 있도록 ID를 담아 ServerRpc 호출
    [Rpc(SendTo.Server)]
    private void RequestRestartRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        RequestRestartFromClient(senderId);
    }
}
