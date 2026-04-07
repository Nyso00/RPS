using Unity.Netcode;

public class InputManager : Singleton<InputManager>
{
    private GameControls _controls;

    protected override void Awake()
    {
        base.Awake();

        _controls = new GameControls();
        _controls.Player1.Rock.performed += ctx => TrySend(RPS.Rock, 1);
        _controls.Player1.Paper.performed += ctx => TrySend(RPS.Paper, 1);
        _controls.Player1.Scissors.performed += ctx => TrySend(RPS.Scissors, 1);

        if (MainUI.IsLocalMode)
        {
            _controls.Player2.Rock.performed += ctx => TrySend(RPS.Rock, 2);
            _controls.Player2.Paper.performed += ctx => TrySend(RPS.Paper, 2);
            _controls.Player2.Scissors.performed += ctx => TrySend(RPS.Scissors, 2);
        }
    }

    private void Start()
    {
        GameManager.Instance.State.OnValueChanged += (oldState, newState) =>
        {
            if (newState == GameState.Playing)
            {
                _controls.Enable();
            }
            else
            {
                _controls.Disable();
            }
        };
    }
    private void TrySend(RPS choice, int playerNum)
    {
        if (GameManager.Instance == null || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }

        var playerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (playerObject == null)
        {
            return;
        }

        var mySender = playerObject.GetComponent<PlayerInputSender>();
        if (mySender == null || GameManager.Instance.State.Value != GameState.Playing)
        {
            return;
        }
        mySender.SendChoiceToServer(choice, playerNum);
    }

    protected override void OnDestroy()
    {
        _controls?.Dispose();
        base.OnDestroy();
    }
}
