using Unity.Netcode;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private GameControls _controls;
    private GameManager _gm;

    private void Awake()
    {
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
        _gm = GameManager.Instance;
        _gm.OnStateChanged += SwapControlEnabled;
    }

    private void TrySend(RPS choice, int playerNum)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            return;
        }

        if (_gm.State.Value != GameState.Playing)
        {
            return;
        }
        _gm.SubmitChoiceRpc(choice, playerNum);
    }

    private void SwapControlEnabled(GameState state)
    {
        if (state == GameState.Playing)
        {
            _controls.Enable();
        }
        else
        {
            _controls.Disable();
        }
    }

    private void OnDestroy()
    {
        _gm.OnStateChanged -= SwapControlEnabled;
        _controls?.Dispose();
    }
}
