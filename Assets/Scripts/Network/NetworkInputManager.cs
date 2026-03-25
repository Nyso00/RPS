using Unity.Netcode;

public class NetworkInputManager : Singleton<NetworkInputManager>
{
    private GameControls _controls;

    protected override void Awake()
    {
        base.Awake();

        _controls = new GameControls();
        _controls.Player1.Rock.performed += ctx => TrySend(RPS.Rock);
        _controls.Player1.Paper.performed += ctx => TrySend(RPS.Paper);
        _controls.Player1.Scissors.performed += ctx => TrySend(RPS.Scissors);
    }

    private void Start()
    {
        NetworkGameManager.Instance.State.OnValueChanged += (oldState, newState) =>
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

    // public void SetSender(PlayerInputSender sender)
    // {
    //     _mySender = sender;
    // }

    private void TrySend(RPS choice)
    {
        var mySender = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInputSender>();
        if (NetworkGameManager.Instance.State.Value != GameState.Playing)
        {
            return;
        }
        mySender.SendChoiceToServer(choice);
    }

    protected override void OnDestroy()
    {
        _controls?.Dispose();
        base.OnDestroy();
    }
}
